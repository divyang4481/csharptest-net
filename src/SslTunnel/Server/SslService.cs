#region Copyright 2009-2014 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;

namespace CSharpTest.Net.SslTunnel.Server
{
	partial class SslService : ServiceBase
	{
		IDisposable _running = null;

		public SslService()
		{
			InitializeComponent();
		}

		protected override void OnStart(string[] args)
		{
			Log.Write("Service starting: {0}", Environment.CommandLine);

			TunnelConfig config = TunnelConfig.Load();
			_running = config.Start();

			Log.Verbose("Service running.");
		}

		protected override void OnStop()
		{
			Log.Verbose("Service stopping.");

			if (_running != null)
				_running.Dispose();
			_running = null;

			Log.Write("Service stopped.");
		}
	}
}
