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
using CSharpTest.Net.Utils;
using CSharpTest.Net.Logging;
using System.Security.Cryptography.X509Certificates;
using CSharpTest.Net.Commands;

namespace CSharpTest.Net.SslTunnel.Server
{
	static class Program
	{
		[MTAThread]
		static int Main(string[] args)
		{
			Log.Config.Output = LogOutputs.LogFile;
			Log.Config.Level = LogLevels.Info;

			if (!Environment.UserInteractive)
			{
				Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
				System.ServiceProcess.ServiceBase.Run(new SslService());
				return 0;
			}

			string ignore;
			bool nologo = ArgumentList.Remove(ref args, "nologo", out ignore);

			using (Log.AppStart(Environment.CommandLine))
			{
				if (nologo == false)
				{
					Console.WriteLine("SslTunnel.Server.exe");
					Console.WriteLine("Copyright 2009 by Roger Knapp, Licensed under the Apache License, Version 2.0");
					Console.WriteLine("");
				}

				CommandInterpreter ci = new CommandInterpreter(DefaultCommands.Help, typeof(Commands));
				ci.Run(args);
			}

			return Environment.ExitCode;
		}
	}
}
