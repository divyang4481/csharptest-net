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
using System.Text;
using System.Collections.Generic;

namespace CSharpTest.Net.CSBuild.Implementation
{
	static class FileUtils
	{
		/// <summary>
		/// For this to work for a directory the argument should end with a '\' character
		/// </summary>
		public static string MakeRelativePath(string startFile, string targetFile)
		{
			StringBuilder newpath = new StringBuilder();

			if (startFile == targetFile)
				throw new ApplicationException("self linked: " + targetFile);

			if (targetFile.StartsWith(startFile, StringComparison.OrdinalIgnoreCase))
				return targetFile.Substring(startFile.Length);

			string[] sfpath = startFile.Split('\\');
			string[] tfpath = targetFile.Split('\\');

			int cmpdepth = Math.Min(sfpath.Length - 1, tfpath.Length - 1);
			int ixdiff = 0;
			for (; ixdiff < cmpdepth; ixdiff++)
				if (false == StringComparer.OrdinalIgnoreCase.Equals(sfpath[ixdiff], tfpath[ixdiff]))
					break;

			for (int i = ixdiff; i < (sfpath.Length - 1); i++)
				newpath.Append("..\\");
			for (int i = ixdiff; i < tfpath.Length; i++)
			{
				newpath.Append(tfpath[i]);
				if((i + 1) < tfpath.Length)
					newpath.Append('\\');
			}
			return newpath.ToString();
		}
	}
}
