#region Copyright 2011 by Roger Knapp, Licensed under the Apache License, Version 2.0
/* Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *   http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using CSharpTest.Net.Interfaces;
using CSharpTest.Net.Serialization;

namespace CSharpTest.Net.Collections
{
    /// <summary>
    /// Represents a seriese of operations on a BPlusTree that can be committed and rolled
    /// back as an atomic operation.
    /// </summary>
    public sealed class BPlusTransaction : ITransactable
    {
        enum TransactionState { Created = 0, Pending = 1, Committing = 2, Committed = 3, RolledBack = 4, Disposed = 5 }
        enum TransactionEntryType { Create = 0x0dd10101, Delete = 0x0dd10202, Update = 0x0dd10303, EoF = 0x0dd10000, }
        static readonly byte[] NoData = new byte[0];

        private readonly Stream _stream;
        private readonly bool _existing;
        private readonly List<ITransactable> _transactables;
        private readonly byte[] _checkvalue;
        private TransactionState _state;
        private long _position;

        internal BPlusTransaction(Stream stream, bool existing)
        {
            _existing = existing;
            _stream = stream;
            _position = 4;
            _checkvalue = Guid.NewGuid().ToByteArray();
            _transactables = new List<ITransactable>();

            if (existing)
            {
                stream.Position = 0;
                _state = (TransactionState)PrimitiveSerializer.Int32.ReadFrom(_stream);

                if (!Enum.IsDefined(typeof(TransactionState), _state))
                    throw new InvalidDataException("Invalid transaction stream.");
            }
            else
            {
                ChangeState(TransactionState.Created);
                _stream.Position = _position;
                PrimitiveSerializer.Bytes.WriteTo(_checkvalue, _stream);
                _position = _stream.Position;
                PrimitiveSerializer.Int32.WriteTo((int)TransactionEntryType.EoF, _stream);
                PrimitiveSerializer.Bytes.WriteTo(_checkvalue, _stream);
                stream.Flush();
            }
        }

        internal bool RollbackRequired { get { return _state == TransactionState.Pending || _state == TransactionState.Committing; } }

        /// <summary/>
        ~BPlusTransaction() { Dispose(false); }
        /// <summary>Will rollback the transaction if it was explicity Committed </summary>
        public void Dispose() { GC.SuppressFinalize(this); Dispose(true); }

        void Dispose(bool disposing)
        {
            if (_state != TransactionState.Disposed)
            {
                if (_state != TransactionState.Committed)
                    Rollback();
                ChangeState(TransactionState.Disposed);

                if (disposing)
                    _stream.Dispose();
            }
        }

        void ChangeState(TransactionState newState)
        {
            Check.Assert<InvalidOperationException>(
                (_state != TransactionState.RolledBack || newState == TransactionState.Disposed)
                && _state != TransactionState.Disposed);

            try
            {
                _state = newState;
                _stream.Position = 0;
                PrimitiveSerializer.Int32.WriteTo((int)_state, _stream);
                _stream.Flush();
            }
            catch (ObjectDisposedException)
            {
                if (newState != TransactionState.Disposed)
                    throw;
            }
        }

        /// <summary> Aborts the operation and reverts pending changes </summary>
        public void Rollback()
        {
            if (_state == TransactionState.RolledBack)
                return;
            ChangeState(TransactionState.RolledBack);

            try
            {
                try { } finally { Rollback(0); }
            }
            finally
            { _transactables.Clear(); }
        }

        private void Rollback(int index)
        {
            if (index >= _transactables.Count)
                return;
            try
            { _transactables[index].Rollback(); }
            finally
            { Rollback(index + 1); }
        }

        /// <summary> Completes the operation </summary>
        public void Commit()
        {
            Check.Assert<InvalidOperationException>(_state == TransactionState.Pending, "The transaction state is in an invalid state for commit.");
            ChangeState(TransactionState.Committing);
            try
            {
                foreach (ITransactable part in _transactables)
                {
                    part.Commit();
                }

                ChangeState(TransactionState.Committed);
            }
            catch
            {
                Rollback();
                throw;
            }
        }

        private void AppendRecord(INodeStorage storageIn, IStorageHandle handle, TransactionEntryType entryType, byte[] body)
        {
            IPersistentNodeStorage storage = storageIn as IPersistentNodeStorage;
            if (storage == null)
                return;
            if (_state == TransactionState.Created)
                ChangeState(TransactionState.Pending);
            Check.Assert<InvalidOperationException>(_state == TransactionState.Pending);

            _stream.Position = _position;
            PrimitiveSerializer.Int32.WriteTo((int)entryType, _stream);
            PrimitiveSerializer.Bytes.WriteTo(body, _stream);
            storage.WriteTo(handle, _stream);
            _position = _stream.Position;

            PrimitiveSerializer.Int32.WriteTo((int)TransactionEntryType.EoF, _stream);
            PrimitiveSerializer.Bytes.WriteTo(_checkvalue, _stream);
            _stream.Flush();
        }

        internal void JoinTransaction(ITransactable part)
        {
            if (_state == TransactionState.Created)
                ChangeState(TransactionState.Pending);
            Check.Assert<InvalidOperationException>(_state == TransactionState.Pending);
            _transactables.Add(Check.NotNull(part));
        }

        internal void AddCreatedHandle(INodeStorage storageIn, IStorageHandle handle)
        { AppendRecord(storageIn, handle, TransactionEntryType.Create, NoData); }

        internal void AddDeleteHandle(INodeStorage storageIn, IStorageHandle handle)
        { AppendRecord(storageIn, handle, TransactionEntryType.Delete, NoData); }

        internal void AddModifyHandle(INodeStorage storageIn, IStorageHandle handle)
        {
            IPersistentNodeStorage storage = storageIn as IPersistentNodeStorage;
            if (storage == null) return;

            byte[] original;
            if (!storage.TryGetNode(handle, out original, BytesSerializer.RawBytes))
                throw new ArgumentException("Unable to read from handle.");

            AppendRecord(storageIn, handle, TransactionEntryType.Delete, original);
        }
    }
}
