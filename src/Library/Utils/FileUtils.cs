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
using System.IO;
using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Principal;

namespace CSharpTest.Net.Utils
{
	/// <summary>
	/// Provides utilities related to files and file paths
	/// </summary>
	public static class FileUtils
	{
		private static readonly string FileNotFoundMessage = new FileNotFoundException().Message;
		private static readonly char[] IllegalFileNameChars = new char[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|' };

		/// <summary>
		/// Returns the fully qualified path to the file if it is fully-qualified, exists in the current directory, or 
		/// in the environment path, otherwise generates a FileNotFoundException exception.
		/// </summary>
		public static string FindFullPath(string location)
		{
			string result;
			if (TrySearchPath(location, out result))
				return result;
			throw new FileNotFoundException(FileNotFoundMessage, location);
		}

		/// <summary>
		/// Returns true if the file is fully-qualified, exists in the current directory, or in the environment path, 
		/// otherwise generates a FileNotFoundException exception.  Will not propagate errors.
		/// </summary>
		public static bool TrySearchPath(string location, out string fullPath)
		{
			fullPath = null;

			try
			{
				if (File.Exists(location))
				{
					fullPath = Path.GetFullPath(location);
					return true;
				}

				if (Path.IsPathRooted(location))
					return false;
				if (location.IndexOfAny(IllegalFileNameChars) >= 0 || location.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
					return false;

				foreach (string pathentry in Environment.GetEnvironmentVariable("PATH").Split(';'))
				{
					string testPath = pathentry.Trim();
					if (testPath.Length > 0 && Directory.Exists(testPath) && File.Exists(Path.Combine(testPath, location)))
					{
						fullPath = Path.GetFullPath(Path.Combine(testPath, location));
						return true;
					}
				}
			}
			catch (System.Threading.ThreadAbortException) { throw; }
			catch (Exception error) { Trace.TraceError("{0}", error); }

			return false;
		}

		/// <summary> Grants the user FullControl for the file, returns true if modified, false if already present </summary>
		public static bool GrantFullControlForFile(string filepath, WellKnownSidType sidType)
		{ return GrantFullControlForFile(filepath, sidType, null); }

		/// <summary> Grants the user FullControl for the file, returns true if modified, false if already present </summary>
		public static bool GrantFullControlForFile(string filepath, WellKnownSidType sidType, SecurityIdentifier domain)
		{
			FileSecurity sec = File.GetAccessControl(filepath);
			SecurityIdentifier sid = new SecurityIdentifier(sidType, domain);
			bool found = false;

			List<FileSystemAccessRule> toremove = new List<FileSystemAccessRule>();
			foreach (FileSystemAccessRule rule in sec.GetAccessRules(true, false, typeof(SecurityIdentifier)))
			{
				if (sid.Value == rule.IdentityReference.Value)
				{
					if (rule.AccessControlType != AccessControlType.Allow || rule.FileSystemRights != FileSystemRights.FullControl)
						toremove.Add(rule);
					else
						found = true;
				}
			}
			if (!found || toremove.Count > 0)
			{
				foreach (FileSystemAccessRule bad in toremove)
					sec.RemoveAccessRule(bad);

				sec.AddAccessRule(new FileSystemAccessRule(sid, FileSystemRights.FullControl, AccessControlType.Allow));
				File.SetAccessControl(filepath, sec);
				return true;
			}

			return false;
		}
	}
}
