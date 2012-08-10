#region Copyright 2009-2012 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.IO;
using System.IO.IsolatedStorage;
using System.Text;
using System.Configuration;

using CSharpTest.Net.Utils;
using CSharpTest.Net.AppConfig;

namespace CSharpTest.Net.Serialization.StorageClasses
{
	/// <summary>
	/// Stores values in the registry at HKCU\Software\{Company}\{Product} path.
	/// </summary>
	public class RegistryStorage : INameValueStore
	{
        private readonly Microsoft.Win32.RegistryKey _hiveRoot;
		private readonly string _pathRoot;

		/// <summary>
		/// Stores values in the registry at HKCU\Software\{Company}\{Product} path.
		/// </summary>
        public RegistryStorage()
            : this(Constants.RegistrySoftwarePath)
		{ }

        /// <summary>
        /// Stores values in the registry at HKCU\Software\{Company}\{Product} path.
        /// </summary>
        public RegistryStorage(string hkcuPath)
            : this(Microsoft.Win32.Registry.CurrentUser, hkcuPath)
        { }

        /// <summary>
        /// Stores values in the registry at path.
        /// </summary>
        public RegistryStorage(Microsoft.Win32.RegistryKey hiveRoot, string path)
        {
            _hiveRoot = hiveRoot;
            _pathRoot = path;
        }

		private string FullPath(string path)
		{
			if (String.IsNullOrEmpty(path)) return _pathRoot;
			return Path.Combine(_pathRoot, StringUtils.SafeFilePath(path));
		}

		/// <summary>
		/// returns true if the property was successfully retireved into the output
		/// variable 'value'
		/// </summary>
		public bool Read(string path, string name, out string value)
		{
			path = FullPath(path);
			name = Check.NotEmpty(StringUtils.SafeFileName(name));
            using (Microsoft.Win32.RegistryKey key = _hiveRoot.CreateSubKey(path))
				value = key.GetValue(name, null) as string;
			return value != null;
		}

		/// <summary>
		/// Writes the given property by name
		/// </summary>
		public void Write(string path, string name, string value)
		{
			path = FullPath(path);
			name = Check.NotEmpty(StringUtils.SafeFileName(name));
            using (Microsoft.Win32.RegistryKey key = _hiveRoot.CreateSubKey(path))
				key.SetValue(name, value, Microsoft.Win32.RegistryValueKind.String);
		}

		/// <summary>
		/// Removes a property from the storage by name
		/// </summary>
		public void Delete(string path, string name)
		{
			path = FullPath(path);
			name = Check.NotEmpty(StringUtils.SafeFileName(name));
            using (Microsoft.Win32.RegistryKey key = _hiveRoot.CreateSubKey(path))
				key.DeleteValue(name, false);
		}
	}

	/// <summary>
	/// Stores values in the IsolatedStorage for the application in {Company}\{Product} path.
	/// </summary>
	public class IsolatedStorage : INameValueStore
	{
		private readonly string _pathRoot;
		
		private enum StorageType { App, Domain, Assembly }
		private readonly StorageType _type;
		
        /// <summary>
		/// Stores values in the IsolatedStorage for the application in {Company}\{Product} path.
		/// </summary>
		public IsolatedStorage()
            : this(Path.Combine(Constants.CompanyName, Constants.ProductName))
		{ }

        /// <summary>
        /// Stores values in the IsolatedStorage for the application in path.
        /// </summary>
        public IsolatedStorage(string relativePath)
        {
            _pathRoot = relativePath;
    
            IsolatedStorageFile file = null;
			try { file = IsolatedStorageFile.GetUserStoreForApplication(); _type = StorageType.App; }
			catch(IsolatedStorageException)
			{
				try { file = IsolatedStorageFile.GetUserStoreForDomain(); _type = StorageType.Domain; }
				catch (IsolatedStorageException)
				{ file = IsolatedStorageFile.GetUserStoreForAssembly(); _type = StorageType.Assembly; }
			}
			finally
			{ if (file != null) file.Dispose(); }
		}

		private string FullPath(string path)
		{
			if (String.IsNullOrEmpty(path)) return _pathRoot;
			return Path.Combine(_pathRoot, StringUtils.SafeFilePath(path));
		}

		private IsolatedStorageFile Open()
		{
			switch (_type)
			{
				case StorageType.App: return IsolatedStorageFile.GetUserStoreForApplication();
				case StorageType.Domain: return IsolatedStorageFile.GetUserStoreForDomain();
				//case StorageType.Assembly: 
				default:
				return IsolatedStorageFile.GetUserStoreForAssembly();
			}

		}

		/// <summary>
		/// returns true if the property was successfully retireved into the output
		/// variable 'value'
		/// </summary>
		public bool Read(string path, string name, out string value)
		{
			path = FullPath(path);
			using (IsolatedStorageFile store = Open())
			{
				store.CreateDirectory(Check.NotEmpty(path));
				name = Check.NotEmpty(StringUtils.SafeFileName(name));
				value = null;
				string filename = Path.Combine(path, name);
				if (store.GetFileNames(filename).Length == 1)
				{
					using (StreamReader rdr = new StreamReader(new IsolatedStorageFileStream(
							filename, FileMode.Open, FileAccess.Read, System.IO.FileShare.Read, store)))
						value = rdr.ReadToEnd();
				}
				return value != null;
			}
		}

		/// <summary>
		/// Writes the given property by name
		/// </summary>
		public void Write(string path, string name, string value)
		{
			path = FullPath(path);
			using (IsolatedStorageFile store = Open())
			{
				store.CreateDirectory(Check.NotEmpty(path));
				name = Check.NotEmpty(StringUtils.SafeFileName(name));
				using (StreamWriter wtr = new StreamWriter(new IsolatedStorageFileStream(
						Path.Combine(path, name), FileMode.Create, FileAccess.Write, System.IO.FileShare.Write, store)))
				{
					wtr.Write(value);
					wtr.Flush();
				}
			}
		}

		/// <summary>
		/// Removes a property from the storage by name
		/// </summary>
		public void Delete(string path, string name)
		{
			path = FullPath(path);
			using (IsolatedStorageFile store = Open())
			{
				store.CreateDirectory(Check.NotEmpty(path));
				name = Check.NotEmpty(StringUtils.SafeFileName(name));
				string filename = Path.Combine(path, name);
				if (store.GetFileNames(filename).Length == 1)
					store.DeleteFile(filename);
			}
		}
	}

	/// <summary>
	/// Stores values in the local %AppData% folder in the path {Company}\{Product}.
	/// </summary>
	public class FileStorage : INameValueStore
	{
		private readonly string _pathRoot;

		/// <summary>
		/// Stores values in the local %AppData% folder in the path {Company}\{Product}.
		/// </summary>
		public FileStorage()
            : this(Constants.LocalApplicationData)
		{ }

        /// <summary>
        /// Stores values in the local %AppData% folder in the path {Company}\{Product}.
        /// </summary>
        public FileStorage(string pathRoot)
        {
            _pathRoot = pathRoot;
        }

		private string FullPath(string path)
		{
			if (String.IsNullOrEmpty(path)) return _pathRoot;
			return Path.Combine(_pathRoot, StringUtils.SafeFilePath(path));
		}

		/// <summary>
		/// returns true if the property was successfully retireved into the output
		/// variable 'value'
		/// </summary>
		public bool Read(string path, string name, out string value)
		{
			path = FullPath(path);
			name = Check.NotEmpty(StringUtils.SafeFileName(name));
			value = null;
			string fileName = Path.Combine(path, name);
			if (File.Exists(fileName))
				value = File.ReadAllText(fileName, Encoding.UTF8);
			return value != null;
		}

		/// <summary>
		/// Writes the given property by name
		/// </summary>
		public void Write(string path, string name, string value)
		{
			path = FullPath(path);
			Directory.CreateDirectory(path);
			name = Check.NotEmpty(StringUtils.SafeFileName(name));
			File.WriteAllText(Path.Combine(path, name), value, Encoding.UTF8);
		}

		/// <summary>
		/// Removes a property from the storage by name
		/// </summary>
		public void Delete(string path, string name)
		{
			path = FullPath(path);
			name = Check.NotEmpty(StringUtils.SafeFileName(name));
			string fileName = Path.Combine(path, name);
			if (File.Exists(fileName))
				File.Delete(fileName);
		}
	}

	/// <summary>
	/// Stores values in the local application's configuration section: "AppSettings"
	/// </summary>
	public class AppSettingStorage : INameValueStore
	{
		/// <summary>
		/// Provides syncronization across instances of AppSettingsStorage classes
		/// modifying the configuration file(s)
		/// </summary>
		protected static readonly object Sync = new object();

		/// <summary>
		/// Stores values in the local application's configuration
		/// </summary>
		public AppSettingStorage()
		{
		}

		#region Protected Virtual Members
		/// <summary>
		/// Creates the full name of the item from path and name
		/// </summary>
		protected virtual string MakePath(string path, string name)
		{
			if (String.IsNullOrEmpty(path)) return Check.NotEmpty(name);
			return String.Format("{0}::{1}", Check.NotEmpty(path), Check.NotEmpty(name));
		}

		/// <summary>
		/// Opens a configuration section and returns the key/value collection associated.
		/// </summary>
		protected virtual KeyValueConfigurationCollection Open(string path, out Configuration config)
		{
			config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			return config.AppSettings.Settings;
		}
		#endregion

		/// <summary>
		/// returns true if the property was successfully retireved into the output
		/// variable 'value'
		/// </summary>
		public virtual bool Read(string path, string name, out string value)
		{
			value = null;
			Configuration config;
			KeyValueConfigurationCollection settings = Open(path, out config);
			KeyValueConfigurationElement item = settings[MakePath(path, name)];
			if (item != null) value = item.Value;
			return value != null;
		}

		/// <summary>
		/// Writes the given property by name
		/// </summary>
		public virtual void Write(string path, string name, string value)
		{
			lock (Sync)
			{
				Configuration config;
				KeyValueConfigurationCollection settings = Open(path, out config);
				KeyValueConfigurationElement item = settings[MakePath(path, name)];
				if (item != null)
					item.Value = value;
				else
					settings.Add(MakePath(path, name), value);

				config.Save(ConfigurationSaveMode.Minimal, false);
			}
		}

		/// <summary>
		/// Removes a property from the storage by name
		/// </summary>
		public virtual void Delete(string path, string name)
		{
			lock (Sync)
			{
				Configuration config;
				KeyValueConfigurationCollection settings = Open(path, out config);
				settings.Remove(MakePath(path, name));
				config.Save(ConfigurationSaveMode.Minimal, false);
			}
		}
	}

	/// <summary>
	/// Stores values in the local configuration section: "userSettings"
	/// </summary>
	public class UserSettingStorage : AppSettingStorage
	{
		private ConfigurationUserLevel _cfgUser;
		/// <summary>
		/// Stores values in the local application's configuration
		/// </summary>
		public UserSettingStorage() : this(ConfigurationUserLevel.PerUserRoamingAndLocal) { }

		/// <summary>
		/// Stores values in the local application's configuration
		/// </summary>
		public UserSettingStorage(ConfigurationUserLevel configUserLevel)
		{
			_cfgUser = configUserLevel;
		}

		#region Virtual Overload Members
		/// <summary>
		/// Creates the full name of the item from path and name
		/// </summary>
		protected override string MakePath(string path, string name)
		{
			return Check.NotEmpty(name);
		}

		/// <summary>
		/// Opens a configuration section and returns the key/value collection associated.
		/// </summary>
		protected override KeyValueConfigurationCollection Open(string path, out Configuration config)
		{
			config = ConfigurationManager.OpenExeConfiguration(_cfgUser);
			UserSettingsSection userSettings = UserSettingsSection.UserSettingsFrom(config);

			if (String.IsNullOrEmpty(path))
				return userSettings.Settings;

			if( null == userSettings.Sections[path] )
				userSettings.Sections.Add(path);

			return userSettings.Sections[path].Settings;
		}
		#endregion
	}

	/// <summary>
	/// Stores values in a IDictionary, by default this dictionary is
	/// placed in the current AppDomain data slot to provide data that
	/// is consistant across instances of DictionaryStorage when no
	/// dictionary is provided to the constructor.
	/// </summary>
	public class DictionaryStorage : INameValueStore
	{
		IDictionary<string, string> _storage;

		/// <summary>
		/// Constructs a DictionarySTorage with a specified dictionary object
		/// </summary>
		public DictionaryStorage(IDictionary<string, string> dictionary)
		{ _storage = Check.NotNull(dictionary); }
		
		/// <summary>
		/// dictionary is retrieved/placed in the current AppDomain data 
		/// slot to provide data that is consistant across instances of 
		/// DictionaryStorage.
		/// </summary>
		public DictionaryStorage()
		{
			lock (typeof(DictionaryStorage))
			{
				_storage = AppDomain.CurrentDomain.GetData(this.GetType().FullName) as IDictionary<string, string>;
				if (_storage == null)
					AppDomain.CurrentDomain.SetData(this.GetType().FullName, _storage = new Dictionary<string, string>());
			}
		}

		/// <summary>
		/// returns true if the property was successfully retireved into the output
		/// variable 'value'
		/// </summary>
		public bool Read(string path, string name, out string value)
		{
			name = String.IsNullOrEmpty(path) ? name : String.Format("{0}::{1}", path, Check.NotEmpty(name));
			lock (_storage)
				return _storage.TryGetValue(name, out value);
		}

		/// <summary>
		/// Writes the given property by name
		/// </summary>
		public void Write(string path, string name, string value)
		{
			name = String.IsNullOrEmpty(path) ? name : String.Format("{0}::{1}", path, Check.NotEmpty(name));
			lock (_storage)
				_storage[name] = value;

		}

		/// <summary>
		/// Removes a property from the storage by name
		/// </summary>
		public void Delete(string path, string name)
		{
			name = String.IsNullOrEmpty(path) ? name : String.Format("{0}::{1}", path, Check.NotEmpty(name));
			lock (_storage)
				_storage.Remove(name);
		}
	}
}