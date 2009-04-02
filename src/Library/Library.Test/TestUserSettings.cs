#region Copyright 2009 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using NUnit.Framework;
using CSharpTest.Net.AppConfig;
using System.Configuration;
using CSharpTest.Net.Serialization.StorageClasses;
using System.IO;
using CSharpTest.Net.Utils;

#pragma warning disable 1591
namespace CSharpTest.Net.Library.Test
{
	[TestFixture]
	[Category("TestUserSettings")]
	public partial class TestUserSettings
	{
		#region TestFixture SetUp/TearDown
		[SetUp]
		public virtual void Setup()
		{
			//remove if exists
			Configuration cfg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);

			//attempt to remove all previous configurations
			string versionRoot = Path.GetDirectoryName(Path.GetDirectoryName(cfg.FilePath));
			foreach (string dir in Directory.GetDirectories(versionRoot, "*.*.*.*"))
			{
				if (RegexPatterns.FullVersion.IsMatch(Path.GetFileName(dir)))
					Directory.Delete(dir, true);
			}

			cfg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
			cfg.Sections.Remove("userSettings");
			Assert.IsNull(cfg.GetSection("userSettings"));
			cfg.Save();
			ConfigurationManager.RefreshSection("userSettings");
		}
		#endregion

		[Test]
		public void TestUserSettingsSection()
		{
			UserSettingsSubSection section = new UserSettingsSubSection();

			section.Name = "a";
			Assert.AreEqual("a", section.Name);

			Assert.IsNotNull(section.Settings);

			Assert.IsNull(section["a"]);
			section["a"] = "b";
			Assert.AreEqual("b", section["a"]);
			section["a"] = "c";
			Assert.AreEqual("c", section["a"]);

			UserSettingsSubSection section2 = new UserSettingsSubSection();
			section2.Name = "b";
			Assert.AreNotEqual(0, section.CompareTo(section2));
			section2.Name = "a";
			Assert.AreEqual(0, section.CompareTo(section2));

			section["a"] = "b";
			section.Settings.Add("hello", "world");
			section2.Settings.Add("hello", "universe");
			Assert.AreEqual("universe", section2["hello"]);

			section2.CopyFrom(section);
			Assert.AreEqual("b", section2["a"]);
			Assert.AreEqual("world", section2["hello"]);
		}

		[Test]
		public void TestUserSettingsSectionCollection()
		{
			UserSettingsSubSection a;
			UserSettingsSubSectionCollection coll1 = new UserSettingsSubSectionCollection();
			Assert.AreEqual(ConfigurationElementCollectionType.AddRemoveClearMap, coll1.CollectionType);

			Assert.IsNull(coll1["a"]);
			a = coll1.Add("a");
			Assert.AreEqual(a, coll1["a"]);

			coll1.Remove("a");
			Assert.IsNull(coll1["a"]);

			a = coll1.Add("a");
			Assert.AreEqual(a, coll1["a"]);
			coll1.Clear();
			Assert.IsNull(coll1["a"]);

			a = coll1.Add("a");
			Assert.AreEqual(a, coll1["a"]);

			UserSettingsSubSectionCollection other = new UserSettingsSubSectionCollection();
			other.CopyFrom(coll1);

			Assert.IsNotNull(other["a"]);
			Assert.AreEqual("a", other["a"].Name);
		}

		[Test]
		public void TestSettings()
		{			
			UserSettingsSection settings = UserSettingsSection.DefaultSettings;
			Assert.IsNull(settings);

			Configuration cfg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
			Assert.IsNull(cfg.GetSection("userSettings"));

			settings = UserSettingsSection.UserSettingsFrom(cfg);
			Assert.IsNotNull(settings);
			Assert.IsNotNull(settings.Settings);
			Assert.IsNotNull(settings.Sections);

			settings.Settings.Add("a", "b");
			settings["1"] = "2";
			Assert.AreEqual("2", settings["1"]);
			settings["1"] = "3";
			Assert.AreEqual("3", settings["1"]);
			cfg.Save();

			ConfigurationManager.RefreshSection("userSettings");
			Assert.IsNotNull(UserSettingsSection.DefaultSettings);
			Assert.AreEqual("b", UserSettingsSection.DefaultSettings["a"]);
			Assert.IsNotNull(UserSettingsSection.UserSettings);
			Assert.AreEqual("b", UserSettingsSection.UserSettings["a"]);

			UserSettingsSection copy = new UserSettingsSection();

			settings["a"] = "b";
			settings["1"] = "2";
			settings.Sections.Add("child.test").Settings.Add("AA", "BB");

			copy.CopyFrom(settings);
			Assert.AreEqual("b", settings["a"]);
			Assert.AreEqual("2", settings["1"]);
			Assert.AreEqual("BB", settings.Sections["child.test"]["AA"]);
		}

		[Test]
		public void TestSettingsUpgrade()
		{
			UserSettingsSection settings;
			Configuration cfg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
			Assert.IsNull(cfg.GetSection("userSettings"));

			settings = UserSettingsSection.UserSettingsFrom(cfg);
			settings["a"] = "b";

			string origPath = cfg.FilePath;
			string dir = Path.GetDirectoryName(cfg.FilePath);
			string version = Path.GetFileName(dir).Trim('\\');
			dir = dir.TrimEnd('\\') + "1";

			Directory.CreateDirectory(dir);
			cfg.SaveAs(Path.Combine(dir, Path.GetFileName(origPath)));
			File.Delete(origPath);

			ConfigurationManager.RefreshSection("userSettings");
			cfg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
			Assert.IsNull(cfg.GetSection("userSettings"));
			settings = UserSettingsSection.UserSettingsFrom(cfg);
			Assert.AreEqual("b", settings["a"]);

			Assert.AreEqual(version + "1", settings.OriginalVersion);
			Assert.AreEqual(version + "1", settings.UpgradedVersion);
			string date = DateTime.Now.ToString("yyyy-MM-dd");
			Assert.AreEqual(date, settings.UpgradedDate.Substring(0, date.Length));

		}

		[Test]
		public void TestSettingsOther()
		{
			Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
			UserSettingsSection section = UserSettingsSection.UserSettingsFrom(config);

			section["a"] = "b";

			UserSettingsSubSection sec2;
			if (null == (sec2 = section.Sections["test.child"]))
				sec2 = section.Sections.Add("test.child");

			sec2["a"] = "test.child.b";

			config.Save();

			UserSettingStorage store = new UserSettingStorage();

			string value;
			Assert.IsTrue(store.Read("test.child", "a", out value));
			Assert.AreEqual("test.child.b", value);

			Assert.IsTrue(store.Read("test.child", "a", out value));
			Assert.AreEqual("test.child.b", value);
		}
	}
}
