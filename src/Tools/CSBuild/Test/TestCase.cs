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
using NUnit.Framework;
using System.IO;
using System.Threading;

#if NET20
#if NET35
#error BOTH FRAMEWORKS DEFINED
#endif
#endif

#pragma warning disable 1591
namespace CSharpTest.Net.CSBuild.Test
{
	[TestFixture]
	public partial class TestCase
	{
		TextWriter _out, _error;
		StringWriter _consoleText, _errorText;
		public readonly string WorkingDirectory;

		#region TestFixture SetUp/TearDown
		public TestCase()
		{
			WorkingDirectory = Path.Combine(Path.GetTempPath(), this.GetType().Assembly.GetName().Name);
		}

		[TestFixtureSetUp]
		public virtual void Setup()
		{
			_out = Console.Out;
			Console.SetOut(_consoleText = new StringWriter());
			_error = Console.Error;
			Console.SetError(_errorText = new StringWriter());

			if (Directory.Exists(WorkingDirectory))
				foreach (string file in Directory.GetFiles(WorkingDirectory, "*", SearchOption.AllDirectories))
					File.Delete(file);
			try { Directory.Delete(WorkingDirectory, true); }
			catch { }
			Directory.CreateDirectory(WorkingDirectory);

			foreach (string res in this.GetType().Assembly.GetManifestResourceNames())
			{
				string folder, file;
				folder = res.Substring(this.GetType().Namespace.Length + 1);

				if (folder.EndsWith(".csproj.test"))
					folder = folder.Replace(".csproj.test", ".csproj");
				int startFile = 1 + folder.LastIndexOf('.', folder.LastIndexOf('.')-1);

				file = folder.Substring(startFile);
				if (file.EndsWith(".config", StringComparison.OrdinalIgnoreCase))
				{
					startFile = 1 + folder.LastIndexOf('.', folder.LastIndexOf('.', folder.LastIndexOf('.') - 1) - 1);
					file = folder.Substring(startFile);
				}

				folder = folder.Substring(0, startFile).Replace('.', '\\');
				folder = Path.Combine(WorkingDirectory, folder);
				file = Path.Combine(folder, file.Replace('_', '.'));
				
				Directory.CreateDirectory(folder);
				using (Stream sout = File.Create(file))
				{
					byte[] bytes = new byte[short.MaxValue];
					int len;
					using( Stream sin = this.GetType().Assembly.GetManifestResourceStream(res) )
					{
						while (0 != (len = sin.Read(bytes, 0, bytes.Length)))
							sout.Write(bytes, 0, len);
					}
				}
			}
		}

		[TestFixtureTearDown]
		public virtual void Teardown()
		{
			if (_out != null)
				Console.SetOut(_out);
			if (_error != null)
				Console.SetError(_error);
		}
		#endregion

		class CSBuildRunner
		{
			readonly ManualResetEvent _done;
			readonly Thread _t;
			readonly string _path;
			readonly string[] _args;
			Exception _error;

			public CSBuildRunner(string path, string[] args)
			{
				_error = null;
				_done = new ManualResetEvent(false);
				_path = path;
				_args = args;

				_t = new Thread(Run);
				_t.Name = "CSBuild";
				_t.SetApartmentState(ApartmentState.STA);
				_t.Start();
			}

			public void Wait()
			{
				_done.WaitOne();
				if (_error != null)
					throw new ApplicationException(String.Format("CSBuild Failed: {0}", _error.Message), _error);
			}

			private void Run()
			{
				string saved = Environment.CurrentDirectory;
				try
				{
					Environment.CurrentDirectory = _path;
					CSharpTest.Net.CSBuild.Program.Run(_args);
				}
				catch (Exception e)
				{
					_error = e;
				}
				finally
				{
					Environment.CurrentDirectory = saved;
					_done.Set();
				}
			}
		}

		public string TestCSBuild(bool assertConfig, string relPath, params string[] args)
		{
			string fullPath = Path.Combine(WorkingDirectory, relPath);
			List<String> arguments = new List<string>(args);

			if (assertConfig)
			{
				Assert.IsTrue(Directory.Exists(fullPath), "Directory not found: " + fullPath);
				Assert.IsTrue(File.Exists(Path.Combine(fullPath, "CSBuild.exe.config")), "File not found: CSBuild.exe.config");

				arguments.Add("-config=CSBuild.exe.config");
			}

			_consoleText.GetStringBuilder().Length = 0;
			_errorText.GetStringBuilder().Length = 0;
			CSBuildRunner runner = new CSBuildRunner(fullPath, arguments.ToArray());

			try
			{
				runner.Wait();
			}
			catch(ApplicationException inner)
			{
				//_error.Write(_errorText.ToString());
				Exception inside = inner;
				if (inside.InnerException != null)
					inside = inside.InnerException;

				string desc = _errorText.ToString().Trim();
				if (desc.Length == 0)
					desc = _consoleText.ToString().Trim();

				throw new ApplicationException(desc, inside);
			}

			return fullPath;
		}

		[Test][ExpectedException(typeof(ApplicationException))]
		public void TestBadConfig()
		{
			TestCSBuild(true, @"Tests\BadConfig", "/quiet");
		}

		[Test][ExpectedException(typeof(ApplicationException))]
		public void TestNoConfig()
		{
			TestCSBuild(false, @"Tests\NoConfig", "/verbose");
		}

		[Test]
		public void TestProjects()
		{
			string path = TestCSBuild(true, @"Tests\Projects", String.Format(@"-log:..\Projects.log"));
			CheckOutputFiles(Path.Combine(path, @"bin20"), 2, 1, 0);
			CheckOutputFiles(Path.Combine(path, @"bin35"), 2, 1, 1);
		}

		private void CheckOutputFiles(string root, int execount, int dllcount, int otherdlls)
		{
			Assert.IsTrue(File.Exists(Path.Combine(root, "msbuild.txt")), "msbuild.txt is missing.");
			Assert.IsTrue(File.Exists(Path.Combine(root, "msbuild.xml")), "msbuild.xml is missing.");
			Assert.AreEqual(execount, Directory.GetFiles(root, "*.exe").Length, "The number of *.exe files is incorrect");
			Assert.AreEqual(dllcount + otherdlls, Directory.GetFiles(root, "*.dll").Length, "The number of *.dll files is incorrect");
			Assert.AreEqual(execount + dllcount, Directory.GetFiles(root, "*.pdb").Length, "The number of *.pdb files is incorrect");

			int total = 2 + (execount * 2) + (dllcount * 2) + otherdlls;
			Assert.AreEqual(total, Directory.GetFiles(root).Length, "The number of files is incorrect");
		}
	}
}
