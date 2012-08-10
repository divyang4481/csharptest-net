#region Copyright 2009-2012 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using CSharpTest.Net.Utils;
using System.Reflection;
using System.ComponentModel;
using CSharpTest.Net.Logging;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using CSharpTest.Net.Commands;
using System.ServiceProcess;
using System.Text;
using System.Security.AccessControl;
using System.Security.Principal;
using CSharpTest.Net.Processes;

namespace CSharpTest.Net.SslTunnel.Server
{
    public static partial class Commands
	{
		private class CmdLineService : SslService
		{
			public new void OnStart(string[] args) { base.OnStart(args); }
			public new void OnStop() { base.OnStop(); }
		}
	
		[Command(Description = "Runs the listener in the interactive console to monitor activity.")]
		public static void Run()
		{
			Log.Config.SetOutputFormat(LogOutputs.Console, "{Level,8} - {Message}");
			Log.Config.Level = LogLevels.Verbose;
			Log.Config.Output |= LogOutputs.Console;

			CmdLineService svc = new CmdLineService();
			svc.OnStart(new string[0]);
			try
			{
				Console.WriteLine("Press [Enter] to quit...");
				Console.ReadLine();
			}
			finally
			{
				svc.OnStop();
			}
		}

		[Command(Description = "Installs the SslTunnel service, displayed as 'SSL Tunnel Service'.")]
		public static void Install()
		{
			try { Uninstall(); }
			catch { }
			RunInstallUtil();
		}

		[Command(Description = "Uninstalls the SslTunnel service.")]
		public static void Uninstall()
		{ RunInstallUtil("/uninstall"); }

		private static void RunInstallUtil(params string[] moreargs)
		{
			string appdata = Path.GetDirectoryName(Log.Config.LogFile);

			List<string> arguments = new List<string>(moreargs);
			arguments.AddRange(new string[] 
				{
					"/ShowCallStack",
					String.Format("/LogToConsole=false"),
					String.Format("/LogFile={0}", Path.Combine(appdata, "install.log")),
					String.Format("/InstallStateDir={0}", appdata),
					String.Format("{0}", typeof(Commands).Assembly.Location),
				});

			TextWriter wtrOut = Console.Out;
			Console.SetOut(new StringWriter());
			try
			{
				AppDomain.CurrentDomain.ExecuteAssembly(
					InstallUtilEXE(),
#if NET20 || NET35
					AppDomain.CurrentDomain.Evidence,
#endif
					arguments.ToArray()
				);
			}
			finally
			{
				Console.SetOut(wtrOut);
			}
		}

		private static string InstallUtilEXE()
		{
			//string loc = Path.GetDirectoryName(typeof(System.Type).Assembly.Location);
			//string file = Path.Combine(loc, "InstallUtil.exe");
			//if (File.Exists(file))
			//    return file;

			//string loc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"..\Microsoft.NET\Framework\v2.0.50727");

            string loc = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();

			string file = Path.GetFullPath(Path.Combine(loc, "InstallUtil.exe"));
			if (!File.Exists(file))
				throw new FileNotFoundException("InstallUtil.exe not found", file);
			return file;
		}
	}
}
