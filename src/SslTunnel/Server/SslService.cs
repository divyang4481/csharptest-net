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
