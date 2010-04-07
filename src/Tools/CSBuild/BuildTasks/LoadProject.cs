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

namespace CSharpTest.Net.CSBuild.BuildTasks
{
    [Serializable]
    class LoadProject : BuildTask
    {
        readonly string _file;
        readonly string[] _dependencies;
        public LoadProject(string projectFile, params string[] dependencies) 
        {
            _file = projectFile;
            _dependencies = dependencies;
        }

        protected override int Run(BuildEngine engine)
        {
            Log.Verbose("Loading {0}", _file);
			try
			{
				engine.Projects.Add(_file, _dependencies);
				return 0;
			}
			catch (ApplicationException ae) 
			{
				System.Diagnostics.Trace.TraceError(ae.ToString());
				Log.Error(ae.Message); 
			}
			catch (Exception error)
			{
				Log.Error(error);
			}
			return 1;
        }
    }
}
