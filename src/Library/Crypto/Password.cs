#region Copyright 2010-2011 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Text;
using System.Security;
using System.Runtime.InteropServices;

namespace CSharpTest.Net.Crypto
{
    /// <summary>
    /// Stores an encrypted version of the supplied password in memory so that it
    /// can be provided as clear-text to external systems.
    /// </summary>
    public class Password : PasswordKey, IEquatable<Password>
    {
        /// <summary> Returns the encoding used for passwords </summary>
        public static Encoding Encoding { get { return Encoding.BigEndianUnicode; } }

        readonly byte[] _passphrase;

        /// <summary> Creates the password from the given bytes and salt </summary>
        public Password(bool clear, byte[] bytes)
            : base(false, Check.ArraySize(bytes, 1, int.MaxValue))
        {
            // _passkey = LocalHostKey.CurrentUser.WithSalt(new Salt());
            _passphrase = Passkey.Encrypt(bytes);
            if (clear) Array.Clear(bytes, 0, bytes.Length);
        }
        /// <summary> Creates the password from the given data and salt </summary>
        public Password(string data)
            : this(true, Password.Encoding.GetBytes(data))
        { }

        /// <summary> Creates the password from the given data and salt </summary>
        public Password(SecureString data)
            : this(true, SecureStringUtils.ToByteArray(data, Password.Encoding))
        { }

        /// <summary>
        /// Allows overriding the encryption/decryption support for the in-memory password
        /// </summary>
        protected virtual IEncryptDecrypt Passkey { get { return RtlProcessKey.Encryptor; } }

        /// <summary> Removes the memory representation of this password and key </summary>
        [System.Diagnostics.DebuggerNonUserCode]
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Array.Clear(_passphrase, 0, _passphrase.Length);
        }

        /// <summary> Returns a salted hash for the password </summary>
        public PasswordHash CreateHash() { return CreateHash(new Salt()); }
        /// <summary> Returns a salted hash for the password </summary>
        public PasswordHash CreateHash(Salt salt)
        {
            return new PasswordHash(this, salt);
        }

        /// <summary> Returns a stream from which the password can be read </summary>
        public Stream ReadBytes()
        { return Passkey.Decrypt(new MemoryStream(_passphrase, 0, _passphrase.Length, false, false)); }
        /// <summary> Returns a stream from which the password can be read </summary>
        public TextReader ReadText()
        { return new UnicodeReader(ReadBytes(), Password.Encoding); }

        /// <summary> Returns a System.Security.SecureString from the password </summary>
        public SecureString ToSecureString()
        {
            using (Stream io = ReadBytes())
                return SecureStringUtils.Create(io, Password.Encoding);
        }

        /// <summary> Returns true if the other object is equal to this one </summary>
        public override bool Equals(object obj)
        {
            return Comparer.Equals(this, obj as Password);
        }

        /// <summary> Returns true if the other object is equal to this one </summary>
        public bool Equals(Password other)
        {
            if (((object)other) == null) return false;
            using (Stream a = ReadBytes())
            using (Stream b = other.ReadBytes())
            {
                int nexta = 0, nextb = 0;
                while (nexta != -1 && nextb != -1 && nexta == nextb)
                {
                    nexta = a.ReadByte();
                    nextb = b.ReadByte();
                }
                return (nexta == nextb);
            }
        }

        /// <summary> Extracts the correct hash code </summary>
        public override int GetHashCode()
        {
            using (Stream a = ReadBytes())
                return Hash.MD5(a).GetHashCode();
        }

        /// <summary> Compares the two objects for non-reference equality </summary>
        public static bool Equals(Password x, Password y)
        {
            return Comparer.Equals(x, y);
        }
        /// <summary> Compares the two objects for non-reference equality </summary>
        public static int GetHashCode(Password obj)
        {
            return Comparer.GetHashCode(obj);
        }

        /// <summary> Compares the two objects for non-reference equality </summary>
        public static bool operator ==(Password x, Password y)
        {
            return Comparer.Equals(x, y);
        }
        /// <summary> Compares the two objects for non-reference equality </summary>
        public static bool operator !=(Password x, Password y)
        {
            return !Comparer.Equals(x, y);
        }

        /// <summary> return a non-reference equality comparer for this class </summary>
        public static readonly EqualityComparer Comparer = new EqualityComparer();

        /// <summary> Implements the equality comparer </summary>
        [System.Diagnostics.DebuggerNonUserCode]
        public sealed class EqualityComparer : EqualityComparer<Password>, IEqualityComparer<Password>
        {
            /// <summary> Compares the two objects for non-reference equality </summary>
            public override bool Equals(Password x, Password y)
            {
                if (((object)x) == null) return ((object)y) == null;
                if (((object)y) == null) return false;
                return x.Equals(y);
            }
            /// <summary> Extracts the correct hash code </summary>
            public override int GetHashCode(Password obj)
            {
                return ((object)obj) == null ? 0 : obj.GetHashCode();
            }
        }
    }
}
