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
using CSharpTest.Net.CSBuild.Build;
using System.IO;

namespace CSharpTest.Net.CSBuild.BuildTasks
{
	[Serializable]
	class SetSolutionDir : BuildTask
    {
		public SetSolutionDir()
        { }
        protected override int Run(BuildEngine engine)
        {
			foreach (ProjectInfo item in engine.Projects)
			{
				if (!String.IsNullOrEmpty(item.Properties[MSProp.SolutionDir]))
					continue;

				//To attempt to gracefully handle those those that use SolutionDir in build rules...
				string solutiondir = Path.GetDirectoryName(item.ProjectFile);
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
				item.Properties[MSProp.SolutionDir] = solutiondir;
			}
            return 0;
        }
    }
}
