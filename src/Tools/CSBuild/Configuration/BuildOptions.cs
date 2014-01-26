#region Copyright 2010-2014 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using Microsoft.Build.Framework;
using System.ComponentModel;
using System.Diagnostics;

namespace CSharpTest.Net.CSBuild.Configuration
{
    [Serializable]
    public class BuildOptions
    {
        const string DefaultLogPath = @"%AppData%\CSBuild\CSBuild.log";
        private object[] _allItems;

		[XmlAttribute("timeout-hours")]
		[DefaultValue(4)]
		public int TimeoutHours = 4;

        [XmlElement("logfile", typeof(LogFilePath))]
        [XmlElement("import", typeof(ImportOptionsPath))]
		[XmlElement("global-property", typeof(BuildProperty))]
		[XmlElement("strict-references", typeof(BuildStrictReferences))]
		[XmlElement("no-standard-references", typeof(NoStdReferences))]
		[XmlElement("force-file-references", typeof(ProjectReferencesToFile))]
		[XmlElement("continue-on-error", typeof(BuildContinueOnError))]
		[XmlElement("save-project-changes", typeof(SaveProjectChanges))]
        [XmlElement("console", typeof(ConsoleOutputLevel))]
        public object[] AllItems { get { return _allItems ?? new object[0]; } set { _allItems = value; } }

		private bool GetOption<T>() where T : IEnabledItem
		{ T item; return GetOption(out item); }
		private bool GetOption<T>(out T item) where T : IEnabledItem
		{
			item = default(T);
			foreach (object o in AllItems)
				if (o is T) { item = (T)o; return ((T)o).IsEnabled(); }
			return false;
		}

        public ImportOptionsPath ImportOptionsFile { get { foreach (object o in AllItems) if (o is ImportOptionsPath) return (ImportOptionsPath)o; return null; } }
        public bool ForceReferencesToFile { get { return GetOption<ProjectReferencesToFile>(); } }
		public bool NoStdReferences { get { return GetOption<NoStdReferences>(); } }
        public bool StrictReferences { get { return GetOption<BuildStrictReferences>(); } }
		public bool SaveProjectChanges(out TraceLevel level)
        {
			SaveProjectChanges cfg;
			if (GetOption(out cfg)) 
			{ 
				level = cfg.LogLevel; 
				return true; 
			}
			level = TraceLevel.Off;
            return false;
        }

		public bool ContinueOnError { get { return GetOption<BuildContinueOnError>(); } }
		public IEnumerable<BuildProperty> GlobalProperties { get { foreach (object o in _allItems) if (o is BuildProperty) yield return o as BuildProperty; } }

        public string LogPath(IDictionary<string, string> namedValues)
        {
			LogFilePath log;
			if (GetOption(out log))
				return log.AbsolutePath(namedValues);
			if (log == null)
                return Util.MakeAbsolutePath(OutputRelative.FixedPath, DefaultLogPath, namedValues);
			return null;
        }

		public bool ConsoleEnabled { get { return ConsoleLevel != null; } }
        public LoggerVerbosity? ConsoleLevel
        {
            get
            {
				ConsoleOutputLevel conout;
				if (GetOption(out conout))
					return conout.Level;
				if( conout == null)
	                return LoggerVerbosity.Minimal;
				return null;
            }
        }
    }

	interface IEnabledItem { bool IsEnabled(); }

    [Serializable]
    public class ImportOptionsPath : BaseFileItem
    { }

    [Serializable]
	public class LogFilePath : BaseFileItem, IEnabledItem
	{
		[XmlAttribute("enabled")]
		[DefaultValue(true)]
		public bool Enabled;
		bool IEnabledItem.IsEnabled() { return Enabled; }
	}
	[Serializable]
	public class ConsoleOutputLevel : BaseOutput, IEnabledItem
	{
		[XmlAttribute("enabled")]
		[DefaultValue(true)]
		public bool Enabled;
		bool IEnabledItem.IsEnabled() { return Enabled; }
	}
	[Serializable]
	public class SaveProjectChanges : IEnabledItem
	{
		[XmlAttribute("enabled")]
		public bool Enabled = false;
		[XmlAttribute("level")]
		[DefaultValue(TraceLevel.Warning)]
		public TraceLevel LogLevel = TraceLevel.Warning;
		bool IEnabledItem.IsEnabled() { return Enabled; }
	}
	[Serializable]
	public abstract class BooleanBuildOption : IEnabledItem
	{
		[XmlAttribute("enabled")]
		[DefaultValue(true)]
		public bool Enabled = true;
		bool IEnabledItem.IsEnabled() { return Enabled; }
	}
	[Serializable]
	public class BuildStrictReferences : BooleanBuildOption { }
	[Serializable]
	public class NoStdReferences : BooleanBuildOption { }
	[Serializable]
	public class ProjectReferencesToFile : BooleanBuildOption { }
	[Serializable]
	public class BuildContinueOnError : BooleanBuildOption { }
}
