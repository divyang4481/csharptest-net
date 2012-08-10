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
using System.Text;

namespace CSharpTest.Net.Crypto
{
    /// <summary> This class has been moved to CSharpTest.Net.Formatting.Safe64Encoding </summary>
    [Obsolete("This class has been moved to CSharpTest.Net.Formatting.Safe64Encoding")]
    public class AsciiEncoder
    {
        /// <summary> Returns the original byte array provided when the encoding was performed </summary>
        public static byte[] DecodeBytes(string data)
        { return Formatting.Safe64Encoding.DecodeBytes(data); }
        /// <summary> Decodes the ascii text from the bytes provided into the original byte array </summary>
        public static byte[] DecodeBytes(byte[] inData)
        { return Formatting.Safe64Encoding.DecodeBytes(inData); }
        /// <summary> Returns a encoded string of ascii characters that are URI safe </summary>
        public static string EncodeBytes(byte[] inData)
        { return Formatting.Safe64Encoding.EncodeBytes(inData); }
    }
}
