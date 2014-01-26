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
using System.Text;
using System.Text.RegularExpressions;

namespace CSharpTest.Net.Utils
{
	/// <summary>
	/// Various routines for string manipulations
	/// </summary>
	[System.Diagnostics.DebuggerNonUserCode]
	public static class StringUtils
	{
		/// <summary>
		/// returns a new string containing only the alpha-numeric characters in the original
		/// </summary>
		public static string AlphaNumericOnly(string input)
		{
			StringBuilder sb = new StringBuilder();
			foreach (char ch in Check.NotNull(input))
			{
				if (Char.IsLetterOrDigit(ch))
					sb.Append(ch);
			}

			return sb.ToString();
		}

		/// <summary>
		/// Parses the text to ensure that it is a valid file name and returns the resulting 
		/// string with the following offending characters replace with '-': /\:*?"'&lt;>|
		/// Also removes any characters class as control characters, or anything below 32 space,
		/// this would include tab, backspace, newline, linefeed, etc.
		/// If provided null, this function returns null
		/// </summary>
		/// <param name="name">The text to parse</param>
		/// <returns>The text provided with only allowable characters</returns>
		public static string SafeFileName(string name)
		{
			if (name == null) return null;
			StringBuilder sbName = new StringBuilder();
			foreach (char ch in name)
			{
				if (ch >= ' ' && ch != '/' && ch != '\\' && ch != ':' &&
					ch != '*' && ch != '?' && ch != '\'' && ch != '"' &&
					ch != '<' && ch != '>' && ch != '|' && !Char.IsControl(ch))
					sbName.Append(ch);
				else sbName.Append('-');
			}
			return sbName.ToString();
		}

		/// <summary>
		/// Splits the string on path characters ('/' and '\\') and passes each
		/// to SafeFileName(), then reconstructs the string using '\\' and
		/// removing any empty segments. 
		/// If provided null, this function returns null, provided an empty
		/// string or just a path seperator '/' it will return String.Empty
		/// </summary>
		/// <param name="path">The text to parse</param>
		/// <returns>The text provided as a valid path</returns>
		public static string SafeFilePath(string path)
		{
			if(path == null) return null;

			StringBuilder sbPath = new StringBuilder();
			foreach (string part in path.Split('/', '\\'))
			{
				string name = SafeFileName(part);
				if (!String.IsNullOrEmpty(name))
				{
					sbPath.Append(name);
					sbPath.Append('\\');
				}
			}

			if (sbPath.Length == 0) return String.Empty;
			return sbPath.ToString(0, sbPath.Length - 1);
		}

		/// <summary>
		/// DO NOT EXPOSE THIS PRIVATE MEMEBER... Since the behavior of this can be changed this could have
		/// adverse effects in unrelated code.
		/// </summary>
		private static readonly StringConverter DefaultConverter = new StringConverter(true);

		/// <summary>
		/// Converts primitives to strings so that they can be reconstituted via TryParse
		/// </summary>
		public static string ToString<TYPE>(TYPE value) { return DefaultConverter.ToString(value); }

		/// <summary>
		/// Converts primitives to strings so that they can be reconstituted via TryParse
		/// </summary>
		public static string ToString(object value) { return DefaultConverter.ToString(value); }
	
		/// <summary>
		/// Reconstructs a type from a string that was previously obtained via StringUtils.ToString(T data)
		/// </summary>
		public static bool TryParse<TYPE>(string input, out TYPE value) { return DefaultConverter.TryParse(input, out value); }

		/// <summary>
		/// Reconstructs a type from a string that was previously obtained via StringUtils.ToString(T data)
		/// </summary>
		public static bool TryParse(string input, Type type, out object value) { return DefaultConverter.TryParse(input, type, out value); }

		/// <summary>
		/// Used for text-template transformation where a regex match is replaced in the input string.
		/// </summary>
		/// <param name="input">The text to perform the replacement upon</param>
		/// <param name="pattern">The regex used to perform the match</param>
		/// <param name="fnReplace">A delegate that selects the appropriate replacement text</param>
		/// <returns>The newly formed text after all replacements are made</returns>
		public static string Transform(string input, Regex pattern, Converter<Match, string> fnReplace)
		{
			int currIx = 0;
			StringBuilder sb = new StringBuilder();

			foreach (Match match in pattern.Matches(input))
			{
				sb.Append(input, currIx, match.Index - currIx);
				string replace = fnReplace(match);
				sb.Append(replace);

				currIx = match.Index + match.Length;
			}

			sb.Append(input, currIx, input.Length - currIx);
			return sb.ToString();
		}
	}
}
