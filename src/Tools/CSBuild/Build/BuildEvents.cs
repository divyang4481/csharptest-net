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
using System.ComponentModel;

namespace CSharpTest.Net.CSBuild.Build
{
	delegate void ProjectLoadedEventHandler(BuildEngine engine, ProjectLoadedEventArgs args);
	[System.Diagnostics.DebuggerDisplay("{Project}")]
	class ProjectLoadedEventArgs : CancelEventArgs
	{
		readonly ProjectInfo _project;
		public ProjectLoadedEventArgs(ProjectInfo project)
			: base(false)
		{
			_project = project;
		}

		public ProjectInfo Project { get { return _project; } }
	}

	delegate void ProjectPreBuildEventHandler(BuildEngine engine, ProjectPreBuildEventArgs args);
	[System.Diagnostics.DebuggerDisplay("{Project}")]
	class ProjectPreBuildEventArgs : CancelEventArgs
	{
		readonly ProjectInfo _project;
		string[] _targets;
		public ProjectPreBuildEventArgs(ProjectInfo project, string[] targets)
			: base(false)
		{
			_project = project;
			_targets = targets;
		}

		public ProjectInfo Project { get { return _project; } }
		public String[] Targets { get { return (String[])_targets.Clone(); } set { _targets = Check.NotEmpty(value); } }
	}

	delegate void ProjectPostBuildEventHandler(BuildEngine engine, ProjectPostBuildEventArgs args);
	[System.Diagnostics.DebuggerDisplay("{Project}")]
	class ProjectPostBuildEventArgs : CancelEventArgs
	{
		readonly ProjectInfo _project;
		readonly bool _failed;
		public ProjectPostBuildEventArgs(ProjectInfo project, bool bFailed)
			: base(false)
		{
			_project = project;
			_failed = bFailed;
		}

		public ProjectInfo Project { get { return _project; } }
		public bool BuildFailed { get { return _failed; } }
	}
}
