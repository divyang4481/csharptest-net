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
using CSharpTest.Net.Interfaces;

namespace CSharpTest.Net.IO
{
    /// <summary>
    /// Creates a temp file based on the given file being replaced and when a call to Commit() is 
    /// made the target file is replaced with the current contents of the temporary file.
    /// </summary>
    public class TransactFile : TempFile, ITransactable
    {
        Stream _locked;
        bool _committed;
        readonly bool _created;
        readonly string _targetFile;

        /// <summary>
        /// Creates a temp file based on the given file being replaced and when a call to Commit() is 
        /// made the target file is replaced with the current contents of the temporary file.
        /// </summary>
        public TransactFile(string targetName) : base()
        {
            if (!Path.IsPathRooted(targetName))
                targetName = Path.GetFullPath(targetName);

            //hold an exclusive write-lock until we are committed.
            _created = !File.Exists(targetName);
            _locked = File.Open(targetName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
            _committed = false;
            _targetFile = targetName;
        }

        /// <summary>
        /// Returns the originally provided filename that is being replaced
        /// </summary>
        public string TargetFile { get { return _targetFile; } }

        /// <summary>
        /// Commits the replace operation on the file
        /// </summary>
        public void Commit() { _committed = true; Dispose(true); }

        /// <summary> 
        /// Aborts the operation and reverts pending changes 
        /// </summary>
        public void Rollback()
        {
            Check.Assert<InvalidOperationException>(_committed == false);
            Dispose(true);
        }

        /// <summary>
        /// Disposes of the open stream and the temporary file.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (_locked != null)
            {
                try
                {
                    if (_committed && disposing)
                    {
                        using (Stream source = Read(FileShare.Read))
                        {
                            long length = source.Length;
                            long copied = IOStream.CopyStream(source, _locked);
                            _locked.SetLength(copied);

                            Check.Assert<IOException>(length == copied);
                        }
                    }
                }
                finally
                {
                    _locked.Dispose();
                    _locked = null;

                    if (_created && !_committed && disposing)
                    {
                        try { File.Delete(_targetFile); }
                        catch { }
                    }
                }
            }
            base.Dispose(disposing);
        }
    }
}
