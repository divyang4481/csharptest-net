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
using System.IO;
using CSharpTest.Net.CSBuild.Build;
using CSharpTest.Net.CSBuild.Configuration;

namespace CSharpTest.Net.CSBuild
{
	static class Util
	{
        internal static string GetFullPath(string path)
        {
            string file = Path.GetFileName(path);
            if (file.IndexOfAny(new char[] { '*', '?' }) >= 0)
            {
                path = Path.GetFullPath(Path.GetDirectoryName(path));
                return Path.Combine(path, file);
            }
            else
                return Path.GetFullPath(path);
        }

		internal static string MakeAbsolutePath(OutputRelative rel, string path)
		{
			if (String.IsNullOrEmpty(path))
				path = @".\";

			path = Utils.FileUtils.ExpandEnvironment(path);

			if( Path.IsPathRooted(path)) 
				return path;

			switch (rel)
			{
				case OutputRelative.None: return path;//no-translation
				case OutputRelative.FixedPath: return Util.GetFullPath( path );
				case OutputRelative.RelativeCSBuildExe:
                    { return Util.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path)); }
				case OutputRelative.RelativeWorkingDir:
                    { return Util.GetFullPath(Path.Combine(Environment.CurrentDirectory, path)); }
				//case OutputRelative.RelativeProject:
			}
			throw new NotImplementedException();
		}

		internal static string MakeFrameworkBinPath(FrameworkVersions framework)
		{
			string frmwrk;
			switch (framework)
			{
				case FrameworkVersions.v20:
				case FrameworkVersions.v30: frmwrk = "v2.0.50727"; break;
				case FrameworkVersions.v35: frmwrk = "v3.5"; break;
				//case FrameworkVersions.v40: frmwrk = "v4.0.30319"; break;
				default: throw new ArgumentException("Unknown framework version");
			}

			string windir = Environment.GetFolderPath(Environment.SpecialFolder.System);
			string msbuild = Path.Combine(windir, String.Format(@"..\Microsoft.NET\Framework\{0}\MSBuild.exe", frmwrk));
			if (!File.Exists(msbuild))
				throw new FileNotFoundException("MSBuild.exe not found.", msbuild);
			return Path.GetFullPath(Path.GetDirectoryName(msbuild));
		}
	}
}
