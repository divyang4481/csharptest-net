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
using CSharpTest.Net.Utils;
using System.Reflection;

namespace CSharpTest.Net.Serialization
{
	/// <summary>
	/// Used with one of the implementations in the StorageClasses namespace, this class
	/// provides rich-type storage on top of the basic INameValueStore string storage
	/// container.
	/// </summary>
	public class Storage
	{
		private readonly INameValueStore _store;
		private PathInfo _context;

		/// <summary>
		/// Constructs a storage wrapper for the given name/value store and sets the initial
		/// path to the provided value.
		/// </summary>
		/// <param name="store">The storage container to use</param>
		public Storage(INameValueStore store) : this(store, null) { }

		/// <summary>
		/// Constructs a storage wrapper for the given name/value store and sets the initial
		/// path to the provided value.
		/// </summary>
		/// <param name="store">The storage container to use</param>
		/// <param name="contextPath">The full context of the storage item, delimit with '/' or '\'</param>
		public Storage(INameValueStore store, string contextPath)
		{
			_store = Check.NotNull(store);
			PathInfo.SetPath(this, contextPath);
		}

		#region private types
		private class PathInfo : IDisposable
		{
			private readonly Storage _store;
			private readonly PathInfo _previous;
			private readonly string _path;

			private PathInfo(Storage store, PathInfo prev, string path) 
			{
				_store = store;
				_previous = prev;

				if (path != null)
					path = path.Trim('/', '\\');//since all paths are absolute, no leading or trailing slash is nessessary
				
				_path = StringUtils.SafeFilePath(path);
			}

			public static PathInfo SetPath(Storage store, string path)
			{
				return (store._context = new PathInfo(store, store._context, path));
			}
			
			public string Path { get { return _path; } }

			public void Dispose()
			{
				_store._context = Check.NotNull(_previous);
			}
		}
		#endregion

		/// <summary>
		/// Retrieves the current context path of the store
		/// </summary>
		public string ContextPath { get { return _context.Path; } }

		/// <summary>
		/// Replaces the context path of the storage, dispose of the returned IDisposable
		/// to restore the Storage's previous ContextPath.
		/// </summary>
		/// <param name="contextPath">The full context path to change to</param>
		/// <returns>Context can be disposed to restore the previous state</returns>
		public IDisposable SetContext(string contextPath) { return PathInfo.SetPath(this, contextPath); }

		#region XXXX GetValue(string name, XXXX defaultValue) overloads

		/// <summary> Retrieves the named defaultValue from the storage </summary>
		public bool GetValue(string name, bool defaultValue) { bool value; if (ReadValue(name, out value)) return value; else return defaultValue; }
		/// <summary> Retrieves the named defaultValue from the storage </summary>
		public byte GetValue(string name, byte defaultValue) { byte value; if (ReadValue(name, out value)) return value; else return defaultValue; }
		/// <summary> Retrieves the named defaultValue from the storage </summary>
		public sbyte GetValue(string name, sbyte defaultValue) { sbyte value; if (ReadValue(name, out value)) return value; else return defaultValue; }
		/// <summary> Retrieves the named defaultValue from the storage </summary>
		public char GetValue(string name, char defaultValue) { char value; if (ReadValue(name, out value)) return value; else return defaultValue; }
		/// <summary> Retrieves the named defaultValue from the storage </summary>
		public DateTime GetValue(string name, DateTime defaultValue) { DateTime value; if (ReadValue(name, out value)) return value; else return defaultValue; }
		/// <summary> Retrieves the named defaultValue from the storage </summary>
		public TimeSpan GetValue(string name, TimeSpan defaultValue) { TimeSpan value; if (ReadValue(name, out value)) return value; else return defaultValue; }
		/// <summary> Retrieves the named defaultValue from the storage </summary>
		public decimal GetValue(string name, decimal defaultValue) { decimal value; if (ReadValue(name, out value)) return value; else return defaultValue; }
		/// <summary> Retrieves the named defaultValue from the storage </summary>
		public double GetValue(string name, double defaultValue) { double value; if (ReadValue(name, out value)) return value; else return defaultValue; }
		/// <summary> Retrieves the named defaultValue from the storage </summary>
		public float GetValue(string name, float defaultValue) { float value; if (ReadValue(name, out value)) return value; else return defaultValue; }
		/// <summary> Retrieves the named defaultValue from the storage </summary>
		public Guid GetValue(string name, Guid defaultValue) { Guid value; if (ReadValue(name, out value)) return value; else return defaultValue; }
		/// <summary> Retrieves the named defaultValue from the storage </summary>
		public Uri GetValue(string name, Uri defaultValue) { Uri value; if (ReadValue(name, out value)) return value; else return defaultValue; }
		/// <summary> Retrieves the named defaultValue from the storage </summary>
		public short GetValue(string name, short defaultValue) { short value; if (ReadValue(name, out value)) return value; else return defaultValue; }
		/// <summary> Retrieves the named defaultValue from the storage </summary>
		public ushort GetValue(string name, ushort defaultValue) { ushort value; if (ReadValue(name, out value)) return value; else return defaultValue; }
		/// <summary> Retrieves the named defaultValue from the storage </summary>
		public int GetValue(string name, int defaultValue) { int value; if (ReadValue(name, out value)) return value; else return defaultValue; }
		/// <summary> Retrieves the named defaultValue from the storage </summary>
		public uint GetValue(string name, uint defaultValue) { uint value; if (ReadValue(name, out value)) return value; else return defaultValue; }
		/// <summary> Retrieves the named defaultValue from the storage </summary>
		public long GetValue(string name, long defaultValue) { long value; if (ReadValue(name, out value)) return value; else return defaultValue; }
		/// <summary> Retrieves the named defaultValue from the storage </summary>
		public ulong GetValue(string name, ulong defaultValue) { ulong value; if (ReadValue(name, out value)) return value; else return defaultValue; }
		/// <summary> Retrieves the named defaultValue from the storage </summary>
		public string GetValue(string name, string defaultValue) { string value; if (ReadValue(name, out value)) return value; else return defaultValue; }
		/// <summary> Retrieves the named defaultValue from the storage </summary>
		public Version GetValue(string name, Version defaultValue) { Version value; if (ReadValue(name, out value)) return value; else return defaultValue; }
		/// <summary> Retrieves the named defaultValue from the storage </summary>
		public object GetValue(string name, Type type, object defaultValue)
		{
			try
			{
				string text;
				object value;
				if (_store.Read(ContextPath, name, out text) && StringUtils.TryParse(text, type, out value))
					return value;
			}
			catch { }
			return defaultValue;
		}

		#endregion
		#region bool TryGetValue(string name, out XXXX value) overloads
		
		/// <summary> Retrieves the named value from the storage </summary>
		public bool TryGetValue(string name, out bool value) { return ReadValue(name, out value); }
		/// <summary> Retrieves the named value from the storage </summary>
		public bool TryGetValue(string name, out byte value) { return ReadValue(name, out value); }
		/// <summary> Retrieves the named value from the storage </summary>
		public bool TryGetValue(string name, out sbyte value) { return ReadValue(name, out value); }
		/// <summary> Retrieves the named value from the storage </summary>
		public bool TryGetValue(string name, out char value) { return ReadValue(name, out value); }
		/// <summary> Retrieves the named value from the storage </summary>
		public bool TryGetValue(string name, out DateTime value) { return ReadValue(name, out value); }
		/// <summary> Retrieves the named value from the storage </summary>
		public bool TryGetValue(string name, out TimeSpan value) { return ReadValue(name, out value); }
		/// <summary> Retrieves the named value from the storage </summary>
		public bool TryGetValue(string name, out decimal value) { return ReadValue(name, out value); }
		/// <summary> Retrieves the named value from the storage </summary>
		public bool TryGetValue(string name, out double value) { return ReadValue(name, out value); }
		/// <summary> Retrieves the named value from the storage </summary>
		public bool TryGetValue(string name, out float value) { return ReadValue(name, out value); }
		/// <summary> Retrieves the named value from the storage </summary>
		public bool TryGetValue(string name, out Guid value) { return ReadValue(name, out value); }
		/// <summary> Retrieves the named value from the storage </summary>
		public bool TryGetValue(string name, out Uri value) { return ReadValue(name, out value); }
		/// <summary> Retrieves the named value from the storage </summary>
		public bool TryGetValue(string name, out short value) { return ReadValue(name, out value); }
		/// <summary> Retrieves the named value from the storage </summary>
		public bool TryGetValue(string name, out ushort value) { return ReadValue(name, out value); }
		/// <summary> Retrieves the named value from the storage </summary>
		public bool TryGetValue(string name, out int value) { return ReadValue(name, out value); }
		/// <summary> Retrieves the named value from the storage </summary>
		public bool TryGetValue(string name, out uint value) { return ReadValue(name, out value); }
		/// <summary> Retrieves the named value from the storage </summary>
		public bool TryGetValue(string name, out long value) { return ReadValue(name, out value); }
		/// <summary> Retrieves the named value from the storage </summary>
		public bool TryGetValue(string name, out ulong value) { return ReadValue(name, out value); }
		/// <summary> Retrieves the named value from the storage </summary>
		public bool TryGetValue(string name, out string value) { return ReadValue(name, out value); }
		/// <summary> Retrieves the named value from the storage </summary>
		public bool TryGetValue(string name, out Version value) { return ReadValue(name, out value); }
		/// <summary> Retrieves the named value from the storage </summary>
		public bool TryGetValue(string name, Type type, out object value)
		{
			value = null;
			try
			{
				string text;
				return (_store.Read(ContextPath, name, out text) && StringUtils.TryParse(text, type, out value));
			}
			catch { return false; }
		}
		
		#endregion
		
		private bool ReadValue<T>(string name, out T value)
		{
			value = default(T);
			try
			{
				string text;
				return (_store.Read(ContextPath, name, out text) && StringUtils.TryParse(text, out value));
			}
			catch { return false; }
		}

		#region void SetValue(string name, bool value) overloads

		/// <summary> Writes/Replaces the named value in the storage </summary>
		public void SetValue(string name, bool value) { WriteValue(name, value); }
		/// <summary> Writes/Replaces the named value in the storage </summary>
		public void SetValue(string name, byte value) { WriteValue(name, value); }
		/// <summary> Writes/Replaces the named value in the storage </summary>
		public void SetValue(string name, sbyte value) { WriteValue(name, value); }
		/// <summary> Writes/Replaces the named value in the storage </summary>
		public void SetValue(string name, char value) { WriteValue(name, value); }
		/// <summary> Writes/Replaces the named value in the storage </summary>
		public void SetValue(string name, DateTime value) { WriteValue(name, value); }
		/// <summary> Writes/Replaces the named value in the storage </summary>
		public void SetValue(string name, TimeSpan value) { WriteValue(name, value); }
		/// <summary> Writes/Replaces the named value in the storage </summary>
		public void SetValue(string name, decimal value) { WriteValue(name, value); }
		/// <summary> Writes/Replaces the named value in the storage </summary>
		public void SetValue(string name, double value) { WriteValue(name, value); }
		/// <summary> Writes/Replaces the named value in the storage </summary>
		public void SetValue(string name, float value) { WriteValue(name, value); }
		/// <summary> Writes/Replaces the named value in the storage </summary>
		public void SetValue(string name, Guid value) { WriteValue(name, value); }
		/// <summary> Writes/Replaces the named value in the storage </summary>
		public void SetValue(string name, Uri value) { WriteValue(name, value); }
		/// <summary> Writes/Replaces the named value in the storage </summary>
		public void SetValue(string name, short value) { WriteValue(name, value); }
		/// <summary> Writes/Replaces the named value in the storage </summary>
		public void SetValue(string name, ushort value) { WriteValue(name, value); }
		/// <summary> Writes/Replaces the named value in the storage </summary>
		public void SetValue(string name, int value) { WriteValue(name, value); }
		/// <summary> Writes/Replaces the named value in the storage </summary>
		public void SetValue(string name, uint value) { WriteValue(name, value); }
		/// <summary> Writes/Replaces the named value in the storage </summary>
		public void SetValue(string name, long value) { WriteValue(name, value); }
		/// <summary> Writes/Replaces the named value in the storage </summary>
		public void SetValue(string name, ulong value) { WriteValue(name, value); }
		/// <summary> Writes/Replaces the named value in the storage </summary>
		public void SetValue(string name, string value) { WriteValue(name, value); }
		/// <summary> Writes/Replaces the named value in the storage </summary>
		public void SetValue(string name, Version value) { WriteValue(name, value); }
		/// <summary> Writes/Replaces the named value in the storage </summary>
		public void SetValue(string name, Type type, object value)
		{
			try
			{
				if (value == null) _store.Delete(ContextPath, name);
				else _store.Write(ContextPath, name, StringUtils.ToString(value));
			}
			catch { }
		}

		#endregion

		private void WriteValue<T>(string name, T value)
		{
			try
			{
				if (value == null) _store.Delete(ContextPath, name);
				else _store.Write(ContextPath, name, StringUtils.ToString(value));
			}
			catch { }
		}

		/// <summary> Removes the named value from the storage </summary>
		public void Delete(string name) { _store.Delete(ContextPath, name); }
	}
}
