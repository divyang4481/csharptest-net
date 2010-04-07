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
using System.IO;
using System.Collections.Generic;
using Microsoft.Build.BuildEngine;
using CSharpTest.Net.CSBuild.Configuration;
using System.Reflection;
using CSharpTest.Net.Utils;

namespace CSharpTest.Net.CSBuild.Implementation
{
	[System.Diagnostics.DebuggerDisplay("{FullName}")]
	partial class ProjectInfo : BuildList.ProjectInfoBase
	{
		public ProjectInfo(Project project) : base(project)
		{
		}

		public string FullName { get { return _filename; } }

		public void Unload()
		{
			Log.Warning("Unloading project: {0}", this.FullName);
			_project.ParentEngine.UnloadProject(_project);
		}

		public bool IsDirty { get { return _project.IsDirty; } }

		public void Save() { SaveTo(_project.FullFileName); }
		public void SaveTo(string path)
		{
			_project.Save(path);
		}

		#region Read-Only accessors for common properties we need:
		public string ProjectFile { get { return Path.GetFullPath(_project.FullFileName); } }
		public Guid ProjectGuid { get { return new Guid(GetProperty("ProjectGuid")); } }
		public string AssemblyName { get { return GetProperty("AssemblyName"); } }
		public string ProjectDir { get { return GetProperty("ProjectDir"); } }
		public string OutDir { get { return GetProperty("OutDir"); } }
		public string TargetFileName { get { return GetProperty("TargetFileName"); } }
		public string OutputType { get { return GetProperty("OutputType"); } }
		public string[] DefaultTargets { get { return _project.DefaultTargets.Split(';'); } }

		public string OutputPath { get { return GetProperty("OutputPath"); } set { SetProperty("OutputPath", value); } }
		public string IntermediateFiles { get { return GetProperty("IntermediateOutputPath"); } set { SetProperty("IntermediateOutputPath", value); } }
		public string TargetFrameworkVersion { get { return GetProperty("TargetFrameworkVersion"); } set { SetProperty("TargetFrameworkVersion", value); } }
		#endregion

		#region Project Path Routines

		public string AbsoluteOutputPath
		{
			get
			{
				String projectDir = this.ProjectDir;
				String outDir = this.OutDir;
				String targetFileName = this.TargetFileName;

				string fullOutputPath = outDir;

				if (!Path.IsPathRooted(fullOutputPath))
					fullOutputPath = Path.Combine(projectDir, fullOutputPath);

				fullOutputPath = Path.Combine(fullOutputPath, targetFileName);

				return Path.GetFullPath(fullOutputPath);
			}
		}

		public string MakeProjectRelativePath(string path)
		{
			return FileUtils.MakeRelativePath(_project.FullFileName, path);
		}

		public bool GetProjectPath(ref string path)
		{
			if (path == null) return false;
			if (!Path.IsPathRooted(path))
				path = Path.Combine(ProjectDir, path);

			path = Path.GetFullPath(path);
			if (File.Exists(path))
				return true;
			return false;
		}

		#endregion

		bool ConditionsMatch(string cond1, string cond2)
		{
			if (String.IsNullOrEmpty(cond1) && String.IsNullOrEmpty(cond2))
				return true;

			if (String.IsNullOrEmpty(cond1) || String.IsNullOrEmpty(cond2))
				return false;

			cond1 = cond1.Replace(" ", "").Trim();
			cond2 = cond2.Replace(" ", "").Trim();
			return StringComparer.OrdinalIgnoreCase.Equals(cond1, cond2);
		}
	}
}
