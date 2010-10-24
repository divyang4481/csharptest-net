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
using System.Text;
using CSharpTest.Net.CSBuild.Configuration;
using CSharpTest.Net.Utils;
using System.IO;
using CSharpTest.Net.CSBuild.Build;

namespace CSharpTest.Net.CSBuild.BuildTasks
{
	[Serializable]
    class ProjectFinder : BuildTask
    {
        Dictionary<String, String> _include = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Dictionary<String, String> _removes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Dictionary<String, List<String>> _depends = new Dictionary<string, List<String>>(StringComparer.OrdinalIgnoreCase);

        public ProjectFinder()
        { }

        public void Add(IEnumerable<AddProjects> addItems)
        {
            AddProjects[] arry = new AddProjects[1];
            foreach (AddProjects add in addItems)
            {
                List<String> dependencies;
                List<String> dependsOn = new List<string>(ForEachProject(add.Depends));
                arry[0] = add;
                foreach (string proj in ForEachProject(arry))
                {
                    _include[proj] = proj;
                    if (dependsOn.Count > 0)
                    {
                        if (!_depends.TryGetValue(proj, out dependencies))
                            _depends.Add(proj, dependencies = new List<string>());
                        dependencies.AddRange(dependsOn);
                    }
                }
            }
        }
        public void Remove(IEnumerable<RemoveProjects> remItems)
        {
            foreach (string proj in ForEachProject(remItems))
                _removes[proj] = proj;
        }

        protected override int Run(BuildEngine engine)
        {
			int errors = 0;
            foreach (string proj in _include.Keys)
            {
                if (false == _removes.ContainsKey(proj))
                {
                    List<string> depends;
                    if (!_depends.TryGetValue(proj, out depends))
                        depends = new List<string>();

					errors += new LoadProject(proj, depends.ToArray()).Perform(engine);
                }
            }
			return errors;
        }

        public IEnumerable<String> ForEachProject<T>(IEnumerable<T> locations) where T: BaseFileItem
        {
			FileList files = new FileList();
			files.FileFound += ProjectsOnly;

            foreach (BaseFileItem location in locations)
            {
                string path = Util.GetFullPath(location.AbsolutePath);
                if (Directory.Exists(path))
                    path = String.Format("{0}\\*.?*proj", path.TrimEnd('\\'));
                files.Add(path);
            }
            return files.GetFileNames();
        }

		static void ProjectsOnly(object sender, FileList.FileFoundEventArgs e)
		{
			bool ignored = true;
			if (StringComparer.OrdinalIgnoreCase.Equals(e.File.Extension, ".csproj"))
				ignored = false;
			else if (StringComparer.OrdinalIgnoreCase.Equals(e.File.Extension, ".vbproj"))
				ignored = false;
			e.Ignore = ignored;
		}
    }
}
