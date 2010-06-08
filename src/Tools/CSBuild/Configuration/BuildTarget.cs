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
using System.Xml.Serialization;
using System.ComponentModel;
using CSharpTest.Net.CSBuild.Build;

#pragma warning disable 169

namespace CSharpTest.Net.CSBuild.Configuration
{
    [Serializable]
	public class BuildTarget
	{
        private object[] _all;
        private string _configuration;

		[XmlAttribute("tools")]
		[DefaultValue(FrameworkVersions.v35)]
		public FrameworkVersions Toolset = FrameworkVersions.v35;

		[XmlAttribute("group")]
		[DefaultValue("")]
		public string GroupName;

		[XmlAttribute("configuration")]
		[DefaultValue("Debug")]
		public string Configuration { get { return String.IsNullOrEmpty(_configuration) ? "Debug" : _configuration; } set { _configuration = value; } }

		[XmlAttribute("platform")]
		[DefaultValue(BuildPlatforms.AnyCPU)]
		public BuildPlatforms Platform = BuildPlatforms.AnyCPU;

        [XmlElement("add", typeof(AddProjects))]
        [XmlElement("remove", typeof(RemoveProjects))]
        [XmlElement("reference", typeof(ReferenceFolder))]
        [XmlElement("log", typeof(LogFileOutput))]
		[XmlElement("xml", typeof(XmlFileOutput))]
		[XmlElement("framework", typeof(TargetFramework))]
		[XmlElement("output", typeof(BuildOutput))]
		[XmlElement("property", typeof(BuildProperty))]
        [XmlElement("intermediateFiles", typeof(BuildIntermediateFiles))]
        [XmlElement("define", typeof(BuildDefineConst))]
		[XmlElement("save-project-changes", typeof(SaveProjectChanges))]
		public object[] AllSettings
        {
            get { return _all ?? new object[0]; }
            set { _all = value; }
        }

        #region Accessors ...
        T FindOne<T>() { foreach (T item in FindAll<T>()) return item; return default(T); }
        T[] FindAll<T>()
        {
            List<T> list = new List<T>();
            foreach (object o in AllSettings)
                if (o is T) list.Add((T)o);
            return list.ToArray();
        }
        #endregion

		public SaveProjectChanges SaveProjectChanges { get { return FindOne<SaveProjectChanges>(); } }
		public TargetFramework TargetFramework { get { return FindOne<TargetFramework>(); } }
		public BuildOutput OutputPath { get { return FindOne<BuildOutput>(); } }
        public BuildIntermediateFiles IntermediateFiles { get { return FindOne<BuildIntermediateFiles>(); } }
        public IEnumerable<AddProjects> AddProjects { get { return FindAll<AddProjects>(); } }
        public IEnumerable<RemoveProjects> RemoveProjects { get { return FindAll<RemoveProjects>(); } }
        public IEnumerable<ReferenceFolder> ReferenceFolders { get { return FindAll<ReferenceFolder>(); } }
        public BuildDefineConst[] DefineConstants { get { return FindAll<BuildDefineConst>(); } }
		public BuildProperty[] BuildProperties { get { return FindAll<BuildProperty>(); } }
        public LogFileOutput TextLog { get { return FindOne<LogFileOutput>(); } }
        public XmlFileOutput XmlLog { get { return FindOne<XmlFileOutput>(); } }
    }

    [Serializable]
    public class TargetFramework
    {
        [XmlAttribute("version")]
        public FrameworkVersions Version;
    }

    [Serializable]
	[System.Diagnostics.DebuggerDisplay("{Name} = {Value}")]
	public abstract class BaseBuildSetting<T>
	{
		protected string _name;
		T _value;

		protected BaseBuildSetting() : this(null, default(T)) { }
		protected BaseBuildSetting(string name) : this(name, default(T)) { }
		protected BaseBuildSetting(string name, T def) { _name = name; _value = def; }

		public string Name { get { return _name; } }

		public override string ToString()
		{ return _value.ToString(); }

		[XmlAttribute("value")]
		public T Value { get { return _value; } set { _value = value; } }
	}

    [Serializable]
	public class BuildDefineConst : BaseBuildSetting<string>
	{ public BuildDefineConst() : base("DefineConstants", "TRACE") { } }

    [Serializable]
	[System.Diagnostics.DebuggerDisplay("{Name} = {Path}")]
	public class BuildOutput : BaseFileItem
	{
		public string Name { get { return "OutputPath"; } }
		public override string AbsolutePath
		{ get { return base.AbsolutePath.TrimEnd('\\') + "\\"; } }
	}

    [Serializable]
	public class BuildProperty : BaseBuildSetting<string>
	{
		[XmlAttribute("name")]
		public new string Name { get { return _name; } set { _name = value; } }

		[XmlAttribute("global"), DefaultValue(false)]
		public bool IsGlobal = false;
	}

    [Serializable]
	[System.Diagnostics.DebuggerDisplay("{Name} = {Path}")]
	public class BuildIntermediateFiles : BaseFileItem
	{
		public string Name { get { return "IntermediateOutputPath"; } }
		public override string AbsolutePath
		{ get { return base.AbsolutePath.TrimEnd('\\') + "\\"; } }
	}
}
