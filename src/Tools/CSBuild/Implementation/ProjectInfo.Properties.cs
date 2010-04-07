#region Copyright 2008 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using Microsoft.Build.BuildEngine;

namespace CSharpTest.Net.CSBuild.Implementation
{
	partial class ProjectInfo
	{
		public string GetProperty(string property)
		{
			return _project.GetEvaluatedProperty(property);
		}

		public string SetProperty(string property, string value)
		{
			//bool changesMade = false;
			string original = GetProperty(property);
			if (original == value)
				return original;

			foreach (BuildPropertyGroup grp in _project.PropertyGroups)
			{
				foreach (BuildProperty prop in grp)
				{
					if (!prop.IsImported && StringComparer.OrdinalIgnoreCase.Equals(prop.Name, property))
					{
						Log.Verbose("Changing property {0} from {1} to {2}", prop.Name, prop.Value, value);
						prop.Value = value;
						//changesMade = true;
					}
				}
			}

			_project.SetProperty(property, value);
			string testNewValue = GetProperty(property);
			if (value != testNewValue)
				throw new ApplicationException(String.Format("Unable to modify property value {0}", property));
			return GetProperty(property);
		}

		bool DeleteProperty(string property)
		{
			bool changesMade = false;
			foreach (BuildPropertyGroup grp in _project.PropertyGroups)
			{
				foreach (BuildProperty prop in grp)
				{
					if (!prop.IsImported && StringComparer.OrdinalIgnoreCase.Equals(prop.Name, property))
					{
						Log.Verbose("Removing property {0} = {1}", prop.Name, prop.Value);
						grp.RemoveProperty(prop);
						changesMade = true;
					}
				}
			}
			return changesMade;
		}
	}
}