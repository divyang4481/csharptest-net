#region Copyright 2009-2010 by Roger Knapp, Licensed under the Apache License, Version 2.0
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

namespace CSharpTest.Net.SslTunnel
{
	public class TcpRedirector : TcpForwardingBase
	{
		TcpClient _target;

		public TcpRedirector(TcpServer server, TcpClient target)
			: base(server)
		{
			_target = target;
		}

		public override void Dispose()
		{
			base.Dispose();
			_target.Dispose();
		}

		protected override TcpClient OnConnectTarget(SslServer.ConnectedEventArgs args)
		{
			return _target.Clone();
		}

		protected override void OnConnectionEstablished(SslServer.ConnectedEventArgs args, TcpClient client)
		{
		}
	}
}
