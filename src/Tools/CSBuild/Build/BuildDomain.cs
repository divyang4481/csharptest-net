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
using System.Collections.Generic;
using CSharpTest.Net.CSBuild.BuildTasks;
using System.Reflection;
using CSharpTest.Net.CSBuild.Configuration;

namespace CSharpTest.Net.CSBuild.Build
{
	[System.Diagnostics.DebuggerDisplay("BuildDomain({ToolsVersion})")]
	class BuildDomain : IDisposable
    {
        readonly AppDomain _domain = null;
        readonly RemoteDomain _instance;
        readonly FrameworkVersions _framework;

        private BuildDomain(AppDomain domain, RemoteDomain instance, FrameworkVersions toolsVersion)
        {
            _domain = domain;
            _instance = instance;
            _framework = toolsVersion;
        }

        public int Perform(params BuildTask[] activities)
        {
            return _instance.Perform(activities);
        }

        public void Dispose()
        {
            _instance.Dispose();
            AppDomain.Unload(_domain);
        }

		public FrameworkVersions ToolsVersion { get { return _framework; } }

        public static BuildDomain CreateInstance(FrameworkVersions toolsVersion, string[] properties)
        {
            AppDomainSetup setup = new AppDomainSetup();
            setup.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;
            setup.ApplicationName = String.Format("CSBuildEngine.{0}", toolsVersion);
            setup.ConfigurationFile = GetConfigPath(toolsVersion);
            setup.DisallowBindingRedirects = false;
            
            Log.Verbose("Constructing AppDomain for build: {0}, version = {1}", setup.ApplicationName, toolsVersion);
            AppDomain domain = AppDomain.CreateDomain(setup.ApplicationName, AppDomain.CurrentDomain.Evidence, setup);
            RemoteDomain instance = (RemoteDomain)domain.CreateInstanceAndUnwrap(typeof(RemoteDomain).Assembly.FullName, typeof(RemoteDomain).FullName);

            instance.Framework = toolsVersion;
            instance.Properties = properties;
            instance.SetLog(Log.TextWriter, Log.ConsoleLevel);

            return new BuildDomain(domain, instance, toolsVersion);
        }

        private static string GetConfigPath(FrameworkVersions toolsVersion)
        {
            string config = String.Format("CSharpTest.Net.CSBuild.{0}.config", toolsVersion.ToString());
            using (TextReader rdr = new StreamReader(typeof(BuildEngine).Assembly.GetManifestResourceStream(config)))
                config = rdr.ReadToEnd();

            string tmpConfig = Path.Combine(Path.GetTempPath(), String.Format("CSBuildEngine.{0}.config", toolsVersion.ToString()));
            File.WriteAllText(tmpConfig, config);
            return tmpConfig;
        }

        private class RemoteDomain : MarshalByRefObject, IDisposable
        {
            BuildEngine _engine = null;
            FrameworkVersions _framework;
            string[] _properties;

            public RemoteDomain()
            { 
				_framework = FrameworkVersions.v20;
				//AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
			}

            public override object InitializeLifetimeService()
            { return null; }

			//System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
			//{
			//    AssemblyName asmName = new AssemblyName(args.Name);
			//    string bin = Util.MakeFrameworkBinPath(_framework);

			//    string testDll = Path.Combine(bin, asmName.Name + ".dll");
			//    if (File.Exists(testDll))
			//        return Assembly.LoadFile(testDll);
			//    return null;
			//}

            public void Dispose() 
            {
                if (_engine != null)
                    _engine.Dispose();
                _engine = null;
            }

            public void SetLog(TextWriter output, System.Diagnostics.TraceLevel consoleLevel) 
            { 
                Log.Open(output);
                Log.ConsoleLevel = consoleLevel;
            }

            public FrameworkVersions Framework { set { _framework = value; } }
            public string[] Properties { set { _properties = value; } }

            BuildEngine CreateEngine(FrameworkVersions toolsVersion)
            {
                string dir = Util.MakeFrameworkBinPath(toolsVersion);
                //if (!CSBuildConfig.ToDictionary(_properties).TryGetValue("FrameworkBIN" + _framework, out dir))

                BuildEngine engine = new BuildEngine(toolsVersion, dir);
                return engine;
            }

            public int Perform(params BuildTask[] actions)
            {
                if(_engine == null)
                    _engine = CreateEngine(_framework);

				int result = 0;
				foreach (BuildTask task in actions)
					result += task.Perform(_engine);

				return result;
            }
        }
    }
}
