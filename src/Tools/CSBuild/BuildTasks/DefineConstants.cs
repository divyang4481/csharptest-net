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

namespace CSharpTest.Net.CSBuild.BuildTasks
{
    [Serializable]
    class DefineConstants : BuildTask
    {
		string[] _defines;

		public DefineConstants(params string[] defines)
        {
			_defines = Check.NotEmpty(defines);
        }
        protected override int Run(BuildEngine engine)
        {
			foreach (ProjectInfo pi in engine.Projects)
			{
				Dictionary<string, string> constants = new Dictionary<string, string>();
				List<string> names = new List<string>();
				names.AddRange(String.Format("{0}", pi.Properties[MSProp.DefineConstants]).Split(';'));
				foreach (string name in _defines)
					if (name != null)
						names.AddRange(name.Split(';'));

				//Add
				foreach (string name in names)
				{
					string tmp = (name ?? String.Empty).Trim();
					if (tmp.Length > 0 && tmp[0] != '-')
						constants[tmp] = tmp[0] == '+' ? tmp.Substring(1) : tmp;
				}
				//Remove
				foreach (string name in names)
				{
					string tmp = (name ?? String.Empty).Trim();
					if (tmp.Length > 0 && tmp[0] == '-')
						constants.Remove(tmp.Substring(1));
				}

				names.Clear();
				names.AddRange(constants.Keys);
				names.Sort();
				pi.Properties[MSProp.DefineConstants] = String.Join(";", names.ToArray());
			}
            return 0;
        }
    }
}
