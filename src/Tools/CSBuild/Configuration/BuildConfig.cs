#region Copyright 2008-2014 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Xml;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Collections.Generic;
using Microsoft.Build.Framework;

#pragma warning disable 1591

namespace CSharpTest.Net.CSBuild.Configuration
{
    [Serializable]
	[XmlRoot("CSBuildConfig")]
	public class CSBuildConfig
	{
		private object[] _all;

        [XmlElement("options", typeof(BuildOptions))]
		[XmlElement("projects", typeof(ProjectIncludes))]
		[XmlElement("target", typeof(BuildTarget))]
		public object[] AllSettings
		{
			get { return _all ?? new object[0]; }
			set { _all = value; }
		}
		
		#region Accessors ...
        T FindOne<T>() 
        { 
            T[] items = FindAll<T>(); 
            if( items.Length==1) return items[0]; 
            if(items.Length==0) return default(T);
            throw new ApplicationException(String.Format("You cannot specify {0} more than once.", typeof(T).Name));
        }
		T[] FindAll<T>()
		{
			List<T> list = new List<T>();
			foreach (object o in AllSettings)
				if (o is T) list.Add((T)o);
			return list.ToArray();
		}

        public BuildOptions Options { get { return FindOne<BuildOptions>() ?? new BuildOptions(); } }
		public ProjectIncludes Projects { get { return FindOne<ProjectIncludes>() ?? new ProjectIncludes(); } }
		public BuildTarget[] Targets { get { return FindAll<BuildTarget>(); } }
		#endregion


        internal static Dictionary<string, string> ToDictionary(string[] rawtext)
        {
            Dictionary<string, string>  properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (string property in rawtext)
            {
                string[] values = property.Split(new char[] { '=', ':' }, 2);
                if (values.Length == 2)
                {
                    string key = values[0].Trim();
                    string val = Environment.ExpandEnvironmentVariables(values[1]).Trim();
                    properties[key] = val;
                }
            }
            return properties;
        }
	}
}
