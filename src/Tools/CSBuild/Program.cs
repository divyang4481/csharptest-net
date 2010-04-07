#region Copyright 2008-2009 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.IO;
using CSharpTest.Net.CSBuild.Configuration;
using CSharpTest.Net.CSBuild.BuildTasks;
using System.Collections.Generic;
using System.Diagnostics;
using CSharpTest.Net.CSBuild.Build;
using Microsoft.Build.Framework;

namespace CSharpTest.Net.CSBuild
{
	/// <summary>
	/// Main program for CS build when automating as library
	/// </summary>
	public static class Program
	{
		static ArgumentList args;
		
		/// <summary>
		/// Provides access to the main program routine
		/// </summary>
		/// <param name="arguments">The program arguments as they would appear on the command-line</param>
		public static void Run(params string[] arguments)
		{
			int result = Main(arguments);
			if (result != 0)
				throw new ApplicationException(String.Format("The operation failed, result = {0}", result));
		}

		[STAThread]
		static int Main(string[] argsRaw)
		{
			int errors = 0;
			args = new ArgumentList(argsRaw);

            Log.Open(TextWriter.Null);
            Log.ConsoleLevel = TraceLevel.Warning;
            try
            {
                CSBuildConfig config = null;

                if (args.Contains("config"))
                {
                    using (System.Xml.XmlReader rdr = new System.Xml.XmlTextReader(args["config"]))
                        config = Config.ReadXml(Config.SCHEMA_NAME, rdr);
                }
                else
                    config = Config.ReadConfig("CSBuildConfig");

                if (config == null)
                    throw new ApplicationException("Unable to locate configuration section 'CSBuildConfig', and no /config= option was given.");

                string logfile = config.Options.LogPath;
                if (args.Contains("log"))
                    logfile = Path.GetFullPath(args["log"]);

				if (logfile != null)
				{
					Directory.CreateDirectory(Path.GetDirectoryName(logfile));
					Log.Open(TextWriter.Synchronized(new StreamWriter(File.Open(logfile, FileMode.Create, FileAccess.Write, FileShare.Read | FileShare.Delete))));
					if(config.Options.ConsoleEnabled)
						Log.ConsoleLevel = args.Contains("verbose") ? TraceLevel.Verbose : !args.Contains("quiet") ? TraceLevel.Info : TraceLevel.Warning;
				}

				using (Log.AppStart(Environment.CommandLine))
				using (Log.Start("Build started {0}", DateTime.Now))
                {
                    LoggerVerbosity? verbosity = config.Options.ConsoleLevel;
                    if (args.Contains("quiet")) verbosity = Microsoft.Build.Framework.LoggerVerbosity.Quiet;
                    else if (args.Contains("verbose")) verbosity = Microsoft.Build.Framework.LoggerVerbosity.Normal;

                    string[] propertySets = args.SafeGet("p").Values;
                    string[] targetNames = new List<string>(args.Unnamed).ToArray();

                    using (CmdLineBuilder b = new CmdLineBuilder(config, verbosity, args.SafeGet("group"), targetNames, propertySets))
                    {
                        b.Start();
                        errors += b.Complete(TimeSpan.FromHours(config.Options.TimeoutHours));
                    }
                }
            }
            catch (ApplicationException ae)
            {
                Log.Verbose(ae.ToString());
                Log.Error("\r\n{0}", ae.Message);
				errors += 1;
            }
            catch (System.Configuration.ConfigurationException ce)
            {
                Log.Verbose(ce.ToString());
                Log.Error("\r\nConfiguration Exception: {0}", ce.Message);
				errors += 1;
            }
            catch (Exception e)
            {
                Log.Error(e);
				errors += 1;
            }

			if (args.Contains("wait"))
			{
				Console.WriteLine();
				Console.WriteLine("Press [Enter] to continue...");
				Console.ReadLine();
			}

			return errors;
		}
	}
}