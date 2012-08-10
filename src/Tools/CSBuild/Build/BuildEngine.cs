#region Copyright 2010-2012 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.ComponentModel;
using System.Threading;

#pragma warning disable 618

namespace CSharpTest.Net.CSBuild.Build
{
	[System.Diagnostics.DebuggerDisplay("BuildEngine({Framework}) = {FrameworkPath}")]
	class BuildEngine : IDisposable
    {
        readonly FrameworkVersions _framework;
        readonly string _frameworkPath;
        readonly Engine Engine;
        readonly ProjectList _projects;
        readonly PropertyList _properties;

        public BuildEngine(FrameworkVersions toolsVersion, string frameworkPath)
        {
            _framework = toolsVersion;
            _frameworkPath = frameworkPath;
			Engine = CreateEngine(_framework, _frameworkPath);
            _projects = new ProjectList(this, _framework);
            _properties = new PropertyList(Engine.GlobalProperties);
        }

        public void Dispose()
        {
            Engine.UnregisterAllLoggers();
            Engine.UnloadAllProjects();
        }

		private bool Is35OrLater { get { return Framework != FrameworkVersions.v20 && Framework != FrameworkVersions.v30; } }
		private string FrameworkVersionString { get { return _framework.ToString().TrimStart('v').Insert(1, "."); } }

        public FrameworkVersions Framework { get { return _framework; } }
        public string FrameworkPath { get { return _frameworkPath; } }
        public Version Version { get { return Engine.GetType().Assembly.GetName().Version; } }

		private static Engine CreateEngine(FrameworkVersions toolsVersion, string frameworkPath)
        {
			Engine engine = new Engine(frameworkPath);

			Version fullVersion = engine.GetType().Assembly.GetName().Version;
			string version = fullVersion.ToString(2);
			if (toolsVersion == FrameworkVersions.v30 && version == "2.0")
				version = "3.0";//these use the same build runtime
			if (version.Replace(".", "") != toolsVersion.ToString().TrimStart('v'))
				throw new ApplicationException(String.Format("Expected runtime {0}, found {1}.", toolsVersion, fullVersion));

            Log.Verbose("Using build engine: {0}", engine.GetType().Assembly.FullName);

			if (toolsVersion == FrameworkVersions.v20 || toolsVersion == FrameworkVersions.v30)
				engine.GlobalProperties.SetProperty("MSBuildToolsPath", frameworkPath);

			//<property name="FrameworkSDKDir" value="%ProgramFiles%\Microsoft.NET\SDK\v2.0\" global="true"/>
			//if (!Directory.Exists(engine.GlobalProperties.SetProperty()))
			//{ }


            new MSBuildLog(engine);
            return engine;
        }

		public void UnloadAll()
        {
            Engine.UnregisterAllLoggers();
			Engine.UnloadAllProjects();
			Engine.GlobalProperties.Clear();
			if (!Is35OrLater)
				Engine.GlobalProperties.SetProperty("MSBuildToolsPath", FrameworkPath);

            this.Projects.Clear();
			this.ProjectLoaded = null;
			this.ProjectPreBuild = null;
			this.ProjectPostBuild = null;
		}

		public ProjectInfo LoadProject(string projFile)
		{
			object[] ctorargs = new object[] { Engine };
			if (Is35OrLater)
				ctorargs = new object[] { Engine, FrameworkVersionString };

			Project prj = (Project)typeof(Project).InvokeMember(null, BindingFlags.CreateInstance, null, null, ctorargs);
			try
			{
				prj.Load(projFile);
			}
			catch (Exception e)
			{
				Log.Error("Invalid Project: {0}\r\n\t{1}", projFile, e.Message);
				throw;
			}

			ProjectInfo project = new ProjectInfo(prj);
			if (OnProjectLoaded(project).Cancel)
			{
				UnloadProject(project);
				throw new OperationCanceledException();
			}
			return project;
		}

		public void UnloadProject(ProjectInfo project)
		{
			Engine.UnloadProject(project.MsProject);
		}

        #region Log Levels & Output Options

        public void SetConsoleLevel(LoggerVerbosity consoleLogLevel)
        {
            Engine.RegisterLogger(new ConsoleLogger(consoleLogLevel));
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
            Engine.RegisterLogger(flog);
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
            Engine.RegisterLogger(xlog);
        }

        #endregion

        public ProjectList Projects { get { return _projects; } }
        public PropertyList Properties { get { return _properties; } }

        public int Build(BuildOrder order)
        { return Build(order, null); }
        public int Build(BuildOrder order, string[] targets)
        {
            int errors = 0;

            if (targets == null || targets.Length == 0)
                targets = new string[] { null };

            using (Log.Start("Building targets {0} on {1} projects", String.Join(",", targets), order.Count))
            {
                foreach (ProjectInfo proj in order.Enumerate())
                {
					bool bFailed = false;
                    if (targets == null || targets.Length == 0 || (targets.Length == 1 && String.IsNullOrEmpty(targets[0])))
                        targets = proj.DefaultTargets;
					try
					{
						if (OnProjectPreBuild(proj, ref targets).Cancel)
							continue;

						using (Log.Start("{0} {1} {2}", Framework, String.Join(",", targets), proj.AssemblyName))
						{
							Log.Info(proj.AssemblyName);
							if (!Engine.BuildProject(proj.MsProject, targets, null, BuildSettings.DoNotResetPreviouslyBuiltTargets))
							{
								Log.Error("Assembly {0} Failed to build.", proj.AssemblyName);
                                //proj.MsProject.Save(proj.MsProject.FullFileName + ".failed");
								bFailed = true;
							}
						}
					}
					catch (Exception e)
					{
						System.Diagnostics.Trace.TraceError(e.ToString());
						Log.Error("Exception building assembly {0}: {1}", proj.AssemblyName, e.Message);
						bFailed = true;
					}

					errors += bFailed ? 1 : 0;
					if (OnProjectPostBuild(proj, bFailed).Cancel)
						break;
                }
            }
            return errors;
        }

		static readonly CancelEventArgs ContinueResponse = new CancelEventArgs(false);
		public event ProjectLoadedEventHandler ProjectLoaded;
		protected CancelEventArgs OnProjectLoaded(ProjectInfo proj)
		{
			if (ProjectLoaded != null)
			{
				ProjectLoadedEventArgs args = new ProjectLoadedEventArgs(proj);
				ProjectLoaded(this, args);
				return args;
			}
			return ContinueResponse;
		}

		public event ProjectPreBuildEventHandler ProjectPreBuild;
		protected CancelEventArgs OnProjectPreBuild(ProjectInfo proj, ref string[] targets)
		{
			if (ProjectPreBuild != null)
			{
				ProjectPreBuildEventArgs args = new ProjectPreBuildEventArgs(proj, targets);
				ProjectPreBuild(this, args);
				targets = args.Targets;
				return args;
			}
			return ContinueResponse;
		}

		public event ProjectPostBuildEventHandler ProjectPostBuild;
		protected CancelEventArgs OnProjectPostBuild(ProjectInfo proj, bool bFailed)
		{
			if (ProjectPostBuild != null)
			{
				ProjectPostBuildEventArgs args = new ProjectPostBuildEventArgs(proj, bFailed);
				ProjectPostBuild(this, args);
				return args;
			}
			return ContinueResponse;
		}

    }
}
