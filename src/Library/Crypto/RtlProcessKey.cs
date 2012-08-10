#region Copyright 2010-2012 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.ComponentModel;

namespace CSharpTest.Net.Crypto
{
    /// <summary>
    /// Provides the ability to encrypt and decrypt data accessable by the current 
    /// process only, requires 
    /// </summary>
    public class RtlProcessKey : AsymmetricKey
    {
        /// <summary> Returns a single instance of the encryptor, it's thread-safe. </summary>
        public static readonly IEncryptDecrypt Encryptor = new RtlProcessKey();

        const int BLOCK = 32;
        /// <summary> The size of an input block </summary>
        protected override int BlockSize { get { return BLOCK - 1; } }
        /// <summary> The size of an output block </summary>
        protected override int TransformSize { get { return BLOCK; } }

        /// <summary> Encrypts the block of data </summary>
        protected override byte[] EncryptBlock(byte[] blob)
        {
            byte[] output = new byte[BLOCK];
            GCHandle h = GCHandle.Alloc(output, GCHandleType.Pinned);
            try
            {
                output[0] = (byte)blob.Length;
                Array.Copy(blob, 0, output, 1, blob.Length);
                Win32.Protect(h.AddrOfPinnedObject(), BLOCK);
                return output;
            }
            finally { h.Free(); }
        }

        /// <summary> Decrypts the block of data </summary>
        protected override byte[] DecryptBlock(byte[] blob)
        {
            byte[] temp = new byte[BLOCK];
            GCHandle h = GCHandle.Alloc(temp, GCHandleType.Pinned);
            try
            {
                Array.Copy(blob, 0, temp, 0, blob.Length);
                Win32.Unprotect(h.AddrOfPinnedObject(), BLOCK);
                byte[] result = new byte[Check.InRange<int>(temp[0], 0, BLOCK - 1)];
                Array.Copy(temp, 1, result, 0, result.Length);
                Array.Clear(temp, 0, temp.Length);
                return result;
            }
            finally { h.Free(); }
        }

        /// <summary> Uses the same API as System.Security.SecureString </summary>
        private static class Win32
        {
            //Vista/2k3 only : [DllImport("Crypt32.dll")]
            [DllImport("advapi32.dll", EntryPoint="SystemFunction040", SetLastError = true)]
            static extern int CryptProtectMemory(IntPtr pDataIn, int cbDataIn, int dwFlags);
            //Vista/2k3 only : [DllImport("Crypt32.dll")]
            [DllImport("advapi32.dll", EntryPoint = "SystemFunction041", SetLastError = true)]
            static extern int CryptUnprotectMemory(IntPtr pData, int cbData, int dwFlags);

            public static void Protect(IntPtr ptr, int szBytes)
            {
                Check.Assert<Win32Exception>(CryptProtectMemory(ptr, szBytes, 0) >= 0);
            }

            public static void Unprotect(IntPtr ptr, int szBytes)
            {
                Check.Assert<Win32Exception>(CryptUnprotectMemory(ptr, szBytes, 0) >= 0);
            }
        }
    }

}
