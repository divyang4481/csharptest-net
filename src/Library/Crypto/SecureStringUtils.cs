#region Copyright 2010 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Security;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

namespace CSharpTest.Net.Crypto
{
    /// <summary>
    /// Various utility methods for access to secure strings.  Lets be real about this before you
    /// go off, SecureString is NOT secure, it obfuscated.  If your in the process you can access 
    /// it's contents; however, if you looking at a crash dump or swap file then the SecureString
    /// provides value... just not much ;)  So these methods are actually my attempt to get people
    /// to USE a SecureString or similar class (i.e. Password) rather than continuing to use plain
    /// text strings.  Hopefully with the ease of access within the process we can provide better
    /// security without.
    /// </summary>
    public static class SecureStringUtils
    {
        /// <summary>
        /// Creates a SecureString from an enumerable set of characters, like: Create("password string");
        /// </summary>
        public static SecureString Create(IEnumerable<Char> chEnum)
        {
            return AppendAll(new SecureString(), chEnum);
        }

        /// <summary>
        /// Creates a SecureString from a stream of unicode characters
        /// </summary>
        public static SecureString Create(Stream io)
        { return Create(io, Encoding.Unicode); }

        /// <summary>
        /// Creates a SecureString from an stream of characters
        /// </summary>
        public static SecureString Create(Stream io, Encoding encoding)
        {
            SecureString ss = new SecureString();
            using (io)
            using (TextReader r = new StreamReader(io, encoding, false))
            {
                int ch;
                while (-1 != (ch = r.Read()))
                    ss.AppendChar((Char)ch);
            }
            ss.MakeReadOnly();
            return ss;
        }

        /// <summary>
        /// Adds the set of characters and makes the string readonly usage: 
        /// SecureString s = new SecureString().AppendAll("This is a password");
        /// </summary>
#if NET20
        public static SecureString AppendAll<T>(SecureString sstr, T chEnum) where T : IEnumerable<char>
#else
        public static SecureString AppendAll<T>(this SecureString sstr, T chEnum) where T : IEnumerable<char>
#endif
        {
            foreach (char ch in chEnum)
                sstr.AppendChar(ch);
            sstr.MakeReadOnly();
            return sstr;
        }

        /// <summary>
        /// Returns a stream of Unicode bytes from the give SecureString instance
        /// </summary>
#if NET20
        public static TextReader ToTextReader(SecureString data)
#else
        public static TextReader ToTextReader(this SecureString data)
#endif
        {
            return new UnicodeReader(new SecureStringStream(data), Encoding.Unicode);
        }
        /// <summary>
        /// Returns a stream of Unicode bytes from the give SecureString instance
        /// </summary>
#if NET20
        public static Stream ToStream(SecureString data)
#else
        public static Stream ToStream(this SecureString data)
#endif
        {
            return new SecureStringStream(data);
        }
        /// <summary>
        /// Converts a System.Security.SecureString into an array of bytes using System.Text.Encoding.Unicode
        /// </summary>
#if NET20
        public static byte[] ToByteArray(SecureString data)
#else
        public static byte[] ToByteArray(this SecureString data)
#endif
        { return ToByteArray(data, Encoding.Unicode); }
        /// <summary>
        /// Converts a System.Security.SecureString into an array of bytes using the Encoding specified
        /// </summary>
#if NET20
        public static byte[] ToByteArray(SecureString data, Encoding encoding)
#else
        public static byte[] ToByteArray(this SecureString data, Encoding encoding)
#endif
        {
            Char[] chars = new Char[data.Length];
            GCHandle hchars = GCHandle.Alloc(chars, GCHandleType.Pinned);
            try
            {
                CopyChars(data, 0, (char[])hchars.Target, 0, data.Length);
                return encoding.GetBytes(chars);
            }
            finally
            {
                Array.Clear(chars, 0, chars.Length);
                hchars.Free();
            }
        }
        /// <summary>
        /// Returns the secure string as an array of characters
        /// </summary>
#if NET20
        public static Char[] ToCharArray(SecureString data)
#else
        public static Char[] ToCharArray(this SecureString data)
#endif
        {
            Char[] chars = new Char[data.Length];
            CopyChars(data, 0, chars, 0, data.Length);
            return chars;
        }
        /// <summary>
        /// Copies the specified range of characters from the secure string to the output character array.
        /// </summary>
#if NET20
        public static void CopyChars(SecureString input, int inputOffset, Char[] output, int outputOffset, int outputLength)
#else
        public static void CopyChars(this SecureString input, int inputOffset, Char[] output, int outputOffset, int outputLength)
#endif
        {
            Check.NotNull(input);
            Check.ArraySize(output, outputOffset + outputLength, int.MaxValue);
            Check.InRange(inputOffset, 0, input.Length);
            Check.InRange(outputOffset, 0, output.Length);
            Check.InRange(outputLength, 0, Math.Min(input.Length - inputOffset, output.Length - outputOffset));

            IntPtr pb = Marshal.SecureStringToBSTR(input);
            try
            {
                IntPtr from = inputOffset == 0 ? pb : new IntPtr(pb.ToInt64() + (inputOffset << 1));
                Marshal.Copy(from, output, outputOffset, outputLength);
            }
            finally
            {
                Marshal.ZeroFreeBSTR(pb);
            }
        }
    }
}
