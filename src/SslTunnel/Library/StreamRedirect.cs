#region Copyright 2009 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Threading;
using System.IO;
using System.IO.Compression;

namespace CSharpTest.Net.SslTunnel
{
	public class StreamRedirect
	{
		const int BUFF_SIZE = 16383; //16kb - 0x03fff
		readonly Stream _from;
		readonly Stream _to;
		readonly string _connectionInfo;
		readonly byte[] _buffer;

		ManualResetEvent _closed;
		IAsyncResult _result;
        BinaryLogWrite _logger;

        public delegate void BinaryLogWrite(byte[] bytes, int length);

        public StreamRedirect(Stream from, Stream to, string connInfo)
		{
            _logger = null;
            _from = from;
			_to = to;
			_connectionInfo = connInfo;

			_closed = new ManualResetEvent(false);

			_buffer = new byte[BUFF_SIZE];
			_result = _from.BeginRead(_buffer, 0, _buffer.Length, OnRead, null);
		}

        public StreamRedirect(Stream from, Stream to, string connInfo, BinaryLogWrite logger)
            : this(from, to, connInfo)
        {
            _logger = logger;
        }

		void OnRead(IAsyncResult ar)
		{
			try
			{
				int count = _from.EndRead(ar);
				if (count > 0)
				{
					int origCount = count;
					byte[] buffer = _buffer;

					if(Log.IsVerboseEnabled) 
						Log.Verbose("Forwarding {0} bytes from {1}", count, _connectionInfo);

                    if (_logger != null)
                        _logger(buffer, count);

					_to.Write(buffer, 0, count);
                    _result = _from.BeginRead(_buffer, 0, _buffer.Length, OnRead, null);
				}
				else
					Close();
			}
			catch (EndOfStreamException)
			{ Close(); }
			catch (IOException)
			{ Close(); }
			catch (ObjectDisposedException)
			{ Close(); }
			catch (ThreadAbortException)
			{ Close(); throw; }
			catch (Exception e)
			{
				Log.Error(e);
				Close();
			}
		}

		public WaitHandle WaitClosed { get { return _closed; } }

		public void Close()
		{
			_from.Close();
			_to.Close();
			_closed.Set();
		}
	}
}
