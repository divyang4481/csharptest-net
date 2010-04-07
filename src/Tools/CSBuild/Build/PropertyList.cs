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
using Microsoft.Build.BuildEngine;

namespace CSharpTest.Net.CSBuild.Build
{
	class PropertyList : IEnumerable<KeyValuePair<string, string>>
    {
        BuildPropertyGroup _properties;
        public PropertyList(BuildPropertyGroup properties)
        {
            _properties = properties;
        }

		public string this[MSProp name]
        {
            get { return GetValue(name.ToString()); }
            set { SetValue(name.ToString(), value); }
        }

        public virtual string GetValue(string name)
        {
            foreach (BuildProperty prop in _properties)
                if (StringComparer.OrdinalIgnoreCase.Equals(prop.Name, name))
                    return prop.Value;
            return null;
        }

        public virtual void SetValue(string name, string value)
        {
            Log.Verbose("Changing property {0} to {1}", name, value);
			if (value != null)
			{
				if(value != GetValue(name))
					_properties.SetProperty(name, value);
			}
			else
				_properties.RemoveProperty(name);
        }

        public virtual void Remove(string name)
        {
            _properties.RemoveProperty(name);
        }

		public bool IsExpression(string value)
		{
			return value.Contains("$(");
		}

        public virtual IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            foreach (BuildProperty prop in _properties)
                yield return new KeyValuePair<string, string>(prop.Name, prop.Value);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        { foreach (KeyValuePair<string, string> prop in this) yield return prop; }
    }
}
