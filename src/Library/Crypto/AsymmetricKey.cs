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
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using CSharpTest.Net.IO;
using System.Runtime.InteropServices;

namespace CSharpTest.Net.Crypto
{
    interface IBlockTransform
    {
        byte[] EncryptBlock(byte[] blob);
        byte[] DecryptBlock(byte[] blob);
    }

    /// <summary>
    /// Provides the ability to encrypt and decrypt block-transform data
    /// </summary>
    public abstract class AsymmetricKey : CryptoKey, IBlockTransform
    {
        /// <summary> Provides the size, in bytes, of the maximum transform unit </summary>
        protected abstract int BlockSize { get; }
        /// <summary> Proivdes the output size, in bytes, assuming an input of BlockSize </summary>
        protected abstract int TransformSize { get; }

        /// <summary>Encrypts a raw data block as a set of bytes</summary>
        protected abstract byte[] EncryptBlock(byte[] blob);
        /// <summary>Decrypts a raw data block as a set of bytes</summary>
        protected abstract byte[] DecryptBlock(byte[] blob);

        /// <summary>Encrypts a raw data block as a set of bytes</summary>
        public sealed override byte[] Encrypt(byte[] blob)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (Stream io = Encrypt(new NonClosingStream(ms)))
                        io.Write(blob, 0, blob.Length);

                    return ms.ToArray();
                }
            }
            catch (InvalidOperationException) { throw; }
            catch { throw CryptographicException(); }
        }
        /// <summary>Decrypts a raw data block as a set of bytes</summary>
        public sealed override byte[] Decrypt(byte[] blob)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (Stream io = Decrypt(new MemoryStream(blob)))
                        IOStream.CopyStream(io, ms);

                    return ms.ToArray();
                }
            }
            catch (InvalidOperationException) { throw; }
            catch { throw CryptographicException(); }
        }
        
        /// <summary> Wraps the stream with a cryptographic stream </summary>
        public sealed override Stream Encrypt(Stream stream)
        {
            try
            {
                ICryptoTransform xform = new Encryptor(this, BlockSize, TransformSize);
                return new DisposingStream(new CryptoStream(stream, xform, CryptoStreamMode.Write))
                    .WithDisposeOf(xform);
            }
            catch (InvalidOperationException) { throw; }
            catch { throw CryptographicException(); }
        }

        /// <summary> Wraps the stream with a cryptographic stream </summary>
        public sealed override Stream Decrypt(Stream stream)
        {
            try
            {
                ICryptoTransform xform = new Decryptor(this, TransformSize, BlockSize);
                return new DisposingStream(new CryptoStream(stream, xform, CryptoStreamMode.Read))
                    .WithDisposeOf(xform);
            }
            catch (InvalidOperationException) { throw; }
            catch { throw CryptographicException(); }
        }


        #region IBlockTransform Members

        byte[] IBlockTransform.EncryptBlock(byte[] blob)
        {
            try { return this.EncryptBlock(blob); }
            catch (InvalidOperationException) { throw; }
            catch { throw CryptographicException(); }
        }

        byte[] IBlockTransform.DecryptBlock(byte[] blob)
        {
            try { return this.DecryptBlock(blob); }
            catch (InvalidOperationException) { throw; }
            catch { throw CryptographicException(); }
        }

        #endregion
        class Encryptor : ICryptoTransform
        {
            readonly IBlockTransform _encryptor;
            readonly int _blockSize, _outputSize;

            public Encryptor(IBlockTransform encryptor, int blockSize, int outputSize)
            {
                _encryptor = encryptor;
                _blockSize = blockSize;
                _outputSize = outputSize;
            }
            public void Dispose() { }

            public bool CanReuseTransform { get { return false; } }
            public bool CanTransformMultipleBlocks { get { return CanReuseTransform; } }

            public int InputBlockSize { get { return _blockSize; } }
            public int OutputBlockSize { get { return _outputSize; } }

            public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
            {
                byte[] data = TransformFinalBlock(inputBuffer, inputOffset, inputCount);
                Array.Copy(data, 0, outputBuffer, outputOffset, data.Length);
                return data.Length;
            }

            public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
            {
                byte[] data = inputBuffer;
                if (inputOffset != 0 || inputCount != inputBuffer.Length)
                {
                    data = new byte[inputCount];
                    Array.Copy(inputBuffer, inputOffset, data, 0, inputCount);
                }
                if (inputCount > 0)
                    data = Transform(_encryptor, data);
                return data;
            }

            protected virtual byte[] Transform(IBlockTransform encryptor, byte[] data)
            {
                return encryptor.EncryptBlock(data);
            }
        }

        class Decryptor : Encryptor
        {
            public Decryptor(IBlockTransform encryptor, int blockSize, int outputSize)
                : base(encryptor, blockSize, outputSize)
            { }

            protected override byte[] Transform(IBlockTransform encryptor, byte[] data)
            {
                return encryptor.DecryptBlock(data);
            }
        }
    }
}
