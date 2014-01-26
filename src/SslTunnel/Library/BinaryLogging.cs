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
using System.IO;
using System.Net;

namespace CSharpTest.Net.SslTunnel
{
    class BinaryLogging : IDisposable
    {
        int __gSequence = 0;
        readonly bool _enabled;
        readonly string _logDirectory;
        readonly IPEndPoint _client, _server;
        readonly Stream _io;

        public BinaryLogging(string logDir, IPEndPoint client, IPEndPoint server)
        {
            _client = client;
            _server = server;
            _logDirectory = logDir;
            _enabled = !String.IsNullOrEmpty(_logDirectory) && Directory.Exists(_logDirectory);

            if (_enabled)
            {
                try
                {
                    string path = _logDirectory;
                    path = Path.Combine(path, String.Format("{0}({1})", _server.Address, _server.Port));
                    path = Path.Combine(path, DateTime.Now.ToString("yyyy-MM-dd"));
                    path = Path.Combine(path, String.Format("{0}", _client.Address));
                    Directory.CreateDirectory(path);

                    string fname = String.Format("{0}.{1:x4} ({2}).log", 
                        DateTime.Now.ToString("HH-mm-ss"),
                        System.Threading.Interlocked.Increment(ref __gSequence),
                        _client.Port);
                    fname = fname.Replace(":", ".");

                    _io = File.Open(Path.Combine(path, fname), FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                }
                catch
                {
                    _enabled = false;
                }
            }
        }

        public void Dispose()
        {
            if (_io != null)
                try { _io.Dispose(); } catch { }
        }

        public void FromClient(byte[] bytes, int length)
        {
            if (!_enabled) return;
            lock (_io)
            {
                _io.Write(bytes, 0, length);
            }
        }

        public void FromServer(byte[] bytes, int length)
        {
            if (!_enabled) return;
            lock (_io)
            {
                _io.Write(bytes, 0, length);
            }
        }
    }
}
