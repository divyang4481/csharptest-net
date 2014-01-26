#region Copyright 2008-2014 by Roger Knapp, Licensed under the Apache License, Version 2.0
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

        private static int ShowHelp(TextWriter sw)
        {
            sw.WriteLine(@"
CSBuild Version {0}
Copyright 2008-2010 by Roger Knapp, Licensed under the Apache License

Targets are specified on the command line without a preceding switch. You
  May specify any number of targets, each as their own argument. All projects
  must support all the targets specified or MsBuild will fail.

Available Targets:
    clean       - Runs the clean target on all projects.
    build       - Runs the build target on all projects.
    rebuild     - Rebuilds all projects.
    [other]     - Any MsBuild target supported by the projects included.

Options are specified on the command line with a switch ('/' or '-') and if 
  required the value may be specified by using a colon ':' or equals '=' to
  separate the name from the value.
Examples:
    /name
    /name=value
    -name:value

Available Options:

    /group=n1,n2 - One or more names of configuration target groups to build.
    /config=     - Path to the configuration file to use (relative to cwd).
    /log=        - Overrides the path to log file to write (relative to cwd).
    /wait        - Causes the program to wait for user input before closing.
    /quiet       - Sets /verbosity=Quiet and limits the console output.
    /verbose     - Sets /verbosity=Normal and increases the console output.
    /verbosity=  - One of the MsBuild defined verbosity levels: 
                       Quiet, Minimal, Normal, Detailed, Diagnostic

Properties can be defined that are passed directly to MsBuild.  You can use
  multiple property definitions as needed.
Examples:
    /p:name=value
    -p=name:value
    /property:name=value
    -property=name:value

Common Properties:
    /p:Platform:AnyCPU
    /p:Configuration=Release
    /p:OutputPath=Release\AnyCPU\v3.5
    /p:TargetFrameworkVersion=v3.5
    /p:FrameworkBinPathv35=%windir%\Microsoft.NET\Framework\v3.5

Configuration file paths:
  1. All paths that are not explicitly stated otherwise are relative to cwd.
  2. Paths may contain environment variables in the form of %OPTION%
  3. Paths may include properties (see above) or values from an included file
       (see /CSBuildConfig/options/include element) by using an make-macro
       style reference: $(name)
  4. The format of the include file element (/CSBuildConfig/options/include)
       is simply 'name=value' text lines.  You may use ':' instead of '='. If
       the line of text does not include either it will be ignored.
",
            typeof(Program).Assembly.GetName().Version
            );
            return 0;
        }

		[STAThread]
		static int Main(string[] argsRaw)
        {
			int errors = 0;
            ArgumentList arguments = new ArgumentList(argsRaw);

            if (!arguments.Contains("nologo"))
            {
                Console.WriteLine("{0}", typeof (Program).Assembly);
                foreach (System.Reflection.AssemblyCopyrightAttribute a in typeof(Program).Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyCopyrightAttribute), false))
                    Console.WriteLine("{0}", a.Copyright);
                Console.WriteLine(".NET Runtime Version={0}", System.Runtime.InteropServices.RuntimeEnvironment.GetSystemVersion());
            }

		    if (arguments.Contains("?") || arguments.Contains("help"))
                return ShowHelp(Console.Out);

            Log.Open(TextWriter.Null);
            Log.ConsoleLevel = arguments.Contains("verbose") ? TraceLevel.Verbose : TraceLevel.Warning;
            try
            {
                CSBuildConfig config = null;

                if (arguments.Contains("config"))
                {
                    using (System.Xml.XmlReader rdr = new System.Xml.XmlTextReader(arguments["config"]))
                        config = Config.ReadXml(Config.SCHEMA_NAME, rdr);
                }
                else
                    config = Config.ReadConfig("CSBuildConfig");

                if (config == null)
                    throw new ApplicationException("Unable to locate configuration section 'CSBuildConfig', and no /config= option was given.");

                string logfile = config.Options.LogPath(new Dictionary<string, string>());
                if (arguments.Contains("log"))
                    logfile = Path.GetFullPath(arguments["log"]);

				if (logfile != null)
				{
					Directory.CreateDirectory(Path.GetDirectoryName(logfile));
					Log.Open(TextWriter.Synchronized(new StreamWriter(File.Open(logfile, FileMode.Create, FileAccess.Write, FileShare.Read | FileShare.Delete))));
					if(config.Options.ConsoleEnabled)
						Log.ConsoleLevel = arguments.Contains("verbose") ? TraceLevel.Verbose : !arguments.Contains("quiet") ? TraceLevel.Info : TraceLevel.Warning;
				}

                List<string> propertySets = new List<string>();
                foreach (BuildProperty p in config.Options.GlobalProperties)
                    propertySets.Add(String.Format("{0}={1}",p.Name, p.Value));
                if (config.Options.ImportOptionsFile != null)
                {
                    try
                    {
                        string fpath = config.Options.ImportOptionsFile.AbsolutePath(new Dictionary<string, string>());
                        propertySets.AddRange(File.ReadAllLines(fpath));
                    }
                    catch(FileNotFoundException e)
                    { throw new ApplicationException("Unable to locate options file: " + e.FileName, e); }
                }
                propertySets.AddRange(arguments.SafeGet("p").Values);
                propertySets.AddRange(arguments.SafeGet("property").Values);

				using (Log.AppStart(Environment.CommandLine))
				using (Log.Start("Build started {0}", DateTime.Now))
                {
                    LoggerVerbosity? verbosity = config.Options.ConsoleLevel;
                    if (arguments.Contains("quiet")) verbosity = LoggerVerbosity.Quiet;
                    else if (arguments.Contains("verbose")) verbosity = LoggerVerbosity.Normal;
                    else if (arguments.Contains("verbosity")) verbosity = (LoggerVerbosity)Enum.Parse(typeof(LoggerVerbosity), arguments["verbosity"], true);

                    string[] targetNames = new List<string>(arguments.Unnamed).ToArray();

                    using (CmdLineBuilder b = new CmdLineBuilder(config, verbosity, arguments.SafeGet("group"), targetNames, propertySets.ToArray()))
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

			if (arguments.Contains("wait"))
			{
				Console.WriteLine();
				Console.WriteLine("Press [Enter] to continue...");
				Console.ReadLine();
			}

			return errors;
		}
	}
}