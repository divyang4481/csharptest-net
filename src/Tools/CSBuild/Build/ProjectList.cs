#region Copyright 2010 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.IO;
using System.Reflection;

namespace CSharpTest.Net.CSBuild.Build
{
    class ProjectList : IEnumerable<ProjectInfo>
    {
        readonly FrameworkVersions _framework;
		readonly BuildEngine Engine;
        readonly List<ProjectInfo> _projects = new List<ProjectInfo>();
        readonly Dictionary<Guid, int> _byProjectId = new Dictionary<Guid, int>();
        readonly Dictionary<string, int> _byProject = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, int> _byAssembly = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, int> _byOutputFile = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public ProjectList(BuildEngine engine, FrameworkVersions framework)
        {
            this.Engine = engine;
            _framework = framework;
        }

        public BuildOrder GetBuildOrder()
        {
            return new BuildOrder(this, _byProject.Keys);
        }

        public void Clear()
        {
            _projects.Clear();
            _byProjectId.Clear();
            _byProject.Clear();
            _byAssembly.Clear();
            _byOutputFile.Clear();
        }

        public void Add(string projectFile, params string[] addedDependencies)
        {
            ProjectInfo project;
            if (TryGetProject(projectFile, out project))
                return;

			try
			{
				project = Engine.LoadProject(projectFile);

				int ordinal = _projects.Count;
				_projects.Add(project);

				_byProject.Add(project.ProjectFile, ordinal);
				_byOutputFile.Add(project.TargetFullName, ordinal);

				int conflict;
				if (_byProjectId.TryGetValue(project.ProjectGuid, out conflict))
					Log.Warning("Multiple projects with id {0}, using {1}", project.ProjectGuid, _projects[conflict].ProjectFile);
				else
					_byProjectId.Add(project.ProjectGuid, ordinal);

				if (_byAssembly.TryGetValue(project.AssemblyName, out conflict))
					Log.Warning("Multiple projects build assembly {0}, using {1}", project.AssemblyName, _projects[conflict].ProjectFile);
				else
					_byAssembly.Add(project.AssemblyName, ordinal);

				project.Dependencies = addedDependencies;
			}
			catch (OperationCanceledException)
			{ }
        }

        public bool Remove(string projectFile)
        {
            ProjectInfo project;
            if (!TryGetProject(projectFile, out project))
                return false;
            
            _projects.Remove(project);
            _byProject.Remove(project.ProjectFile);
            _byOutputFile.Remove(project.TargetFullName);
            _byProjectId.Remove(project.ProjectGuid);
            _byAssembly.Remove(project.AssemblyName);

            Engine.UnloadProject(project);
            return true;
        }

        public bool Contains(string projectFile)
        { return _byProject.ContainsKey(Path.GetFullPath(projectFile)); }

        internal bool TryGetProject(string projectFile, out ProjectInfo info)
        {
            int ordinal;
            if (_byProject.TryGetValue(Path.GetFullPath(projectFile), out ordinal))
            {
                info = _projects[ordinal];
                return true;
            }
            else
            {
                info = null;
                return false;
            }
        }

        public bool TryGetProject(ReferenceInfo reference, out ProjectInfo info)
        {
            int ordinal = -1;
            if (reference.ProjectGuid.HasValue && _byProjectId.TryGetValue(reference.ProjectGuid.Value, out ordinal))
            { }
            else if (reference.ProjectFile != null && _byProject.TryGetValue(reference.ProjectFile, out ordinal))
            { }
            else if (reference.HintPath != null && _byOutputFile.TryGetValue(reference.HintPath, out ordinal))
            { }
            else if (reference.Assembly != null && reference.Assembly.Name != null &&
                _byAssembly.TryGetValue(reference.Assembly.Name, out ordinal))
            { }
            else
            {
                info = null;
                return false;
            }
            info = _projects[ordinal];
            return true;
        }

        public int Count { get { return _projects.Count; } }

		public IEnumerator<ProjectInfo> GetEnumerator()
		{ return _projects.GetEnumerator(); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{ return ((System.Collections.IEnumerable)_projects).GetEnumerator(); }
	}
}
