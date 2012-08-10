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
using CSharpTest.Net.IO;
using System.Security;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;

namespace CSharpTest.Net.Crypto
{
    /// <summary>
    /// Allows you to stream raw bytes from a secure string, use UTF16 to decode text
    /// </summary>
    public class SecureStringStream : MarshallingStream
    {
        IntPtr _hBytes;

        /// <summary>
        /// Creates a stream from the provided SecureString's contents, use UTF16 to decode text
        /// </summary>
        public SecureStringStream(SecureString str)
            : this(Marshal.SecureStringToBSTR(str), str.Length * 2)
        { }

        private SecureStringStream(IntPtr hBytes, int length)
            : base(hBytes, true, length)
        { _hBytes = hBytes; }

        /// <summary> Disposes of the decrypted string </summary>
        protected override void Dispose(bool disposing)
        {
            if (_hBytes != null)
            {
                Marshal.ZeroFreeBSTR(_hBytes);
                _hBytes = IntPtr.Zero;
            }
            base.Dispose(disposing);
        }
    }
}
