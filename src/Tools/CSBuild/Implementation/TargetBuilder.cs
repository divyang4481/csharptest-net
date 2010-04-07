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
using CSharpTest.Net.CSBuild.Configuration;
using CSharpTest.Net.Utils;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using FrameworkVersions = CSharpTest.Net.CSBuild.Build.FrameworkVersions;

namespace CSharpTest.Net.CSBuild.Implementation
{
	class xTargetBuilder : IDisposable
	{
		static readonly string MyAssemblyName = typeof(BuildList).Assembly.GetName().Name;
		readonly CSBuildConfig _config;
		readonly BuildTarget _target;
		readonly BuildList _build;

		public xTargetBuilder(CSBuildConfig config, BuildTarget target, string[] properties)
		{
			_config = config;
			_target = Check.NotNull(target);
			_build = BuildList.CreateInstance(_target.Toolset.ToString().Insert(2, ".").TrimStart('v'), _target.FrameworkPath,
                GetReferenceFolders().Length > 0);
			
			new TargetBuilderDispatch(this, _build);

			if (config.Console != null)
				_build.SetConsoleLevel(config.Console.Level);
            if (_target.TextLog != null)
                _build.SetTextLogFile(Environment.CurrentDirectory, _target.TextLog.AbsolutePath, _target.TextLog.Level);
            if (_target.XmlLog != null)
                _build.SetXmlLogFile(Environment.CurrentDirectory, _target.XmlLog.AbsolutePath, _target.XmlLog.Level);

			_build.SetProperty("Configuration", _target.Configuration);
			_build.SetProperty("Platform", _target.Platform.ToString());

			foreach (string property in properties)
			{
				string[] values = property.Split( new char [] { '=', ':' }, 2);
				if (values.Length == 2)
				{
					Log.Verbose("Setting property {0} = {1}", values[0], values[1]);
					_build.SetProperty(values[0], values[1]);
				}
			}

			Log.Info("Using Configuration = {0}", _build.GetProperty("Configuration"));
			Log.Info("Using Platform = {0}", _build.GetProperty("Platform"));
		}

		void IDisposable.Dispose()
		{
			_build.Dispose();
		}

		public void AddProject(FileInfo projFile)
		{
			using (Log.Start("Loading {0}", projFile.FullName))
			{
				ProjectInfo item;
				if (!_build.LoadProject(projFile.FullName, out item))
					return;
			}
		}

		public void AddDependencies(IEnumerable<KeyValuePair<FileInfo, List<FileInfo>>> depends)
		{
			Dictionary<string, ProjectInfo> projectFiles = new Dictionary<string, ProjectInfo>(StringComparer.OrdinalIgnoreCase);
			foreach (ProjectInfo prj in _build.ToArray())
				projectFiles.Add(prj.ProjectFile, prj);
			foreach (KeyValuePair<FileInfo, List<FileInfo>> kv in depends)
			{
				if (kv.Value.Count == 0) continue;
				ProjectInfo project;
				if (projectFiles.TryGetValue(kv.Key.FullName, out project))
				{
					List<ProjectInfo> items = new List<ProjectInfo>();
					foreach (FileInfo dep in kv.Value)
					{
						ProjectInfo projDep;
						if (projectFiles.TryGetValue(dep.FullName, out projDep))
							items.Add(projDep);
					}
					project.AddDependencies(items.ToArray());
				}
			}
		}

		private bool AllowProject(ProjectInfo item, out string reason)
		{
			//We can't build ourselves...
			if (item.AssemblyName == MyAssemblyName)
			{
				reason = null;
				return false;
			}

			string curTargetFramework = item.TargetFrameworkVersion;
			if (curTargetFramework != null)
			{
				FrameworkVersions curTargetVer = (FrameworkVersions)Enum.Parse(typeof(FrameworkVersions), curTargetFramework.Replace(".", ""));

				//If they do not want to build for each framework version, then we do not build lower versions
				if (_target.Toolset < curTargetVer)
				{
					//reason = "Target framework is " + item.TargetFrameworkVersion.ToString();
					//return false;
				}
			}

			reason = null;
			return true;
		}

		public int Build(params string[] targets)
        {
            int errors = 0;
			ProjectInfo[] buildOrder = VerifyReferences();

            foreach (ProjectInfo proj in buildOrder)
                errors += FixupProjectForBuild(proj);

            errors += _build.RunTarget(buildOrder, targets);
            return errors;
		}

		private int FixupProjectForBuild(ProjectInfo item)
		{
            int errors = 0;
            errors += SetSolutionDir(item);
            errors += ApplyBuildRules(item);
            errors += RestrictFrameworkReferences(item);
            errors += RebindProjectReferences(item);
            return errors;
		}

		int SetSolutionDir(ProjectInfo item)
		{
			//To attempt to gracefully handle those those that use SolutionDir in build rules...
			string solutiondir = Path.GetDirectoryName(item.FullName);
			DirectoryInfo parent = new DirectoryInfo(solutiondir);
			while (parent != null)
			{
				if (parent.GetFiles("*.sln").Length > 0)
				{
					solutiondir = parent.FullName;
					break;
				}
				parent = parent.Parent;
			}
			if (!solutiondir.EndsWith(@"\"))
				solutiondir += @"\";
			_build.SetProperty("SolutionDir", solutiondir);
            return 0;
		}

        int RestrictFrameworkReferences(ProjectInfo item)
		{
			string targetFrmWrk = item.TargetFrameworkVersion;
			if (!String.IsNullOrEmpty(targetFrmWrk))
			{
				Version targetVer = new Version(targetFrmWrk.TrimStart('v'));
				foreach (ProjectRef r in item.GetReferences())
				{
					if (!String.IsNullOrEmpty(r.RequiresVersion))
					{
						if (new Version(r.RequiresVersion) > targetVer)
						{
							item.RemoveReference(r);
							continue;
						}
					}
					if (r.Assembly.Version != null &&r.Assembly.Name.StartsWith("System.", StringComparison.OrdinalIgnoreCase))
					{
						Version ver = new Version(r.Assembly.Version.Major, r.Assembly.Version.Minor);
						if (r.Assembly.Version > targetVer)
						{
							item.RemoveReference(r);
						}
					}
				}
			}
            return 0;
		}

        int ApplyBuildRules(ProjectInfo item)
		{
            if(_target.TargetFramework != null)
    			item.TargetFrameworkVersion =  _target.TargetFramework.Version.ToString().Insert(2, ".");
			if (_target.IntermediateFiles != null)
				item.IntermediateFiles = _target.IntermediateFiles.AbsolutePath;
			if (_target.OutputPath != null)
				item.OutputPath = _target.OutputPath.AbsolutePath;

			if (_target.DefineConstants != null && _target.DefineConstants.Length > 0)
			{
				StringBuilder sb = new StringBuilder();
				sb.Append(item.GetProperty("DefineConstants"));
				foreach (BuildDefineConst set in _target.DefineConstants)
				{
					if (sb.Length > 0) sb.Append(',');
					sb.Append(set.Value);
				}
				item.SetProperty("DefineConstants", sb.ToString());
			}
            return 0;
		}

        Dictionary<string, string> _foundReferences = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        AssemblyName __mscorlib = new AssemblyName("mscorlib");
        string __mscorlibPath = null;

        int RebindProjectReferences(ProjectInfo item)
		{
            if (GetReferenceFolders().Length == 0)
                return 0;

            string file = null;
            if (__mscorlibPath == null)
                __mscorlibPath = FindPath(__mscorlib);
            item.AddReference(null, __mscorlib, null, __mscorlibPath, false, false);

            int errors = 0;
            List<ProjectRef> refs = new List<ProjectRef>(item.GetReferences());
			foreach (ProjectRef r in refs)
			{
                if (!_foundReferences.TryGetValue(r.Assembly.Name, out file))
                {
                    if (r.Resolved)
                    {
                        ProjectInfo proj = _build.GetReference(r);
                        if (proj == null)
                            throw new ApplicationException(String.Format("Unable to remove unkown project reference {0}", r.Assembly));

                        file = proj.AbsoluteOutputPath;
                    }
                    else
                    {
                        file = FindPath(r.Assembly);
                    }
                    _foundReferences[r.Assembly.Name] = file;
                }
                if (file != null)
                {
                    item.ResolveReference(r, file, r.CopyLocal, r.SpecificVersion);
                    Log.Verbose("Project reference {0} changed to file {1}", r.Assembly.Name, file);
                }
                else
                {
                    Log.Error("Unable to locate reference {0}", r.Assembly);
                    errors++;
                }
			}
            return errors;
		}

        private string FindPath(AssemblyName assembly)
        {
            foreach (string folder in GetReferenceFolders())
            {
                if (!Directory.Exists(folder))
                    continue;
                string[] found = Directory.GetFiles(folder, assembly.Name + ".dll");
                if (found.Length == 0)
                    found = Directory.GetFiles(folder, assembly.Name + ".exe");
                if (found.Length == 0)
                    found = Directory.GetFiles(folder, assembly.Name);
                if (found.Length == 0)
                    continue;
                return found[0];
            }
            return null;
        }

		private string[] _validReferenceFolders;
        string[] GetReferenceFolders()
		{
			if (_validReferenceFolders != null)
				return _validReferenceFolders;

			List<String> folders = new List<String>();
			foreach (ReferenceFolder rf in _config.Projects.ReferenceFolders)
				folders.Add(rf.AbsolutePath);
			foreach (ReferenceFolder rf in _target.ReferenceFolders)
				folders.Add(rf.AbsolutePath);
			folders.Reverse();
			return _validReferenceFolders = folders.ToArray();
		}

		ProjectInfo[] VerifyReferences()
		{
			return _build.OrderedProjects;
		}

		#region TargetBuilderDispatch
	
		class TargetBuilderDispatch : MarshalByRefObject
		{
			xTargetBuilder _instance;
			public TargetBuilderDispatch(xTargetBuilder inst, BuildList build)
				: this(inst)
			{
				build.ProjectLoaded += ProjectLoaded;
			}

			public TargetBuilderDispatch(xTargetBuilder inst)
			{
				_instance = inst;
			}

			bool ProjectLoaded(ProjectInfo item)
			{
				string reason;
				if (!_instance.AllowProject(item, out reason))
				{
					if (reason != null)
						Log.Warning("Ignoring project {0}, reason = {1}", item.AssemblyName, reason);
					return false;
				}
				return true;
			}
		}

		#endregion
	}
}
