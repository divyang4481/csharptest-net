#region Copyright 2010 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.IO;
using System.Threading;
using CSharpTest.Net.Processes;
using NUnit.Framework;

namespace CSharpTest.Net.Library.Test
{
	[TestFixture]
	public class TestScriptRunner
	{
		[Test]
		public void TestCmdScript()
		{
			using (ScriptRunner runner = new ScriptRunner(ScriptEngine.Language.Cmd, "@ECHO From CMD.exe"))
			{
				StringWriter sw = new StringWriter();
				runner.OutputReceived += delegate(object o, ProcessOutputEventArgs e) { sw.WriteLine(e.Data); };
				Assert.AreEqual(0, runner.Run());
				Assert.AreEqual("From CMD.exe", sw.ToString().Trim());
			}
		}
		[Test]
		public void TestCSharpScript()
		{
			using (ScriptRunner runner = new ScriptRunner(ScriptEngine.Language.CSharp, "class Program { static void Main() { System.Console.WriteLine(\"From C#\"); } }"))
			{
				StringWriter sw = new StringWriter();
				runner.OutputReceived += delegate(object o, ProcessOutputEventArgs e) { sw.WriteLine(e.Data); };
				Assert.AreEqual(0, runner.Run());
				Assert.AreEqual("From C#", sw.ToString().Trim());
			}
		}
		[Test]
		public void TestExeScript()
		{
			using (ScriptRunner runner = new ScriptRunner(ScriptEngine.Language.Exe, @"cmd.exe"))
			{
				StringWriter sw = new StringWriter();
				runner.OutputReceived += delegate(object o, ProcessOutputEventArgs e) { sw.WriteLine(e.Data); };
				Assert.AreEqual(0, runner.Run("/c", "@echo From EXE"));
				Assert.AreEqual("From EXE", sw.ToString().Trim());
			}
		}
		[Test]
		public void TestJScriptScript()
		{
			using (ScriptRunner runner = new ScriptRunner(ScriptEngine.Language.JScript, @"WScript.StdOut.WriteLine(WScript.Arguments(0));"))
			{
				StringWriter sw = new StringWriter();
				runner.OutputReceived += delegate(object o, ProcessOutputEventArgs e) { sw.WriteLine(e.Data); };
				runner.Start("From JScript");
				runner.WaitForExit();
				Assert.AreEqual(0, runner.ExitCode);
				Assert.AreEqual("From JScript", sw.ToString().Trim());
			}
		}
		[Test]
		public void TestPowerShellScript()
		{
			using (ScriptRunner runner = new ScriptRunner(ScriptEngine.Language.PowerShell, "ECHO From_PowerShell"))
			{
				StringWriter sw = new StringWriter();
				runner.OutputReceived += delegate(object o, ProcessOutputEventArgs e) { sw.WriteLine(e.Data); };
				runner.Start();
				runner.WaitForExit();
				Assert.AreEqual(0, runner.ExitCode);
				Assert.AreEqual("From_PowerShell", sw.ToString().Trim());
			}
		}
		[Test]
		public void TestVbNetScript()
		{
			using (ScriptRunner runner = new ScriptRunner(ScriptEngine.Language.VBNet,
@"Module Module1
    Sub Main()
        System.Console.WriteLine(""From VB.Net"")
    End Sub
End Module
"))
			{
				StringWriter sw = new StringWriter();
				runner.OutputReceived += delegate(object o, ProcessOutputEventArgs e) { sw.WriteLine(e.Data); };
				Assert.AreEqual(0, runner.Run());
				Assert.AreEqual("From VB.Net", sw.ToString().Trim());
			}
		}
		[Test]
		public void TestVbScriptScript()
		{
			using (ScriptRunner runner = new ScriptRunner(ScriptEngine.Language.JScript, "WScript.StdOut.WriteLine(\"From VBScript\")"))
			{
				StringWriter sw = new StringWriter();
				runner.OutputReceived += delegate(object o, ProcessOutputEventArgs e) { sw.WriteLine(e.Data); };
				Assert.AreEqual(0, runner.Run());
				Assert.AreEqual("From VBScript", sw.ToString().Trim());
			}
		}

		[Test]
		public void TestToString()
		{
			using (ScriptRunner runner = new ScriptRunner(ScriptEngine.Language.Exe, @"cmd.exe"))
				Assert.AreEqual(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe"), runner.ToString().Trim());
		}
		[Test]
		public void TestRemoveOutputEvent()
		{
			using (ScriptRunner runner = new ScriptRunner(ScriptEngine.Language.Cmd, "@ECHO From CMD.exe"))
			{
				StringWriter sw = new StringWriter();
				ProcessOutputEventHandler h = delegate(object o, ProcessOutputEventArgs e) { sw.WriteLine(e.Data); };
				runner.OutputReceived += h;
				runner.OutputReceived -= h;
				Assert.AreEqual(0, runner.Run());
				Assert.AreEqual("", sw.ToString().Trim());
			}
		}
		[Test]
		public void TestExitedEvent()
		{
			using (ScriptRunner runner = new ScriptRunner(ScriptEngine.Language.Cmd, "@ECHO From CMD.exe"))
			{
				ManualResetEvent mre = new ManualResetEvent(false);
				runner.ProcessExited += delegate(object o, ProcessExitedEventArgs e) { mre.Set(); };
				Assert.AreEqual(0, runner.Run());
				Assert.IsTrue(mre.WaitOne(1000, false));
			}
		}
		[Test]
		public void TestRemoveExitedEvent()
		{
			using (ScriptRunner runner = new ScriptRunner(ScriptEngine.Language.Cmd, "@ECHO From CMD.exe"))
			{
				ManualResetEvent mre = new ManualResetEvent(false);
				ProcessExitedEventHandler h = delegate(object o, ProcessExitedEventArgs e) { mre.Set(); };
				runner.ProcessExited += h;
				runner.ProcessExited -= h;
				Assert.AreEqual(0, runner.Run());
				Assert.IsFalse(mre.WaitOne(250, false));
			}
		}
		[Test]
		public void TestJScriptWithStdIn()
		{
			using (ScriptRunner runner = new ScriptRunner(ScriptEngine.Language.JScript, @"WScript.StdOut.Write(WScript.StdIn.ReadAll());"))
			{
				StringWriter sw = new StringWriter();
				runner.OutputReceived += delegate(object o, ProcessOutputEventArgs e) { sw.WriteLine(e.Data); };
				runner.Start();
				runner.StandardInput.WriteLine("From JScript");
				runner.WaitForExit();
				Assert.AreEqual(0, runner.ExitCode);
				Assert.AreEqual("From JScript", sw.ToString().Trim());
			}
		}
		[Test]
		public void TestCSharpKilling()
		{
			using (ScriptRunner runner = new ScriptRunner(ScriptEngine.Language.CSharp, "class Program { static void Main() { System.Threading.Thread.Sleep(System.TimeSpan.FromHours(1)); } }"))
			{
				ManualResetEvent mre = new ManualResetEvent(false);
				runner.ProcessExited += delegate(object o, ProcessExitedEventArgs e) { mre.Set(); };

				runner.Start();
				Assert.IsTrue(runner.IsRunning);

				Assert.IsFalse(runner.WaitForExit(TimeSpan.Zero));
				runner.Kill();
	
				Assert.IsTrue(mre.WaitOne(0, false));
				Assert.IsFalse(runner.IsRunning);
				Assert.IsTrue(runner.WaitForExit(TimeSpan.Zero));
				Assert.AreNotEqual(0, runner.ExitCode);
			}
		}
		[Test, ExpectedException(typeof(ApplicationException))]
		public void TestCSharpCompilerError()
		{
			try
			{
				new ScriptRunner(ScriptEngine.Language.CSharp, "#error CompilerError");
				Assert.Fail("Should not be here");
			}
			catch (ApplicationException ae)
			{
				if(!ae.Message.Trim().EndsWith("#error: 'CompilerError'"))
					Assert.Fail("Incorrect message: " + ae.Message);
				throw;
			}
		}
	}
}
