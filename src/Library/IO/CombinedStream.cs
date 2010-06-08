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
using System.IO;

namespace CSharpTest.Net.IO
{
    /// <summary> Creates a single 'pseudo' stream out of multiple input streams </summary>
    public class CombinedStream : BaseStream
    {
        bool _valid;
        readonly IEnumerator<Stream> _streams;

        /// <summary> Creates a single 'pseudo' stream out of multiple input streams </summary>
        public CombinedStream(params Stream[] streams) : this((IEnumerable<Stream>)streams) { }
        /// <summary> Creates a single 'pseudo' stream out of multiple input streams </summary>
        public CombinedStream(IEnumerable<Stream> streams) 
        {
            _streams = streams.GetEnumerator();
            _valid = _streams.MoveNext();
        }

        /// <summary>  </summary>
        public override bool CanRead { get { return true; } }
        /// <summary> Reads from the next stream available </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            while (_valid)
            {
                int len = _streams.Current.Read(buffer, offset, count);
                if (len > 0)
                    return len;

                _streams.Current.Dispose();
                _valid = _streams.MoveNext();
            }

            return 0;
        }
        /// <summary> Disposes of all remaining streams. </summary>
        protected override void Dispose(bool disposing)
        {
            while (disposing && _valid)
            {
                _streams.Current.Dispose();
                _valid = _streams.MoveNext();
            }
                
            base.Dispose(disposing);
        }
    }
}
