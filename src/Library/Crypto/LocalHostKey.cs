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
using System.Collections.Generic;
using System.Security.Cryptography;

namespace CSharpTest.Net.Crypto
{
    /// <summary>
    /// Provides access to local machine and local user encryption via the ProtectedData class
    /// </summary>
    public class LocalHostKey : AsymmetricKey
    {
        private readonly Salt _salt;
        private readonly DataProtectionScope _scope;
        private LocalHostKey(DataProtectionScope scope)
        {
            _scope = scope;
            _salt = null;
        }
        private LocalHostKey(DataProtectionScope scope, Salt salt)
            : this(scope)
        {
            _salt = salt;
        }

        /// <summary>
        /// Sets or changes the salt for this encryption key
        /// </summary>
        public LocalHostKey WithSalt(Salt salt) 
        { return new LocalHostKey(_scope, salt); }

        /// <summary>Encrypts data for the current user</summary>
        public static readonly LocalHostKey CurrentUser = new LocalHostKey(DataProtectionScope.CurrentUser);
        /// <summary>Encrypts data for the this machine</summary>
        public static readonly LocalHostKey LocalMachine = new LocalHostKey(DataProtectionScope.LocalMachine);

        /// <summary> Block size </summary>
        protected override int BlockSize { get { return 1024; } }
        /// <summary> Output size </summary>
        protected override int TransformSize { get { return EncryptBlock(new byte[BlockSize]).Length; } }

        private byte[] Entropy
        {
            get
            {
                if (_salt != null)
                    return _salt.ToArray();
                return null;
            }
        }

        /// <summary> Encrypts the given bytes </summary>
        protected override byte[] EncryptBlock(byte[] blob)
        {
            return ProtectedData.Protect(blob, Entropy, _scope);
        }

        /// <summary> Decrypts the given bytes </summary>
        protected override byte[] DecryptBlock(byte[] blob)
        {
            return ProtectedData.Unprotect(blob, Entropy, _scope);
        }
    }
}
