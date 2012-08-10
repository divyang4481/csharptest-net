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
using CSharpTest.Net.CSBuild.Build;

namespace CSharpTest.Net.CSBuild.BuildTasks
{
    [Serializable]
    class BuildAll : BuildTask
    {
        string[] _targets;

        public BuildAll(params string[] targets)
        {
            _targets = targets;
        }

		public string[] Targets { get { return (string[])_targets.Clone(); } set { _targets = value; } }

        protected override int Run(BuildEngine engine)
        {
			int errors;

			BuildOrder order;
            using (Log.Start("Creating build order for {0} projects.", engine.Projects.Count))
                order = engine.Projects.GetBuildOrder();
            using (Log.Start("Building all projects"))
                errors = engine.Build(order, _targets);

			return errors;
        }
    }
}
