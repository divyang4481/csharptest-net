#region Copyright 2010-2014 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Threading;
using CSharpTest.Net.Collections;

namespace CSharpTest.Net.Threading
{
    using Node = LListNode<IWaitAndContinue>;

    /// <summary>
    /// Represents a set of queued IWorkAndContinue items that can be processed
    /// </summary>
    public class WaitAndContinueList : IDisposable
    {
        private readonly Node.LList _list;
        private WaitHandle[] _handles;
        private Node[] _workers;
        private bool _disposed;

        /// <summary> Constructs an empty WaitAndContinueList </summary>
        public WaitAndContinueList()
        {
            _list = new Node.LList();
        }

        /// <summary> Disposes of the list and all it's contents </summary>
        public void Dispose()
        {
            _disposed = true;
            _handles = null;
            _workers = null;

            lock (_list)
            {
                foreach (Node n in _list)
                    n.Value.Dispose();
                _list.Clear();
            }
        }

        /// <summary> Returns the number of work queue items </summary>
        public int Count { get { return _list.Count; } }

        /// <summary> Returns true if the work queue is empty </summary>
        public bool IsEmpty { get { return _list.First == null; } }

        /// <summary> Adds a unit of work to the list </summary>
        public void AddWork(IWaitAndContinue item)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);
            Check.NotNull(item);
            if (item.Completed)
            {
                item.Dispose();
                return;
            }
            lock (_list)
                _list.AddLast(item);
        }
        
        /// <summary> Moves the work in the other list into this list </summary>
        public void AddWork(WaitAndContinueList other)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);
            lock (_list)
            lock (other._list)
            {
                Node next = other._list.First;
                while (next != null)
                {
                    Node node = next;
                    next = next.Next;
                    other._list.Remove(node);
                    _list.AddLast(node);
                }
            }
        }

        private void Remove(ref Node node)
        {
            IWaitAndContinue item = node.Value;
            lock (_list)
                _list.Remove(node);
            node.Value = null;
            node = null;
            item.Dispose();
        }

        /// <summary>
        /// Returns true if a unit of work was processed within the timeout, or false if
        /// the timeout expired prior to a unit of work completion.
        /// </summary>
        public bool PerformWork(int timeout)
        {
            IWaitAndContinue item;
            return PerformWork(timeout, out item);
        }

        /// <summary>
        /// Returns true if a unit of work was processed within the timeout, or false if
        /// the timeout expired prior to a unit of work completion.  itemProcessed will
        /// be set to the instance of the item process when the result is true.
        /// </summary>
        public bool PerformWork(int timeout, out IWaitAndContinue itemProcessed)
        {
            itemProcessed = null;
            Node next;
            if (_handles == null)
            {
                int count = 0;
                next = _list.First;
                while (next != null)
                {
                    Node node = next;
                    IWaitAndContinue item = next.Value;
                    next = next.Next;

                    try { count += item.HandleCount; }
                    catch
                    {
                        Remove(ref node);
                        throw;
                    }
                }
                if (count == 0)
                    return false;
                _handles = _handles ?? new WaitHandle[count];
                _workers = _workers ?? new Node[count];
            }

            int offset = 0;
            next = _list.First;
            while (next != null)
            {
                Node node = next;
                IWaitAndContinue item = next.Value;
                next = next.Next;

                try
                {
                    if (item.Completed)
                    {
                        Remove(ref node);
                        continue;
                    }
                    int handleCount = item.HandleCount;
                    if (handleCount == 0)
                        continue; //not yet available
                    if ((offset + handleCount) > 64)
                        break; //not enough room, WaitHandle.WaitAny requires no more than 64 handles
                    if (_handles.Length < (offset + handleCount))
                    {
                        Array.Resize(ref _handles, offset + handleCount);
                        Array.Resize(ref _workers, offset + handleCount);
                    }

                    item.CopyHandles(_handles, offset);

                    for (int ix = 0; ix < handleCount; ix++)
                        _workers[offset++] = node;
                }
                catch
                {
                    Remove(ref node);
                    throw;
                }
            }

            if (offset == 0)
                return false;
            if (_handles.Length != offset)
            {
                Array.Resize(ref _handles, offset);
                Array.Resize(ref _workers, offset);
            }

            Node itemSignaled;
            WaitHandle handleSignaled;
            try
            {
                int response = WaitHandle.WaitAny(_handles, timeout, true);
                if (response == WaitHandle.WaitTimeout)
                    return false;
                handleSignaled = _handles[response];
                itemSignaled = _workers[response];
            }
            catch (ObjectDisposedException)
            { return false; }
            catch (AbandonedMutexException ae)
            {
                if (ae.Mutex == null || ae.MutexIndex < 0 || ae.MutexIndex >= _workers.Length)
                    throw;
                handleSignaled = ae.Mutex;
                itemSignaled = _workers[ae.MutexIndex];
            }

            itemProcessed = itemSignaled.Value;
            try { itemProcessed.ContinueProcessing(handleSignaled); }
            catch
            {
                Remove(ref itemSignaled);
                throw;
            }
            if (itemProcessed.Completed)
                Remove(ref itemSignaled);

            return true;
        }
    }
}
