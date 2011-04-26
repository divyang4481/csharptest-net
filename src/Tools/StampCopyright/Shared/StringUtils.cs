#region Copyright 2010-2011 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Text;
using System.Text.RegularExpressions;

namespace CSharpTest.Net.Utils
{
	static class StringUtils
	{
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
