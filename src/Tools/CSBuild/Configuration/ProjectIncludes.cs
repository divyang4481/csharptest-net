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
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.ComponentModel;

namespace CSharpTest.Net.CSBuild.Configuration
{
    [Serializable]
	public class ProjectIncludes
	{
		[XmlElement("add", typeof(AddProjects))]
		[XmlElement("remove", typeof(RemoveProjects))]
		[XmlElement("reference", typeof(ReferenceFolder))]
		public object[] AllItems;

		private IEnumerable<T> GetItems<T>()
		{
			if (AllItems == null) yield break;
			foreach (object obj in AllItems)
				if (obj is T) yield return (T)obj;
		}

		public IEnumerable<AddProjects> AddProjects
		{ get { return GetItems<AddProjects>(); } }
		public IEnumerable<RemoveProjects> RemoveProjects
		{ get { return GetItems<RemoveProjects>(); } }
		public IEnumerable<ReferenceFolder> ReferenceFolders
		{ get { return GetItems<ReferenceFolder>(); } }
	}

    [Serializable]
	public class AddProjects : BaseFileItem
	{
        DependsUpon[] _depends;
        [XmlElement("dependsOn")]
        public DependsUpon[] Depends
        {
            get { return _depends ?? new DependsUpon[0]; }
            set { _depends = value; }
        }
	}

    [Serializable]
	public class DependsUpon : BaseFileItem
	{
	}

    [Serializable]
	public class ReferenceFolder : BaseFileItem
	{
        [XmlAttribute("recursive")][DefaultValue(false)]
        public bool Recursive = false;
    }

    [Serializable]
	public class RemoveProjects : BaseFileItem
	{
	}
}
