#region Copyright 2008 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using Microsoft.Build.BuildEngine;
using Microsoft.Build.Framework;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace CSharpTest.Net.CSBuild.Implementation
{
	[System.Diagnostics.DebuggerDisplay("Count = {Count}")]
	class BuildList : MarshalByRefObject, IDisposable
	{
		static AppDomain __domain = null;

		string _toolsVersion;
		Engine _engine;
		ItemLookup _items;
		bool Is35 = false;
		Dictionary<string, bool> _unknown = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

		public static BuildList CreateInstance(string toolsVersion, string frameworkDir, bool hardReferences)
		{
			if (__domain != null)
			{//need to get rid of old domain...
				AppDomain.Unload(__domain);
				__domain = null;
			}

			BuildList instance = null;
			string verVnoDot = String.Format("v{0}", toolsVersion.Remove(1, 1));

			string config = String.Format("CSharpTest.Net.CSBuild.{0}.config", verVnoDot);
			using (TextReader rdr = new StreamReader(typeof(BuildList).Assembly.GetManifestResourceStream(config)))
				config = rdr.ReadToEnd();

			string tmpConfig = Path.Combine(Path.GetTempPath(), String.Format("CSBuild.{0}.config", verVnoDot));
			File.WriteAllText(tmpConfig, config);

			AppDomainSetup setup = new AppDomainSetup();
			setup.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;
			setup.ApplicationName = String.Format("CSBuild - v{0}", toolsVersion);
			setup.ConfigurationFile = tmpConfig;
			setup.DisallowBindingRedirects = false;

			__domain = AppDomain.CreateDomain(setup.ApplicationName, AppDomain.CurrentDomain.Evidence, setup);
			instance = (BuildList)__domain.CreateInstanceAndUnwrap(typeof(BuildList).Assembly.FullName, typeof(BuildList).FullName);

            string version = instance.InitRuntime(toolsVersion, frameworkDir, hardReferences);

			if (toolsVersion == "3.0" && version == "2.0")//these use the same build runtime
				version = "3.0";

			if (version != toolsVersion)
				throw new ApplicationException(String.Format("Attempt to create build runtime {0} failed (version {1} loaded)", toolsVersion, version));

			global::Log.Info("Using MSBuild Version {0}", version);
			return instance;
		}

		public void Dispose()
		{
			_engine.UnregisterAllLoggers();
			_engine.UnloadAllProjects();
		}

        private string InitRuntime(string toolsVersion, string frameworkDir, bool hardReferences)
		{
			_items = new ItemLookup();
			_toolsVersion = toolsVersion;
			_engine = new Engine( frameworkDir );

			Log.Verbose("Using build engine: {0}", _engine.GetType().Assembly.FullName);
			string version = _engine.GetType().Assembly.GetName().Version.ToString(2);
			Is35 = version == "3.5";

            if (hardReferences)
            {
                _engine.GlobalProperties.SetProperty("AssemblySearchPaths", "{HintPathFromItem}");
                //{CandidateAssemblyFiles}
                //{HintPathFromItem}
                //{TargetFrameworkDirectory}
                //{Registry:Software\Microsoft\.NetFramework,v2.0,AssemblyFoldersEx}
                //{AssemblyFolders}
                //{GAC}
                //{RawFileName}
                //$(OutputPath)

                _engine.GlobalProperties.SetProperty("NoStdLib", true.ToString());
            }
			if( !Is35 )
				_engine.GlobalProperties.SetProperty("MSBuildToolsPath", frameworkDir);

			ConsoleLogger trace = new Microsoft.Build.BuildEngine.ConsoleLogger(
					LoggerVerbosity.Minimal, ConsoleWrite, ColorSetter, ColorResetter
				);
			trace.SkipProjectStartedText = false;
			trace.ShowSummary = false;
			_engine.RegisterLogger(trace);

			return version;
		}

		#region Log Events

		public event WriteHandler BuildOut;

		private void ConsoleWrite(string text)
		{ if (BuildOut != null) BuildOut(text); }
		private void ColorSetter(ConsoleColor c) { }
		private void ColorResetter() { }

		public delegate bool ProjectLoadedEvent(ProjectInfo project);
		public event ProjectLoadedEvent ProjectLoaded;

		#endregion

		#region Log Levels & Output Options

		public void SetConsoleLevel(LoggerVerbosity consoleLogLevel)
		{
			_engine.RegisterLogger(new ConsoleLogger(consoleLogLevel)); 
		}

		public void SetTextLogFile(string relativeTo, string path, LoggerVerbosity logLevel)
		{
			if (!Path.IsPathRooted(path))
				path = Path.Combine(relativeTo, path);
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			path = Path.GetFullPath(path);

			FileLogger flog = new FileLogger();
			flog.Verbosity = logLevel;
			flog.Parameters = String.Format("logfile={0}", path);
			_engine.RegisterLogger(flog);
		}

		public void SetXmlLogFile(string relativeTo, string path, LoggerVerbosity logLevel)
		{
			if (!Path.IsPathRooted(path))
				path = Path.Combine(relativeTo, path);
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			path = Path.GetFullPath(path);

			XmlLogger xlog = new XmlLogger();
			xlog.Verbosity = logLevel;
			xlog.Parameters = String.Format("logfile={0}", path);
			_engine.RegisterLogger(xlog);
		}

		#endregion

		public void SetProperty(string prop, string val)
		{
			_engine.GlobalProperties.SetProperty(prop, val);
		}

		public string GetProperty(string prop)
		{
			foreach (BuildProperty bp in _engine.GlobalProperties)
			{
				if (StringComparer.OrdinalIgnoreCase.Equals(bp.Name, prop))
					return bp.FinalValue;
			}
			return null;
		}

		public bool LoadProject(string projFile, out ProjectInfo item)
		{
			object[] ctorargs = new object[] { _engine };
			if( Is35 )
				ctorargs = new object[] { _engine, _toolsVersion };

			Project prj = (Project)typeof(Project).InvokeMember(null, BindingFlags.CreateInstance, null, null, ctorargs);

			try
			{
                prj.Load(projFile);
			}
			catch (Exception e)
			{
				Log.Error("Invalid Project: {0}\r\n\t{1}", projFile, e.Message);
				item = null;
				return false;
			}

			item = new ProjectInfo(prj);
			if (ProjectLoaded != null)
			{
				if (!ProjectLoaded(item))
					return false;
			}

			this.Add(item);
			return true;
		}

		public int RunTarget(ProjectInfo[] sorted, string[] targets)
		{
			int errors = 0;

			if (targets == null || targets.Length == 0)
				targets = new string[] {null};

			using (Log.Start("Building targets {0} on {1} projects", String.Join(",",targets), sorted.Length))
			{
                foreach (ProjectInfo proj in sorted)
                {
                    if (targets == null || targets.Length == 0 || (targets.Length == 1 && String.IsNullOrEmpty(targets[0])))
                        targets = proj.DefaultTargets;

                    using (Log.Start("{0} {1}", String.Join(",", targets), proj.AssemblyName))
                    {
                        Log.Info(Path.GetFileName(proj.FullName));
                        //proj.SaveTo(Path.ChangeExtension(proj.ProjectFile, ".tmp"));

                        Project msProject = ((IMSProjectItem)proj).GetMSProject();
                        if (!_engine.BuildProject(msProject, targets, null, BuildSettings.DoNotResetPreviouslyBuiltTargets))
                        {
                            Log.Error("Assembly {0} Failed to build.", proj.AssemblyName);
                            errors++;
                        }
                    }
                }
			}
			return errors;
		}

		public ProjectInfo[] OrderedProjects
		{
			get
			{
				List<ProjectInfo> buildOrder;
				
				if( 0 == PrepareBuild(out buildOrder))
					return buildOrder.ToArray();

				return new ProjectInfo[0];
			}
		}

		public int PrepareBuild(out List<ProjectInfo> buildOrder)
		{
			using (Log.Start("Sorting {0} projects", _items.Count))
			{
				buildOrder = new List<ProjectInfo>();
				Dictionary<string, ProjectInfo> working = new Dictionary<string, ProjectInfo>(StringComparer.Ordinal);

				int maxIter = 50;//just an additional safe-guard
				int lastCount = 0;

				foreach (ProjectInfo pi in _items)
				{
					working.Add(pi.FullName, pi);
				}

				while (working.Count > 0 && lastCount != working.Count && --maxIter > 0)
				{
					lastCount = working.Count;//every loop should peel at least one project out...

					foreach (KeyValuePair<string, ProjectInfo> item in new List<KeyValuePair<string, ProjectInfo>>(working))
					{
						bool canBuild = true;
						foreach (ProjectRef r in item.Value.GetReferences())
						{
							ProjectInfo proj = _items.FindItem(r);
							r.Resolved |= (proj != null);
							if (proj != null && working.ContainsKey(proj.FullName))
							{
								canBuild = false;
								break;
							}
							else if (proj == null && false == r.Resolved &&
								false == _unknown.ContainsKey(r.Assembly.Name))
							{
								_unknown.Add(r.Assembly.Name, true);
							}
						}

						if (canBuild)
						{
							buildOrder.Add(item.Value);
							working.Remove(item.Key);
						}
					}
				}

				if (working.Count > 0)
				{
					Log.Error("Aborted {0} Projects: Possible circular reference in projects.", working.Count);
					return working.Count;
				}
				return 0;
			}
		}

		public ProjectInfo GetReference(ProjectRef reference)
		{
			return _items.FindItem(reference);
		}

		#region ICollection<ProjectInfo> Members

		public void Add(ProjectInfo item)
		{
			_items.Add(item);
		}

		public bool Remove(ProjectInfo item)
		{
			if (!_items.Contains(item))
				return false;

			_items.Remove(item);
			item.Unload();
			return true;
		}

		public void Clear()
		{
			_items.Clear();
		}

		public bool Contains(ProjectInfo item)
		{
			return _items.Contains(item);
		}

		public void CopyTo(ProjectInfo[] array, int arrayIndex)
		{
			_items.CopyTo(array, arrayIndex);
		}

		public int Count { get { return _items.Count; } }

		public ProjectInfo[] ToArray() { return _items.ToArray(); }

		#endregion

		#region IMSProjectItem
		protected interface IMSProjectItem
		{
			Project GetMSProject();
		}

		public abstract class ProjectInfoBase : MarshalByRefObject, IMSProjectItem
		{
			protected readonly Project _project;
			protected readonly string _filename;

			protected ProjectInfoBase(Project project)
			{
				_project = project;
				_filename = _project.FullFileName;
			}

			Project IMSProjectItem.GetMSProject() { return _project; }
		}
		#endregion

	}
}
