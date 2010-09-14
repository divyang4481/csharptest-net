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
using System.IO;
using System.Runtime.InteropServices;
using CSharpTest.Net.Delegates;
using CSharpTest.Net.Utils;
using Microsoft.Build.BuildEngine;

#pragma warning disable 618 //Obsolete warning on new Engine()

namespace CSharpTest.Net.CustomTool
{
	public class ProjectVisitor
	{
		private FileList _projects;
		public ProjectVisitor(string[] projects)
		{
			_projects = new FileList();
			_projects.ProhibitedAttributes = FileAttributes.Hidden;
			_projects.FileFound += new EventHandler<FileList.FileFoundEventArgs>(FileFound);
			_projects.Add(projects);
		}

		public int Count { get { return _projects.Count; } }

		void FileFound(object sender, FileList.FileFoundEventArgs e)
		{
			e.Ignore = false == StringComparer.OrdinalIgnoreCase.Equals(e.File.Extension, ".csproj");
		}

		public void VisitProjects(Action<Project> visitor)
		{
			Engine e = new Engine(RuntimeEnvironment.GetRuntimeDirectory());
			e.GlobalProperties.SetProperty("MSBuildToolsPath", RuntimeEnvironment.GetRuntimeDirectory());

			foreach (FileInfo file in _projects)
			{
				Project prj = new Project(e);
				try
				{
					prj.Load(file.FullName);
				}
				catch (Exception ex)
				{
					Log.Warning("Unable to open project: {0}", file);
					Log.Verbose(ex.ToString());
				}

				visitor(prj);
				e.UnloadProject(prj);
			}
		}

		public void VisitProjectItems(Action<Project, BuildItemGroup, BuildItem> visitor)
		{
			VisitProjects(
				delegate(Project p)
				{
					foreach (BuildItemGroup group in p.ItemGroups)
						foreach (BuildItem item in group)
						{
							if (group.IsImported) break;
							if (item.IsImported) continue;
							visitor(p, group, item);
						}
				}
			);
		}

		public void VisitProjectProperties(Action<Project, BuildPropertyGroup, BuildProperty> visitor)
		{
			VisitProjects(
				delegate(Project p)
				{
					foreach (BuildPropertyGroup group in p.PropertyGroups)
						foreach (BuildProperty item in group)
						{
							if (group.IsImported) break;
							if (item.IsImported) continue;
							visitor(p, group, item);
						}
				}
			);
		}
	}
}
