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
