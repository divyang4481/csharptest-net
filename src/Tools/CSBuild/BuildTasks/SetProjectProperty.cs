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
using CSharpTest.Net.CSBuild.Configuration;

namespace CSharpTest.Net.CSBuild.BuildTasks
{
    [Serializable]
	class SetProjectProperty : BuildTask
    {
		readonly string _name, _value;
		public SetProjectProperty(MSProp name, string value)
			: this(name.ToString(), value)
		{ }
		public SetProjectProperty(string name, string value)
        {
			_name = name;
			_value = value;
        }
        protected override int Run(BuildEngine engine)
        {
			foreach (ProjectInfo pi in engine.Projects)
				pi.Properties.SetValue(_name, _value);
            return 0;
        }
    }
}
