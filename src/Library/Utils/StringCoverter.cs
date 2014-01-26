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

namespace CSharpTest.Net.Utils
{
	/// <summary>
	/// Handles the conversion of data to and from strings for serialization.  Can
	/// alternatly be configured to provide other transforms for display or other
	/// outputs.
	/// </summary>
	public class StringConverter
	{
		/// <summary>
		/// The delegate type used to try and parse a string
		/// </summary>
		public delegate bool TryParseMethod<TYPE>(string text, out TYPE value);
		#region private class TypeConverter
		private interface IConvertToFromString
		{
			string ToString(object value);
			bool TryParse(string text, out object value);
		}
		private class ConvertToFromString<TYPE> : IConvertToFromString
		{
			public readonly TryParseMethod<TYPE> TryParse;
			bool IConvertToFromString.TryParse(string text, out object value)
			{
				TYPE data;
				if (TryParse(text, out data))
				{ value = data; return true; }
				else
				{ value = null; return false; }
			}

			public new readonly Converter<TYPE, String> ToString;
			string IConvertToFromString.ToString(object value)
			{
				return ToString((TYPE)value);
			}

			public ConvertToFromString(TryParseMethod<TYPE> tryParse, Converter<TYPE, String> toString)
			{
				this.TryParse = tryParse;
				this.ToString = toString;
			}
		}
		#endregion

		private Dictionary<Type, IConvertToFromString> _converters;

		/// <summary>
		/// Constructs a default StringConverter object for serialization
		/// </summary>
		public StringConverter() : this(true) 
		{}

		/// <summary>
		/// Constructs a StringConverter optionally populated with the default
		/// serialization transforms.
		/// </summary>
		/// <param name="includeDefaults">true to include default transforms</param>
		public StringConverter(bool includeDefaults) 
		{
			_converters = new Dictionary<Type,IConvertToFromString>();
			if (includeDefaults) AddDefaults();
		}

		#region Default Converters
		private static string NormalToString<T>(T value) { return value.ToString(); }
		//System.Single
		private static string SingleToString(Single value) { return value.ToString("r", System.Globalization.CultureInfo.InvariantCulture); }
		private static bool SingleTryParse(string value, out Single data) { return Single.TryParse(value, System.Globalization.NumberStyles.AllowDecimalPoint | System.Globalization.NumberStyles.AllowExponent | System.Globalization.NumberStyles.AllowLeadingSign, System.Globalization.CultureInfo.InvariantCulture, out data); }
		//System.Double
		private static string DoubleToString(Double value) { return value.ToString("r", System.Globalization.CultureInfo.InvariantCulture); }
		private static bool DoubleTryParse(string value, out Double data) { return Double.TryParse(value, System.Globalization.NumberStyles.AllowDecimalPoint | System.Globalization.NumberStyles.AllowExponent | System.Globalization.NumberStyles.AllowLeadingSign, System.Globalization.CultureInfo.InvariantCulture, out data); }
		//System.DateTime
		private static string DateTimeToString(DateTime date) { return date.ToString("o", System.Globalization.CultureInfo.InvariantCulture); }
		private static bool DateTimeTryParse(string value, out DateTime dt) { return DateTime.TryParseExact(value, "o", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out dt); }
		//System.Uri
		private static string UriToString(Uri value) { return value.AbsoluteUri; }
		private static bool UriTryParse(string value, out Uri uri) { return Uri.TryCreate(value, UriKind.Absolute, out uri); }
		//System.Guid
		private static bool StringTryParse(string value, out string data) { data = value; return data != null; }
		private static bool GuidTryParse(string value, out Guid data) { data = Guid.Empty; try { if (RegexPatterns.Guid.IsMatch(value)) { data = new Guid(value); return true; } } catch { } return false; }
		//System.Version
		private static bool VersionTryParse(string value, out Version data) { data = null; try { if (RegexPatterns.Version.IsMatch(value)) { data = new Version(value); return true; } } catch { } return false; }


		private void AddDefaults()
		{
			Add<bool>( bool.TryParse, NormalToString<bool> );
			Add<byte>(byte.TryParse, NormalToString<byte>);
			Add<sbyte>(sbyte.TryParse, NormalToString<sbyte>);
			Add<char>(char.TryParse, NormalToString<char>);
			Add<DateTime>(DateTimeTryParse, DateTimeToString);
			Add<TimeSpan>(TimeSpan.TryParse, NormalToString<TimeSpan>);
			Add<decimal>(decimal.TryParse, NormalToString<decimal>);
			Add<double>(DoubleTryParse, DoubleToString);
			Add<float>(SingleTryParse, SingleToString);
			Add<Guid>(GuidTryParse, NormalToString<Guid>);
			Add<Uri>(UriTryParse, UriToString);
			Add<short>(short.TryParse, NormalToString<short>);
			Add<ushort>(ushort.TryParse, NormalToString<ushort>);
			Add<int>(int.TryParse, NormalToString<int>);
			Add<uint>(uint.TryParse, NormalToString<uint>);
			Add<long>(long.TryParse, NormalToString<long>);
			Add<ulong>(ulong.TryParse, NormalToString<ulong>);
			Add<string>(StringTryParse, NormalToString<string>);
			Add<Version>(VersionTryParse, NormalToString<Version>);
		}
		#endregion

		/// <summary>
		/// Adds a converter for the type TYPE that can transform the TYPE to and from a string
		/// </summary>
		/// <typeparam name="TYPE">The type that can be transformed by the delegates</typeparam>
		/// <param name="tryParse">A delegate method to convert from a string</param>
		/// <param name="toString">A delegate method to convert to a string</param>
		public void Add<TYPE>(TryParseMethod<TYPE> tryParse, Converter<TYPE, String> toString)
		{
			_converters[typeof(TYPE)] = new ConvertToFromString<TYPE>(Check.NotNull(tryParse), Check.NotNull(toString));
		}

		/// <summary>
		/// Removes the TYPE from the set of types allowed to be converted to and from strings.
		/// </summary>
		/// <typeparam name="TYPE">The type that will no longer be transformed</typeparam>
		public void Remove<TYPE>() 
		{
			_converters.Remove(typeof(TYPE)); 
		}

		/// <summary>
		/// Converts an object to a string if the type is registered, or ArgumentOutOfRangeException
		/// is thrown if no transform is registered for that type.
		/// </summary>
		public string ToString(object value)
		{
			IConvertToFromString cnvt;
			if (!_converters.TryGetValue(Check.NotNull(value).GetType(), out cnvt))
				throw new ArgumentOutOfRangeException(Resources.StringConverterTryParse(value.GetType()));

			return cnvt.ToString(value);
		}
		
		/// <summary>
		/// Converts the value of TYPE to a string if the type is registered, or ArgumentOutOfRangeException
		/// is thrown if no transform is registered for that type.
		/// </summary>
		public string ToString<TYPE>(TYPE value)
		{
			IConvertToFromString cnvt;
			if (!_converters.TryGetValue(typeof(TYPE), out cnvt))
				throw new ArgumentOutOfRangeException(Resources.StringConverterTryParse(typeof(TYPE)));

			return ((ConvertToFromString<TYPE>)cnvt).ToString(Check.NotNull(value));
		}

		/// <summary>
		/// Converts the provided string to a value of TYPE if the type is registered, 
		/// or raises ArgumentOutOfRangeException if no transform is registered for that type.
		/// Throws an ArgumentException if the string can not be converted.
		/// </summary>
		public TYPE FromString<TYPE>(string value)
		{
			TYPE data;
			if (!TryParse<TYPE>(Check.NotNull(value), out data))
				throw new ArgumentException();
			return data;
		}

		/// <summary>
		/// Converts the provided string to a value of TYPE if the type is registered, 
		/// or raises ArgumentOutOfRangeException if no transform is registered for that type.
		/// </summary>
		/// <param name="input">The string value to convert</param>
		/// <param name="type">The type of the value to be converted to</param>
		/// <param name="value">The value once converted</param>
		/// <returns>True if it was able to make the conversion</returns>
		public bool TryParse(string input, Type type, out object value)
		{
			if (input == null) { value = null; return false; }

			IConvertToFromString cnvt;
			if (!_converters.TryGetValue(Check.NotNull(type), out cnvt))
				throw new ArgumentOutOfRangeException(Resources.StringConverterTryParse(type));

			return cnvt.TryParse(Check.NotNull(input), out value);
		}

		/// <summary>
		/// Converts the provided string to a value of TYPE if the type is registered, 
		/// or raises ArgumentOutOfRangeException if no transform is registered for that type.
		/// </summary>
		/// <typeparam name="TYPE">The type of the value to be converted to</typeparam>
		/// <param name="input">The string value to convert</param>
		/// <param name="value">The value once converted</param>
		/// <returns>True if it was able to make the conversion</returns>
		public bool TryParse<TYPE>(string input, out TYPE value)
		{
			if (input == null) { value = default(TYPE); return false; }

			IConvertToFromString cnvt;
			if (!_converters.TryGetValue(typeof(TYPE), out cnvt))
				throw new ArgumentOutOfRangeException(Resources.StringConverterTryParse(typeof(TYPE)));

			return ((ConvertToFromString<TYPE>)cnvt).TryParse(Check.NotNull(input), out value);
		}
	}
}
