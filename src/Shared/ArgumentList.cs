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

namespace CSharpTest.Net.Utils
{
	/// <summary>
	/// This is a private class as the means of sharing is to simply include the source file not
	/// reference a library.
	/// </summary>
	internal class ArgumentList : System.Collections.ObjectModel.KeyedCollection<string, ArgumentList.Item>
	{
		#region Static Configuration Options
		static StringComparer _defaultCompare = StringComparer.OrdinalIgnoreCase;
		static char[] _prefix = new char[] { '/', '-' };
		static char[] _delim = new char[] { ':', '=' };
		static readonly string[] EmptyList = new string[0];

		internal static StringComparer DefaultComparison
		{
			get { return _defaultCompare; }
			set
			{
				if (value == null) throw new ArgumentNullException();
				_defaultCompare = value;
			}
		}

		internal static char[] PrefixChars
		{
			get { return (char[])_prefix.Clone(); }
			set
			{
				if (value == null) throw new ArgumentNullException();
				if (value.Length == 0) throw new ArgumentOutOfRangeException();
				_prefix = (char[])value.Clone();
			}
		}
		internal static char[] NameDelimeters
		{
			get { return (char[])_delim.Clone(); }
			set
			{
				if (value == null) throw new ArgumentNullException();
				if (value.Length == 0) throw new ArgumentOutOfRangeException();
				_delim = (char[])value.Clone();
			}
		}
		#endregion Static Configuration Options

		readonly List<string> _unnamed;
		/// <summary>
		/// Initializes a new instance of the ArgumentList class using the argument list provided
		/// </summary>
		public ArgumentList(params string[] arguments) : this(DefaultComparison, arguments) { }
		public ArgumentList(StringComparer comparer, params string[] arguments)
			: base(comparer, 0)
		{
			_unnamed = new List<string>();
			this.AddRange(arguments);
		}

		/// <summary>
		/// Returns a list of arguments that did not start with a character in the PrefixChars
		/// static collection.  These arguments can be modified by the methods on the returned
		/// collection, or you set this property to a new collection (a copy is made).
		/// </summary>
		public ICollection<string> Unnamed
		{
			get { return _unnamed; }
			set 
			{
				_unnamed.Clear();
				if (value != null)
					_unnamed.AddRange(value);
			}
		}

		/// <summary>
		/// Parses the strings provided for switch names and optionally values, by default in one
		/// of the following forms: "/name=value", "/name:value", "-name=value", "-name:value"
		/// </summary>
		public void AddRange(params string[] arguments)
		{
			if (arguments == null) throw new ArgumentNullException();

			foreach (string arg in arguments)
			{
				string cleaned = CleanArgument(arg);//strip quotes
				string name, value;
				if (TryParseNameValue(cleaned, out name, out value))
					Add(name, value);
				else
					_unnamed.Add(cleaned);
			}
		}

		/// <summary>
		/// Adds a name/value pair to the collection of arguments, if value is null the name is
		/// added with no values.
		/// </summary>
		public void Add(string name, string value)
		{
			if (name == null)
				throw new ArgumentNullException();

			Item item;
			if (!TryGetValue(name, out item))
				base.Add(item = new Item(name));

			if (value != null)
				item.Add(value);
		}

		/// <summary>
		/// Returns true if the value was found by that name and set the output value
		/// </summary>
		public bool TryGetValue(string name, out Item value)
		{
			if (name == null)
				throw new ArgumentNullException();

			if (Dictionary != null)
				return Dictionary.TryGetValue(name, out value);
			value = null;
			return false;
		}

		public Item SafeGet(string name)
		{
			Item result;
			if (TryGetValue(name, out result))
				return result;
			return new Item(name, null);
		}

		#region Protected / Private operations...

		private string CleanArgument(string argument)
		{
			if (argument == null) throw new ArgumentNullException();
			if (argument.Length >= 2 && argument[0] == '"' && argument[argument.Length - 1] == '"')
				argument = argument.Substring(1, argument.Length - 2).Replace("\"\"", "\"");
			return argument;
		}

		private bool TryParseNameValue(string argument, out string name, out string value)
		{
			name = value = null;

			if (0 != argument.IndexOfAny(_prefix, 0, 1))
				return false;

			name = argument.Substring(1);
			int endName = name.IndexOfAny(_delim, 1);

			if (endName > 0)
			{
				value = name.Substring(endName + 1);
				name = name.Substring(0, endName);
			}

			return true;
		}

		/// <summary>
		/// Abract override for extracting key
		/// </summary>
		protected override string GetKeyForItem(ArgumentList.Item item)
		{
			return item.Name;
		}

		#endregion

		#region Item class used for collection
		/// <summary>
		/// This is a single named argument within an argument list collection, this
		/// can be implicitly assigned to a string, or a string[] array
		/// </summary>
		internal class Item : System.Collections.ObjectModel.Collection<string>
		{
			protected readonly string _name;
			protected readonly List<string> _values;

			public Item(string name, params string[] items)
				: this(new List<string>(), name, items) { }

			private Item(List<string> impl, string name, string[] items)
				: base(impl)
			{
				if (name == null)
					throw new ArgumentNullException();

				_name = name;
				_values = impl;
				if (items != null)
					_values.AddRange(items);
			}

			public string Name { get { return _name; } }

			public string Value
			{
				get { return _values.Count > 0 ? _values[0] : null; }
				set
				{
					_values.Clear();
					if (value != null)
						_values.Add(value);
				}
			}

			public string[] Values
			{
				get { return _values.ToArray(); }
				set
				{
					_values.Clear();
					if (value != null)
						_values.AddRange(value);
				}
			}

			public string[] ToArray() { return _values.ToArray(); }
			public void AddRange(IEnumerable<string> items) { _values.AddRange(items); }

			public static implicit operator KeyValuePair<string, string[]>(Item item)
			{
				if (item == null) throw new ArgumentNullException();
				return new KeyValuePair<string, string[]>(item.Name, item.Values);
			}
			public static implicit operator string(Item item) { return item == null ? null : item.Value; }
			public static implicit operator string[](Item item) { return item == null ? null : item.Values; }
		}

		#endregion Item class used for collection
	}
}