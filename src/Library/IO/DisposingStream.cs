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
using System.Collections.Generic;
using System.IO;
using CSharpTest.Net.Collections;

namespace CSharpTest.Net.IO
{
    /// <summary>
    /// Servers a dual-role, it can prevent an aggregated stream from disposing, or
    /// it can manage other objects that need to be disposed when the stream is disposed.
    /// </summary>
    public class DisposingStream : AggregateStream
    {
        readonly DisposingList _disposables;

        /// <summary> Create the wrapper on the provided stream, add disposables via WithDosposeOf(...) </summary>
        public DisposingStream(Stream stream)
            : base(stream)
        {
            _disposables = new DisposingList();
        }
        /// <summary> Disposes of the stream and then all objects in the disposable list </summary>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
                _disposables.Dispose();
        }
        /// <summary> Adds an object to this stream that will be disposed when the stream is disposed. </summary>
        public DisposingStream WithDisposeOf(IDisposable disposable)
        {
            _disposables.Add(disposable);
            return this;
        }
    }
}
