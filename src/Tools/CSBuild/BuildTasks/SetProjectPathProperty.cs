#region Copyright 2010-2011 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using CSharpTest.Net.Utils;

namespace CSharpTest.Net.CSBuild.BuildTasks
{
	[Serializable]
	class SetProjectPathProperty : BuildTask
	{
		readonly MSProp _name;
		readonly string _value;
		public SetProjectPathProperty(MSProp name, string value)
		{
			_name = name;
			_value = value;
			if (_value != null && !_value.EndsWith("\\"))
				_value += "\\";
		}
		protected override int Run(BuildEngine engine)
		{
			foreach (ProjectInfo pi in engine.Projects)
			{
				string value = _value;
				if (!pi.Properties.IsExpression(_value))
					value = FileUtils.MakeRelativePath(pi.ProjectFile, _value);

				pi.Properties[_name] = value;
			}
			return 0;
		}
	}
}
