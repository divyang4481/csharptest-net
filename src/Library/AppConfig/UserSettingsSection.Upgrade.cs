#region Copyright 2009-2010 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Configuration;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;
using CSharpTest.Net.Utils;
using System.Diagnostics;

namespace CSharpTest.Net.AppConfig
{
	partial class UserSettingsSection
	{
		#region Upgrade Version Properties

		/// <summary>
		/// Describes the version of the application that originally wrote the configuration
		/// </summary>
		[ConfigurationProperty("originalVersion", IsRequired = false)]
		public string OriginalVersion
		{
			get { return (string)base["originalVersion"]; }
			set { base["originalVersion"] = value; }
		}

		/// <summary>
		/// The version that the user settings were previously updagraded from
		/// </summary>
		[ConfigurationProperty("upgradedVersion", IsRequired = false)]
		public string UpgradedVersion
		{
			get { return (string)base["upgradedVersion"]; }
			set { base["upgradedVersion"] = value; }
		}

		/// <summary>
		/// The last date/time the settings were upgraded from the upgradedVersion
		/// </summary>
		[ConfigurationProperty("upgradedDate", IsRequired = false)]
		public string UpgradedDate
		{
			get { return (string)base["upgradedDate"]; }
			set { base["upgradedDate"] = value; }
		}

		#endregion

		/// <summary>
		/// Searches for old user settings from previous versions and copies them into the
		/// configuration provided.
		/// </summary>
		/// <param name="config">The configuration to inspect for previous versions</param>
		/// <param name="settings">The destination UserSettingsSection object</param>
		public static void UpgradeUserSettings(Configuration config, UserSettingsSection settings)
		{
			if (String.IsNullOrEmpty(config.FilePath))
				return;

			try
			{
				//pattern for version strings:  "1.0.1000.325622", etc
				Regex match = RegexPatterns.FullVersion;
				List<String> folders = new List<string>();
				string foundVersionFile = null;
				Version foundVersion = null;

				//breakdown the current configuration path to work from
				string newVersionFile = config.FilePath;
				string fileName = Path.GetFileName(newVersionFile);
				string versionDir = Path.GetDirectoryName(newVersionFile);
				string myVersion = Path.GetFileName(versionDir);
				string allVersions = Path.GetDirectoryName(versionDir);

				//The last directory in the path should be our current version, if not, get outta here
				if (false == match.IsMatch(myVersion))
					return;//not versioned?

				//Get any directories that match a basic wildcard mask
				folders.AddRange(Directory.GetDirectories(allVersions, "*.*.*.*"));

				foreach (string folder in folders)
				{
					//directory must be a numeric version
					if (false == match.IsMatch(Path.GetFileName(folder)))
						continue;
					//user.config file must be located in directory
					if (!File.Exists(Path.Combine(folder, fileName)))
						continue;//no data
					//we are not looking for our own current configuration
					if (StringComparer.OrdinalIgnoreCase.Equals(myVersion, Path.GetFileName(folder)))
						continue;

					//try to parse the version and see if it's the newest, if so hang on to it.
					try
					{
						Version testVersion = new Version(Path.GetFileName(folder));
						if (foundVersion == null || testVersion > foundVersion)
						{
							foundVersion = testVersion;
							foundVersionFile = Path.Combine(folder, fileName);
						}
					}
					catch { }
				}

				//Hopefully we now have a previous file and version, if not he'll just return.
				UpgradeSettingsFromFile(config, settings, foundVersion == null ? null : foundVersion.ToString(), myVersion, foundVersionFile);
			}
			catch (Exception e) { Trace.TraceError("{1}\r\n{0}", e, "Failed to upgrade user settings."); }
		}

		/// <summary>
		/// Forces a read of the configuration file specified and copies the settings from
		/// the old file
		/// </summary>
		private static void UpgradeSettingsFromFile(Configuration config, UserSettingsSection settings, string oldVersionString, string newVersionString, string oldVersionConfig)
		{
			if (String.IsNullOrEmpty(oldVersionConfig) || !File.Exists(oldVersionConfig))
				return;

			//Log.Info("Upgrading settings from version {0} to {1}", oldVersionString, newVersionString);

			//Copy the config file so that we can modify and read
			string tempexename = Path.GetTempFileName();
			string tempconfig = tempexename + ".config";
			try
			{
				//Make a copy and modify to ensure that we have a section declaration
				File.Copy(oldVersionConfig, tempconfig, true);
				ReplaceConfigDeclaration(tempconfig);

				//Read the new configuration
				Configuration upgradeFrom = ConfigurationManager.OpenExeConfiguration(tempexename);
				UserSettingsSection upgradeSettings = upgradeFrom.Sections[SECTION_NAME] as UserSettingsSection;
				if (upgradeSettings != null)
				{
					//copy the settings
					settings.CopyFrom(upgradeSettings);

					//update version upgrade information
					if (!String.IsNullOrEmpty(upgradeSettings.OriginalVersion))
						settings.OriginalVersion = upgradeSettings.OriginalVersion;
					else
						settings.OriginalVersion = oldVersionString;
					settings.UpgradedVersion = oldVersionString;
					settings.UpgradedDate = XmlConvert.ToString(DateTime.Now, XmlDateTimeSerializationMode.RoundtripKind);

					//I guess it worked ;)
					Trace.WriteLine("Settings upgrade successful.");
				}
			}
			finally
			{
				//done with our temp files
				try { File.Delete(tempexename); File.Delete(tempconfig); }
				catch { }
			}
		}

		private static void ReplaceConfigDeclaration(string filename)
		{
			//Replace the declared sections with just our own
			XmlDocument doc = new XmlDocument();
			doc.Load(filename);
			XmlElement sections = doc.SelectSingleNode("/configuration/configSections") as XmlElement;
			if (sections == null)
			{
				sections = doc.CreateElement("configSections");
				Check.NotNull(doc.SelectSingleNode("/configuration")).InsertAfter(sections, null);
			}
			//Just trash whatever is there
			sections.RemoveAll();
			//create our section declaration
			XmlElement sectionDecl = doc.CreateElement("section");
			sectionDecl.SetAttribute("name", "userSettings");
			sectionDecl.SetAttribute("type", String.Format("{0}, {1}", typeof(UserSettingsSection).FullName, typeof(UserSettingsSection).Assembly.FullName));
			//add it back and save
			sections.AppendChild(sectionDecl);
			doc.Save(filename);
		}
	}
}
