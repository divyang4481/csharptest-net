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
using CSharpTest.Net.IO;
using NUnit.Framework;
using CSharpTest.Net.Processes;
using System.IO;
using CSharpTest.Net.Utils;

#pragma warning disable 1591
namespace CSharpTest.Net.Library.Test
{
	[TestFixture]
	public class TestProcessRunner
	{
        [Test]
        public void TestStdOutput()
        {
            using (ProcessRunner runner = new ProcessRunner("cmd.exe"))
            {
                StringWriter wtr = new StringWriter();
                StringWriter err = new StringWriter();
                ProcessOutputEventHandler handler =
                        delegate(object o, ProcessOutputEventArgs e)
                        { if (e.Error) err.WriteLine(e.Data); else wtr.WriteLine(e.Data); };

                runner.OutputReceived += handler;

                Assert.AreEqual(0, runner.Run("/C", "dir", "/b", "/on", "/ad-h-s", "c:\\"));
                Assert.AreEqual(String.Empty, err.ToString());

                StringReader rdr = new StringReader(wtr.ToString());
                List<DirectoryInfo> rootdirs = new List<DirectoryInfo>(new DirectoryInfo("C:\\").GetDirectories());
                rootdirs.Sort(delegate(DirectoryInfo x, DirectoryInfo y) { return StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name); });
                foreach (DirectoryInfo di in rootdirs)
                {
                    if ((di.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0)
                        continue;
                    Assert.AreEqual(di.Name, rdr.ReadLine());
                }

                Assert.AreEqual(null, rdr.ReadLine());
            }
        }

		[Test]
		public void TestToString()
		{
			ProcessRunner runner = new ProcessRunner("cmd.exe", "/c", "echo hi");
			string target = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"cmd.exe");
			Assert.AreEqual(target + " /c \"echo hi\"", runner.ToString());
		}

		[Test]
		public void TestStdError()
		{
			ProcessRunner runner = new ProcessRunner("cmd.exe");

			StringWriter wtr = new StringWriter();
			StringWriter err = new StringWriter();
			runner.OutputReceived += delegate(object o, ProcessOutputEventArgs e)
			{ if (e.Error) err.WriteLine(e.Data); else wtr.WriteLine(e.Data); };

			Assert.AreNotEqual(0, runner.Run("/C", "dir", "c:\\I truely hope for this tests sake that this directory doesn't exist\\"));
			Assert.AreEqual(String.Empty, wtr.ToString());
			Assert.AreEqual("The system cannot find the file specified.", err.ToString().Trim());
		}

		[Test]
		public void TestOutputEventUnsubscribe()
		{
			ProcessRunner runner = new ProcessRunner("cmd.exe", "/C", "echo");

			bool outputReceived = false;
			ProcessOutputEventHandler handler =
					delegate(object o, ProcessOutputEventArgs e)
					{ outputReceived = true; };

			runner.OutputReceived += handler;
			runner.OutputReceived -= handler;

			Assert.AreEqual(0, runner.Run());
			Assert.IsFalse(outputReceived);
        }

        [Test]
        public void TestRunWithInput()
        {
            using (ProcessRunner runner = new ProcessRunner("cmd.exe", "/C", "sort"))
            {
                List<string> lines = new List<string>();
                runner.OutputReceived += delegate(Object o, ProcessOutputEventArgs e) { lines.Add(e.Data); };
                int exitCode = runner.Run(new StringReader("Hello World\r\nWhatever!\r\nA line that goes first."));
                Assert.AreEqual(0, exitCode);
                Assert.AreEqual("A line that goes first.", lines[0]);
                Assert.AreEqual("Hello World", lines[1]);
                Assert.AreEqual("Whatever!", lines[2]);
            }
        }

        [Test]
        public void TestRunWithWorkingDirectory()
        {
            using (TempDirectory dir = new TempDirectory())
            using (ProcessRunner runner = new ProcessRunner("cmd.exe", "/C", "echo %CD%"))
            {
                List<string> lines = new List<string>();
                runner.OutputReceived += delegate(Object o, ProcessOutputEventArgs e) { lines.Add(e.Data); };

                Assert.AreNotEqual(dir.TempPath, runner.WorkingDirectory);
                runner.WorkingDirectory = dir.TempPath;
                Assert.AreEqual(dir.TempPath, runner.WorkingDirectory);
                
                int exitCode = runner.Run();
                Assert.AreEqual(0, exitCode);
                Assert.AreEqual(dir.TempPath.TrimEnd('\\', '/'), lines[0].TrimEnd('\\', '/'));
            }
        }

        [Test]
        public void TestExitedEvent()
        {
            ProcessRunner runner = new ProcessRunner("cmd.exe", "/C", "echo");

            int exitCode = -1;
            bool receivedExit = false;
            ProcessExitedEventHandler handler =
                    delegate(object o, ProcessExitedEventArgs e)
                    { receivedExit = true; exitCode = e.ExitCode; };

            runner.ProcessExited += handler;

            Assert.AreEqual(0, runner.Run());
            Assert.IsTrue(receivedExit);
            Assert.AreEqual(0, exitCode);
		}

		[Test]
		public void TestExitedEventUnsubscribe()
		{
			ProcessRunner runner = new ProcessRunner("cmd.exe", "/C", "echo");

			int exitCode = -1;
			bool receivedExit = false;
			ProcessExitedEventHandler handler =
					delegate(object o, ProcessExitedEventArgs e)
					{ receivedExit = true; exitCode = e.ExitCode; };

			runner.ProcessExited += handler;
			runner.ProcessExited -= handler;

			Assert.AreEqual(0, runner.Run());
			Assert.IsFalse(receivedExit);
			Assert.AreEqual(-1, exitCode);
		}

		[Test]
		public void TestStdInput()
		{
			string tempfile = Path.GetTempFileName();
			try
			{
				ProcessRunner runner = new ProcessRunner("cmd.exe");

				runner.Start();
				runner.StandardInput.WriteLine("ECHO Hello > " + tempfile);
				runner.StandardInput.WriteLine("EXIT");

				Assert.IsTrue(runner.WaitForExit(TimeSpan.FromMinutes(1)));
				Assert.AreEqual(0, runner.ExitCode);

				string output = File.ReadAllText(tempfile).Trim();
				Assert.AreEqual("Hello", output);
			}
			finally
			{ File.Delete(tempfile); }
		}

		[Test]
		public void TestFormatArgs()
		{
			string tempfile = Path.GetTempFileName();
			try
			{
				ProcessRunner runner = new ProcessRunner("cmd.exe", "/C", "ECHO", "Hello", ">{0}");

				runner.StartFormatArgs(tempfile);
				Assert.AreEqual(0, runner.ExitCode);

				string output = File.ReadAllText(tempfile).Trim();
				Assert.AreEqual("Hello", output);

				File.Delete(tempfile);
				Assert.AreEqual(0, runner.RunFormatArgs(tempfile));

				output = File.ReadAllText(tempfile).Trim();
				Assert.AreEqual("Hello", output);
			}
			finally
			{ File.Delete(tempfile); }
		}

		[Test]
		public void TestKill()
		{
			string tempfile = Path.GetTempFileName();
			try
			{
				Environment.CurrentDirectory = Path.GetDirectoryName(FileUtils.FindFullPath("cmd.exe"));
				ProcessRunner runner = new ProcessRunner("cmd.exe");
				Assert.IsFalse(runner.IsRunning);
				runner.Kill(); // always safe to call

				runner.Start("/K", "ECHO Hello > " + tempfile);
				Assert.IsTrue(runner.IsRunning);

				try //make sure we can't start twice.
				{
					runner.Start("/C", "ECHO", "HELO");
					Assert.Fail();
				}
				catch (InvalidOperationException)
				{ }

				Assert.IsFalse(runner.WaitForExit(TimeSpan.FromSeconds(1), false));
				runner.Kill();
				string output = File.ReadAllText(tempfile).Trim();
				Assert.AreEqual("Hello", output);
			}
			finally
			{ File.Delete(tempfile); }
		}
	}
}
