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
using System.IO;
using System.Collections.Generic;
using Microsoft.Build.Framework;
using CSharpTest.Net.CSBuild.Configuration;
using CSharpTest.Net.CSBuild.Build;
using CSharpTest.Net.CSBuild.BuildTasks;

namespace CSharpTest.Net.CSBuild
{
	class CmdLineBuilder : IDisposable
	{
		readonly System.Threading.Thread _thread;
		readonly CSBuildConfig _config;
		readonly LoggerVerbosity? _verbosity;
		readonly string[] _propertySets;
		readonly string[] _targetNames;
		readonly string _groups;
		volatile int _errors = 0;

		public CmdLineBuilder(CSBuildConfig config, LoggerVerbosity? verbosity, string groups, string[] targetNames, string[] propertySets)
		{
			_config = config;
			_verbosity = verbosity;
			_groups = groups;
			_targetNames = targetNames;
			_propertySets = propertySets;
			_thread = new System.Threading.Thread(this.Build);
		}

        public void Dispose()
        {
            Complete(TimeSpan.Zero);
        }

		public void Start()
		{
            _thread.Name = "MSBuild";
            _thread.SetApartmentState(System.Threading.ApartmentState.STA);
            _thread.Start();
		}

		public int Complete(TimeSpan timeout)
		{
            if (_thread.IsAlive && !_thread.Join(timeout))
            {
                Log.Error("The build has exceeded the timeout limit.");
                _thread.Abort();
                _thread.Join(TimeSpan.FromSeconds(10));
                throw new TimeoutException();
            }
			return _errors;
		}

		private void Build()
		{
			BuildDomain domain = null;
			BuildTarget[] targets = Check.NotEmpty(_config.Targets);

			try
			{
				foreach (BuildTarget target in targets)
				{
					if (!String.IsNullOrEmpty(_groups))
					{
						if (_groups.IndexOf(target.GroupName, StringComparison.OrdinalIgnoreCase) < 0 &&
							target.GroupName.IndexOf(_groups, StringComparison.OrdinalIgnoreCase) < 0)
							continue;
					}

					using (Log.Start("Target {0}, {1}, {2}", target.Toolset, target.Configuration, target.Platform))
					{
						if (domain == null || domain.ToolsVersion != target.Toolset)
						{
							if (domain != null) domain.Dispose();
							domain = BuildDomain.CreateInstance(target.Toolset);
						}

						_errors += domain.Perform(new BuildTasks.SetContinue(_config.Options.ContinueOnError));
						if (_verbosity.HasValue)
							_errors += domain.Perform(new BuildTasks.ConsoleOutput(_verbosity.Value));

						TargetBuilder build = new TargetBuilder(_config, target, _propertySets, _targetNames);
						_errors += domain.Perform(build);
						_errors += domain.Perform(new UnloadAll());
					}

					if (_errors > 0 && !_config.Options.ContinueOnError)
						break;
				}
			}
			finally
			{
				if (domain != null)
					domain.Dispose();
			}
		}
	}
}
