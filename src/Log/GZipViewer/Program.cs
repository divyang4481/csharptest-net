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
using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace GZipViewer
{
	class Program
	{
		[STAThread]
		static int Main(string[] args)
		{
			if(args.Length == 0 || IsDefined("?", args))
				return Help();
			
			try
			{
				if(IsDefined("register", args))
					return Register();

				bool view = IsDefined("view", args);

				List<string> files = new List<string>();
				foreach(string arg in args)
				{
					if(arg.StartsWith("-") || arg.StartsWith("/"))
						continue;

					if(!File.Exists(arg))
						throw new FileNotFoundException("File not found.", arg);
					
					files.Add(arg);
				}

				if(files.Count == 0)
					throw new FileNotFoundException( "No input file specified.\r\n" + Environment.CommandLine );

				foreach(string file in files)
					ProcessFile(file, view);
			}
			catch(Exception e)
			{
				System.Windows.Forms.MessageBox.Show(null, e.Message, e.Source, System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
				Environment.ExitCode = -1;
			}

			return Environment.ExitCode;
		}

		static void ProcessFile(string file, bool view)
		{
			bool extract = file.EndsWith(".gz", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".gzip", StringComparison.OrdinalIgnoreCase);
			string from = file;
			string to = file;

			if(extract)
			{
				to = to.Substring(0, to.LastIndexOf(".gz", StringComparison.OrdinalIgnoreCase));
				if(view)
					to = Path.Combine(Path.GetTempPath(), Path.GetFileName(to));
			}
			else
				to += ".gz";
			
			GZipFile(from, to, extract == false);

			if(view)
			{
				System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo();
				psi.FileName = to;
				psi.UseShellExecute = true;
				psi.Verb = "open";
				psi.WorkingDirectory = Environment.CurrentDirectory;
				System.Diagnostics.Process process = System.Diagnostics.Process.Start(psi);
				if(process != null)
				{
					process.WaitForExit();
					File.Delete(to);
				}
			}
			else
				File.Delete(from);
		}


		private static void GZipFile(string from, string to, bool compress)
		{
			Stream sout = File.Open(to, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
			if(compress)
				sout = new GZipStream(sout, CompressionMode.Compress, false);
			
			using(sout)
			{
				Stream sin = File.Open(from, FileMode.Open, FileAccess.Read, FileShare.Write);
				if(!compress)
					sin = new GZipStream(sin, CompressionMode.Decompress, false);
				using(sin)
				{
					byte[] buffer = new byte[8192];
					int len;

					while(0 != (len = sin.Read(buffer, 0, buffer.Length)))
						sout.Write(buffer, 0, len);

					sout.Flush();
					sout.Close();
					sin.Close();
				}
			}
		}

		static bool IsDefined(string test, string[] args)
		{
			foreach(string arg in args)
			{
				if( StringComparer.OrdinalIgnoreCase.Equals( test, arg.TrimStart('-','/')) )
					return true;
			}
			return false;
		}

		static int Help()
		{
			System.IO.StringWriter sw = new System.IO.StringWriter();
			sw.WriteLine("Usages: ");
			sw.WriteLine();
			sw.WriteLine(@"  -register    - registers the .gz extension with this exe");
			sw.WriteLine(@"  file1.ext [file2.ext ...]    - compress files to file1.ext.gz");
			sw.WriteLine(@"  file1.ext.gz [file3.ext.gz ...]    - decompress files to file1.ext");
			sw.WriteLine(@"  -view file1.ext.gz    - decompress file to %temp%\file1.ext and opens the file for viewing");

			System.Windows.Forms.MessageBox.Show(null, sw.ToString(), "Help", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
			return 0;
		}

		static int Register()
		{
			SetValue(".gz", null, "GZipViewer");
			SetValue(".gzip", null, "GZipViewer");

			SetValue(@"GZipViewer\DefaultIcon", null, "\"{0}\",0", typeof(Program).Assembly.Location);

			SetValue(@"*\shell\gzip", null, "GZip File");
			SetValue(@"*\shell\gzip\command", null, "\"{0}\" \"%1\"", typeof(Program).Assembly.Location);
			
			SetValue(@"GZipViewer\shell\open", null, "Extract && View");
			SetValue(@"GZipViewer\shell\open\command", null, "\"{0}\" -view \"%1\"", typeof(Program).Assembly.Location);
			
			SetValue(@"GZipViewer\shell\extract", null, "Extract");
			SetValue(@"GZipViewer\shell\extract\command", null, "\"{0}\" \"%1\"", typeof(Program).Assembly.Location);
			return 0;
		}

		static void SetValue(string path, string valueName, string valueData, params object[] args)
		{
			if(args.Length > 0) 
				valueData = String.Format(valueData, args);
			
			RegistryKey key = Registry.ClassesRoot.OpenSubKey(path, true);
			if(key == null) key = Registry.ClassesRoot.CreateSubKey(path);
			key.SetValue(valueName, valueData);
			key.Close();
		}
	}
}
