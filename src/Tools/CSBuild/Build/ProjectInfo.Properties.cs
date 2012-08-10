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
using Microsoft.Build.BuildEngine;

namespace CSharpTest.Net.CSBuild.Build
{
    class ProjectPropertyList : PropertyList
    {
        readonly Project _project;

        public ProjectPropertyList(Project project)
            : base(project.EvaluatedProperties)
        {
            _project = project;
        }

        public override string GetValue(string name)
        {
            return _project.GetEvaluatedProperty(name);
        }

		delegate string ReadValue(string name);

        public override void SetValue(string property, string value)
        {
            if (value == null)
            {
                Remove(property);
                return;
            }

			bool expression = IsExpression(value);
			ReadValue read = this.GetValue;
			if (expression) read = base.GetValue;

            Log.Verbose("Changing property {0} to {1}", property, value);
            bool changesMade = false;
			string original = read(property);
            if (original == value)
                return;

            foreach (BuildPropertyGroup grp in _project.PropertyGroups)
            {
                if (grp.IsImported) continue;
                foreach (BuildProperty prop in grp)
                {
                    if (!prop.IsImported && StringComparer.OrdinalIgnoreCase.Equals(prop.Name, property))
                    {
                        prop.Value = value;
                        changesMade = true;
                    }
                }
            }

			if (!changesMade || value != read(property))
			{
				if (expression) base.SetValue(property, value);
				else _project.SetProperty(property, value);
			}
			string testNewValue = read(property);
            if (value != testNewValue)
                throw new ApplicationException(String.Format("Unable to modify property value {0}", property));
        }

        public override void Remove(string property)
        {
            foreach (BuildPropertyGroup grp in _project.PropertyGroups)
            {
                if (grp.IsImported) continue;
                foreach (BuildProperty prop in grp)
                {
                    if (!prop.IsImported && StringComparer.OrdinalIgnoreCase.Equals(prop.Name, property))
                    {
                        Log.Verbose("Removing property {0} = {1}", prop.Name, prop.Value);
                        grp.RemoveProperty(prop);
                    }
                }
            }
        }
    }
}
