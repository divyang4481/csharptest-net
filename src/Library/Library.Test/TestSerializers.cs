#region Copyright 2009-2014 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using Microsoft.Win32;
using System.Configuration;
using System.IO;

using CSharpTest.Net.Serialization.StorageClasses;
using CSharpTest.Net.Serialization;
using CSharpTest.Net.AppConfig;

#pragma warning disable 1591
namespace CSharpTest.Net.Library.Test
{
	public abstract class BaseTestSerializers<T> where T : INameValueStore, new()
	{
		protected readonly INameValueStore Store;

		protected BaseTestSerializers() : this(new T()) { }
		protected BaseTestSerializers(T store) { Store = store; }

		[Test]
		public void TestStorage()
		{
			string value;

			Store.Write(null, "<?a", "b");

			Assert.IsTrue(Store.Read(null, "<?a", out value));
			Assert.AreEqual("b", value);

			Assert.IsTrue(Store.Read(String.Empty, "<?a", out value));
			Assert.AreEqual("b", value);

			Store.Write(null, "<?a", "b");

			Assert.IsTrue(Store.Read(null, "<?a", out value));
			Assert.AreEqual("b", value);

			Assert.IsTrue(Store.Read(String.Empty, "<?a", out value));
			Assert.AreEqual("b", value);

			Store.Write(">?1/\\", "<?a", "c");

			Assert.IsTrue(Store.Read(String.Empty, "<?a", out value));
			Assert.AreEqual("b", value);

			Assert.IsTrue(Store.Read(">?1/\\", "<?a", out value));
			Assert.AreEqual("c", value);

			Store.Delete(">?1/\\", "<?a");
			Assert.IsFalse(Store.Read(">?1/\\", "<?a", out value));

			Store.Delete(null, "<?a");
			Assert.IsFalse(Store.Read(null, "<?a", out value));
			Assert.IsFalse(Store.Read(String.Empty, "<?a", out value));
		}

	}

	#region Storage derived tests
	[TestFixture]
	public class TestRegistryStorage : BaseTestSerializers<RegistryStorage>
	{
		[TestFixtureSetUp]
		public void Setup()
		{
			try {
				Registry.CurrentUser.DeleteSubKeyTree(Path.GetDirectoryName(Constants.RegistrySoftwarePath));
			} catch (ArgumentException) { }
		}
	}
	[TestFixture]
	public class TestIsolatedStorage : BaseTestSerializers<IsolatedStorage>
	{ }
	[TestFixture]
	public class TestFileStorage : BaseTestSerializers<FileStorage>
	{
		[TestFixtureSetUp]
		public void Setup()
		{
			if(Directory.Exists(Constants.ApplicationData)) Directory.Delete(Constants.ApplicationData, true);
			if (Directory.Exists(Constants.LocalApplicationData)) Directory.Delete(Constants.LocalApplicationData, true);
		}
	}
	[TestFixture]
	public class TestAppSettingStorage : BaseTestSerializers<AppSettingStorage>
	{
		[TestFixtureSetUp]
		public void Setup()
		{
			Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			foreach (string key in config.AppSettings.Settings.AllKeys)
				config.AppSettings.Settings.Remove(key);
		}
	}
	[TestFixture]
	public class TestUserSettingStorage : BaseTestSerializers<UserSettingStorage>
	{
		[TestFixtureSetUp]
		public void Setup()
		{
			Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
			if (null != config.GetSection("userSettings"))
				config.Sections.Remove("userSettings");
			config.Save();
		}
	}
	[TestFixture]
	public class TestDictionaryStorage : BaseTestSerializers<DictionaryStorage>
	{
		[TestFixtureSetUp]
		public void Setup()
		{ AppDomain.CurrentDomain.SetData(typeof(DictionaryStorage).FullName, null); }
	}
	[TestFixture]
	public class TestDictionaryStorage2 : BaseTestSerializers<DictionaryStorage>
	{
		public TestDictionaryStorage2()
			: base(new DictionaryStorage(new Dictionary<string, string>()))
		{ }
	}
	#endregion

}
