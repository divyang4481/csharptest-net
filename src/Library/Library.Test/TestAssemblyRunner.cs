#region Copyright 2010-2012 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Threading;
using CSharpTest.Net.IO;
using CSharpTest.Net.Reflection;
using NUnit.Framework;
using CSharpTest.Net.Processes;
using System.IO;
using CSharpTest.Net.Utils;

#pragma warning disable 1591
namespace CSharpTest.Net.Library.Test
{
    [TestFixture]
    public class TestAssemblyRunner
    {
        //Hack to create the managed executable from source.
        ScriptRunner _exeMaker;
        string Exe { get { return (_exeMaker ?? (_exeMaker = new ScriptRunner(ScriptEngine.Language.CSharp, Resources.AssemblyRunnerSampleApp_cs))).ScriptEngine.Executable; } }

        [Test]
        public void TestStdOutputAndStdError()
        {
            using (AssemblyRunner runner = new AssemblyRunner(Exe))
            {
                StringWriter wtr = new StringWriter();
                StringWriter err = new StringWriter();
                ProcessOutputEventHandler handler =
                        delegate(object o, ProcessOutputEventArgs e)
                        { if (e.Error) err.WriteLine(e.Data); else wtr.WriteLine(e.Data); };

                runner.OutputReceived += handler;

                Assert.AreEqual(1, runner.Run("command-line-argrument"));
                Assert.AreEqual("std-err", err.ToString().Trim());

                StringReader rdr = new StringReader(wtr.ToString());
                Assert.AreEqual("WorkingDirectory = " + Environment.CurrentDirectory, rdr.ReadLine());
                Assert.AreEqual("argument[0] = command-line-argrument", rdr.ReadLine());
                Assert.AreEqual("std-input:", rdr.ReadLine());
                Assert.AreEqual(null, rdr.ReadLine());
            }
        }

        [Test]
        public void TestToString()
        {
            using (AssemblyRunner runner = new AssemblyRunner(Exe))
                Assert.AreEqual(Exe, runner.ToString());
        }

        [Test]
        public void TestDisposal()
        {
            AssemblyRunner runner = new AssemblyRunner(Exe);
            runner.Dispose();
            Assert.IsTrue(runner.IsDisposed);
        }

        [Test, ExpectedException(typeof(AppDomainUnloadedException))]
        public void TestUnloaded()
        {
            AppDomain domain = null;
            try
            {
                AssemblyRunner runner = new AssemblyRunner(Exe);
                //I hate using reflection for testing; however, sometimes it's best to be certain things are behaving as expected:
                domain = new PropertyValue<AppDomain>(runner, "_workerDomain").Value;
                runner = null;
            }
            catch { throw; }

            GC.Collect(0, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();

            //The GC only starts the AppDomain unload within the ThreadPool, we have to wait for it to actually happen.
            DateTime max = DateTime.Now.AddMinutes(1);
            while(DateTime.Now < max)
                Assert.AreEqual(domain.Id, domain.Id);
        }

        [Test, ExpectedException(typeof(NotSupportedException))]
        public void TestStandardInputFail()
        {
            using (IRunner runner = new AssemblyRunner(Exe))
            {
                runner.Start("-wait");
                runner.StandardInput.WriteLine();
            }
        }

        [Test, ExpectedException(typeof(System.ArgumentOutOfRangeException))]
        public void TestUnhandledExceptionWaitForExit()
        {
            using (IRunner runner = new AssemblyRunner(Exe))
            {
                runner.Start("-throw", "System.ArgumentOutOfRangeException");
                runner.WaitForExit();
                Assert.Fail();
            }
        }

        [Test, ExpectedException(typeof(System.ArgumentOutOfRangeException))]
        public void TestUnhandledExceptionRun()
        {
            using (IRunner runner = new AssemblyRunner(Exe))
            {
                runner.Run("-throw", "System.ArgumentOutOfRangeException");
                Assert.Fail();
            }
        }

        [Test]
        public void TestOutputEventUnsubscribe()
        {
            using (AssemblyRunner runner = new AssemblyRunner(Exe))
            {
                bool outputReceived = false;
                ProcessOutputEventHandler handler =
                        delegate(object o, ProcessOutputEventArgs e)
                        { outputReceived = true; };

                runner.OutputReceived += handler;
                runner.OutputReceived -= handler;

                Assert.AreEqual(0, runner.Run());
                Assert.IsFalse(outputReceived);
            }
        }

        [Test]
        public void TestRunWithInput()
        {
            using (AssemblyRunner runner = new AssemblyRunner(Exe))
            {
                List<string> lines = new List<string>();
                runner.OutputReceived += delegate(Object o, ProcessOutputEventArgs e) { lines.Add(e.Data); };
                int exitCode = runner.Run(new StringReader("Hello World\r\nWhatever!\r\nAnother line."));
                Assert.AreEqual(0, exitCode);

                // 0 == WorkingDirectory = 
                // 1 == std-input:
                Assert.AreEqual("Hello World", lines[2]);
                Assert.AreEqual("Whatever!", lines[3]);
                Assert.AreEqual("Another line.", lines[4]);
            }
        }

        [Test]
        public void TestRunWithWorkingDirectory()
        {
            using (TempDirectory dir = new TempDirectory())
            using (AssemblyRunner runner = new AssemblyRunner(Exe))
            {
                List<string> lines = new List<string>();
                runner.OutputReceived += delegate(Object o, ProcessOutputEventArgs e) { lines.Add(e.Data); };

                Assert.AreNotEqual(dir.TempPath, runner.WorkingDirectory);
                runner.WorkingDirectory = dir.TempPath;
                Assert.AreEqual(dir.TempPath, runner.WorkingDirectory);

                int exitCode = runner.Run();
                Assert.AreEqual(0, exitCode);
                Assert.AreEqual("WorkingDirectory = " + dir.TempPath, lines[0]);
            }
        }

        [Test]
        public void TestExitedEvent()
        {
            using (AssemblyRunner runner = new AssemblyRunner(Exe))
            {
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
        }

        [Test]
        public void TestExitedEventUnsubscribe()
        {
            using (AssemblyRunner runner = new AssemblyRunner(Exe))
            {
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
        }
        [Test]
        public void TestKill()
        {
            Environment.CurrentDirectory = Path.GetDirectoryName(FileUtils.FindFullPath("cmd.exe"));

            using (AssemblyRunner runner = new AssemblyRunner(Exe))
            {
                ManualResetEvent gotExit = new ManualResetEvent(false);
                runner.ProcessExited += delegate(Object o, ProcessExitedEventArgs e) { gotExit.Set(); };
                Assert.IsFalse(runner.IsRunning);
                runner.Kill(); // always safe to call

                runner.Start("-wait");
                Assert.IsTrue(runner.IsRunning);

                try //make sure we can't start twice.
                {
                    runner.Start();
                    Assert.Fail();
                }
                catch (InvalidOperationException)
                { }

                Assert.IsFalse(runner.WaitForExit(TimeSpan.FromSeconds(1), false));
                runner.Kill();
                Assert.IsFalse(runner.IsRunning);
                Assert.IsTrue(gotExit.WaitOne(30000, false));
            }
        }
    }
}
