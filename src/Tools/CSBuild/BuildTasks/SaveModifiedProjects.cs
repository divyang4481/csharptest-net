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
using System.Collections.Generic;
using CSharpTest.Net.CSBuild.Build;
using Project = Microsoft.Build.BuildEngine.Project;
using System.Diagnostics;

namespace CSharpTest.Net.CSBuild.BuildTasks
{
	class SaveModifiedProjects : BuildTask
	{
		readonly TraceLevel _warningLevel;
		public SaveModifiedProjects(TraceLevel warningLevel) { _warningLevel = warningLevel; }

		protected override int Run(BuildEngine engine)
		{
			engine.ProjectPostBuild += new ProjectPostBuildEventHandler(ProjectPostBuild);
			return 0;
		}

		void ProjectPostBuild(BuildEngine engine, ProjectPostBuildEventArgs args)
		{
			Project proj = args.Project.MsProject;
			if (proj.IsDirty)
			{
				Log.Write(_warningLevel, "Project modified: {0}", proj.FullFileName);
				proj.Save(proj.FullFileName);
			}
		}
	}
}
