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
using System.IO;
using System.Reflection;
using System.Xml;
using CSharpTest.Net.CustomTool.XmlConfig;
using System.Configuration;

namespace CSharpTest.Net.CustomTool.CodeGenerator
{
    class ConfigurationLoader : IEnumerable<ICodeGenerator>
	{
		const string CONFIG_SECTION = "CmdTool";
		const string CONFIG_FILE_NAME = "CmdTool.config";

	    private static DateTime _configLoadTime;
	    private static Configuration _configFile;

        private IGeneratorArguments _args;
        private List<ICodeGenerator> _generators;

        public ConfigurationLoader(IGeneratorArguments args)
        {
            _args = args;

            _generators = new List<ICodeGenerator>();

            string filename = _args.PseudoPath;
			DirectoryInfo di = new DirectoryInfo(Path.GetDirectoryName(filename));
            while (di != null && !di.Exists)
                di = di.Parent;
            if (di != null && di.Exists)
                SearchConfig(_generators, di);

            CmdToolConfig config = ReadAppConfig();
            PerformMatch(_generators, config);
		}

        public int Count { get { return _generators.Count; } }

		private void SearchConfig(List<ICodeGenerator> generators, DirectoryInfo dir)
		{
			FileInfo[] cfgfiles = dir.GetFiles(CONFIG_FILE_NAME, SearchOption.TopDirectoryOnly);
			foreach (FileInfo file in cfgfiles)//0 or 1
			{
				CmdToolConfig config;
				using( XmlReader reader = new XmlTextReader(file.FullName) )
					config = Config.ReadXml(reader);

				config.MakeFullPaths(dir.FullName);
				PerformMatch(generators, config);
			}

			if (dir.Parent != null)
				SearchConfig(generators, dir.Parent);
		}

		private void PerformMatch(List<ICodeGenerator> generators, CmdToolConfig config)
		{
		    foreach (FileMatch match in config.Matches)
		    {
		        string directory = Path.GetDirectoryName(_args.InputPath);

		        bool ismatch = false;
                foreach (string file in Directory.GetFiles(directory, match.FileSpec))
                    ismatch |= StringComparer.OrdinalIgnoreCase.Equals(file, _args.InputPath);
                if(!ismatch)
                    continue;

		        ismatch = match.AppliesTo.Length == 0;
		        foreach (MatchAppliesTo appliesTo in match.AppliesTo)
                    ismatch |= directory.StartsWith(appliesTo.FolderPath, StringComparison.OrdinalIgnoreCase);
                if (!ismatch)
                    continue;

                foreach(GeneratorConfig gen in match.Generators)
    		        generators.Add(new OutOfProcessGenerator(gen));
		    }
		}

        CmdToolConfig ReadAppConfig()
        {
            CmdToolConfig cfg = null;
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
            try
            {
                if (_configFile == null || _configLoadTime < File.GetLastWriteTime(_configFile.FilePath))
                {
                    _configFile = ConfigurationManager.OpenExeConfiguration(typeof(Config).Assembly.Location);
                    _configLoadTime = DateTime.Now;
                }
                if (_configFile != null)
                    cfg = _configFile.GetSection(CONFIG_SECTION) as Config;
                if (cfg != null)
                    cfg.MakeFullPaths(Path.GetDirectoryName(_configFile.FilePath));
            }
            catch (Exception ex)
            { _args.WriteLine("{0}: {1}", _configFile != null ? _configFile.FilePath : "configuration error", ex.ToString()); }
            finally
            { AppDomain.CurrentDomain.AssemblyResolve -= AssemblyResolve; }

            return cfg ?? new CmdToolConfig();
        }

        static Assembly AssemblyResolve(object sender, ResolveEventArgs args)
        {
            AssemblyName asm = new AssemblyName(args.Name);
            if (asm.Name == typeof(Config).Assembly.GetName().Name)
                return typeof(Config).Assembly;
            return null;
        }

        public IEnumerator<ICodeGenerator> GetEnumerator()
        { return _generators.GetEnumerator(); }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        { return GetEnumerator(); }
    }

}
