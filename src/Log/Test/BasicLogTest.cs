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
using System.Threading;
using NUnit.Framework;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using System.Xml;

#pragma warning disable 1591
namespace CSharpTest.Net.Logging.Test
{
	[TestFixture][Category("Logging")]
	public partial class BasicLogTest : TraceListeningTest
	{
		#region TestFixture SetUp/TearDown
		protected ManualResetEvent _message = new ManualResetEvent(false);
		protected List<EventData> _lastMessages = new List<EventData>();
		protected String UniqueData;

		[SetUp]
		public virtual void StartTest()
		{
			Log.Config.Level = LogLevels.Verbose; //we occasion much with this for testing...
			_message.Reset();
			_lastMessages.Clear();
			_lastTrace = null;

			_rout.GetStringBuilder().Length = 0;
			_rerror.GetStringBuilder().Length = 0;

			UniqueData = Guid.NewGuid().ToString();
		}

		[TearDown]
		public virtual void StopTest()
		{}

		TextWriter _out, _error;
		StringWriter _rout, _rerror;

		[TestFixtureSetUp]
		public override void Setup()
		{
			base.Setup();

			_out = Console.Out;
			_error = Console.Error;
			Console.SetOut(_rout = new StringWriter());
			Console.SetError(_rerror = new StringWriter());

			Log.LogWrite += new LogEventHandler(Log_LogWrite);

			if (Log.Config.Level != LogLevels.Verbose) Log.Config.Level = LogLevels.Verbose;

			// To ensure that the log is functioning properly we will set ALL output devices (save for EventLog since we don't have a well-known source)
			Log.Config.Output = LogOutputs.AspNetTrace | LogOutputs.Console | LogOutputs.LogFile | LogOutputs.TraceWrite;
			NextMessage.ToString();
			Log.Config.Options = LogOptions.GZipLogFileOnRoll | LogOptions.ConsoleColors | LogOptions.LogAddAssemblyInfo | LogOptions.LogAddFileInfo | LogOptions.LogImmediateCaller | LogOptions.LogNearestCaller;
			NextMessage.ToString();
			Log.Config.Level = LogLevels.Verbose;
			NextMessage.ToString();
			Log.Config.SetOutputFormat(LogOutputs.TraceWrite, "{Message}");
			NextMessage.ToString();
		}

		[TestFixtureTearDown]
		public override void Teardown()
		{
			Log.LogWrite -= new LogEventHandler(Log_LogWrite);
			Console.SetOut(_out);
			Console.SetError(_error);

			base.Teardown();
		}

		void Log_LogWrite(object sender, LogEventArgs args)
		{
			if (args.Count > 0)
			{
				lock (_lastMessages)
				{
					Assert.AreEqual(args.Count, args.ToArray().Length);
					Assert.IsNotNull(((System.Collections.IEnumerable)args).GetEnumerator());
					Assert.AreNotEqual(String.Empty, args.ToString());

					_lastMessages.AddRange(args);
					_message.Set();
				}
			}
		}

		protected EventData NextMessage
		{
			get
			{
				if (_lastMessages.Count == 0)
				{
					lock (_lastMessages)
					{ if (_lastMessages.Count == 0) _message.Reset(); }
					Assert.IsTrue(_message.WaitOne(300, false), "failed to get a message");
				}
				EventData msg;
				lock (_lastMessages)
				{
					msg = _lastMessages[0];
					_lastMessages.RemoveAt(0);
				}
				return msg;
			}
		}

		protected EventData LastMessage
		{
			get
			{
				if (_lastMessages.Count == 0)
				{
					lock (_lastMessages)
						{ if( _lastMessages.Count == 0 ) _message.Reset(); }
			
					Assert.IsTrue(_message.WaitOne(300, false), "failed to get a message");
				}
				EventData[] data;
				lock (_lastMessages)
				{
					data = _lastMessages.ToArray();
					_lastMessages.Clear();
					_message.Reset();
				}

				Assert.IsTrue(data.Length > 0, "no messages");
				EventData last = data[data.Length - 1];
				Assert.AreEqual(String.Format("{0}: {1}", last.MethodType, last.Message), _lastTrace);
				return last;
			}
		}
		#endregion

		internal void AssertMessage(EventData data, LogLevels level, string expected, Type expectedError)
		{ AssertMessage(typeof(BasicLogTest), null, data, level, expected, expectedError); }
		internal static void AssertMessage(Type from, string[] stack, EventData data, LogLevels level, string expected, Type expectedError)
		{
			DateTime time = DateTime.Now;

			Assert.AreEqual(AppDomain.CurrentDomain.FriendlyName, data.AppDomainName);
			Assert.AreEqual("nunit.core", data.EntryAssembly.ToLower());

			Assert.Less(0, data.EventId);
			Assert.IsTrue(data.EventTime <= DateTime.Now);
			if (!System.Diagnostics.Debugger.IsAttached)
				Assert.IsTrue((time - data.EventTime).TotalMilliseconds < 500);
			if (expectedError == null)
				Assert.IsNull(data.Exception);
			else
				Assert.AreEqual(data.Exception.GetType(), expectedError);
			Assert.Less(0, data.FileColumn, "no FileColumn");
			Assert.Less(0, data.FileLine, "no FileLine");
			Assert.IsTrue(data.FileLocation.Contains(data.FileName));
			Assert.AreEqual(from.Name + ".cs", Path.GetFileName(data.FileName));
			if(expected != null) Assert.IsTrue(data.FullMessage.Contains(expected));
			Assert.IsTrue(data.FullMessage.Contains(data.FileName));
			//ROK - not always available, optimizations can disable
			//Assert.Less(0, data.IlOffset, "no ILOffset");
			Assert.Less(-1, data.IlOffset, "no ILOffset");
			Assert.AreEqual(level, data.Level);
			Assert.IsTrue(data.Location.Contains(String.Format("{0}.{1}(", from.FullName, data.MethodName)));
			if (stack == null)
			{
				Assert.IsNull(data.LogCurrent);
				Assert.IsNull(data.LogStack);
			}
			else
			{
				Assert.AreEqual(stack[stack.Length - 1], data.LogCurrent);
				Assert.AreEqual(String.Join("::", stack), data.LogStack);
			}
			Assert.AreEqual(Thread.CurrentThread.ManagedThreadId, data.ManagedThreadId);
			Assert.AreEqual(Thread.CurrentThread.Name, data.ManagedThreadName);
			if (expected != null) Assert.AreEqual(expected, data.Message);
			Assert.IsTrue(data.Method.StartsWith(from.FullName));
			Assert.IsTrue(data.Method.EndsWith(")"));
			Assert.IsNotNull(data.MethodArgs);
			Assert.AreEqual(from.Assembly.GetName().Name, data.MethodAssembly);
			Assert.AreEqual(from.Assembly.GetName().Version.ToString(), data.MethodAssemblyVersion);
			Assert.IsNotEmpty(data.MethodName);
			Assert.AreEqual(from.FullName, data.MethodType);
			Assert.AreEqual(from.Name, data.MethodTypeName);
			Assert.AreEqual(Log.Config.Output, data.Output);
			Assert.AreEqual(Process.GetCurrentProcess().Id, data.ProcessId);
			Assert.AreEqual("nunit-console", Path.GetFileNameWithoutExtension( data.ProcessName ).ToLower());
			Assert.IsTrue(String.IsNullOrEmpty(data.ThreadPrincipalName));
			
			Assert.IsNotEmpty(data.ToXml());
			new XmlDocument().LoadXml(data.ToXml());

			StringWriter sw = new StringWriter();
			data.Write(sw);
			if (expected != null) Assert.IsTrue(sw.ToString().Contains(expected));

			if (expected != null)
			{
				Assert.AreEqual(expected, data.ToString("{Message}"));
				Assert.AreEqual("!" + expected + "-", data.ToString("{Message:!%s-}"));
				Assert.AreEqual(expected, data.ToString("{Message}{ThreadPrincipalName}"));

				StringWriter wtr = new StringWriter();
				data.Write(wtr, "{Message}");
				Assert.AreEqual(expected, wtr.ToString().Trim());
			}
		}

		[Test]
		public void TestAppStart()
		{
			using (Log.AppStart(UniqueData))
			{
				AssertMessage(NextMessage, LogLevels.Verbose, "AppStart " + UniqueData, null);
			}

			EventData msg = LastMessage;
			Assert.IsTrue(msg.Message.StartsWith("End " + UniqueData));
		}

		[Test]
		public void TestStartStop()
		{
			Log.Write("");
			EventData msg = LastMessage;
			Assert.IsNull(msg.LogCurrent);
			Assert.IsNull(msg.LogStack);

			using (Log.Start(UniqueData))
			{
				msg = LastMessage;
				Assert.AreEqual("Start " + UniqueData, msg.Message);
				Assert.AreEqual(UniqueData, msg.LogCurrent);
				Assert.AreEqual(msg.LogCurrent, msg.LogStack);

				using (Log.Start("2ndlevel"))
				{
					msg = LastMessage;
					Assert.AreEqual("Start 2ndlevel", msg.Message);
					Assert.AreEqual("2ndlevel", msg.LogCurrent);
					Assert.AreEqual(UniqueData + "::2ndlevel", msg.LogStack);

					Assert.IsTrue(msg.ToXml().Contains("2ndlevel"));
					Assert.AreEqual("2ndlevel", msg.ToString("{LogCurrent}"));
				}
				msg = LastMessage;
				Assert.IsTrue(msg.Message.StartsWith("End 2ndlevel"));
				Assert.AreEqual(UniqueData, msg.LogCurrent);
				Assert.AreEqual(msg.LogCurrent, msg.LogStack);
			}
			msg = LastMessage;
			Assert.IsTrue(msg.Message.StartsWith("End " + UniqueData));
			Assert.IsNull(msg.LogCurrent);
			Assert.IsNull(msg.LogStack);
		}

		[Test]
		public void TestClearStack()
		{
			Log.Write(UniqueData);
			EventData msg = LastMessage;
			Assert.IsNull(msg.LogCurrent);
			Assert.IsNull(msg.LogStack);

			using (Log.Start(UniqueData))
			{
				msg = LastMessage;
				Assert.AreEqual("Start " + UniqueData, msg.Message);
				Assert.AreEqual(UniqueData, msg.LogCurrent);
				Assert.AreEqual(msg.LogCurrent, msg.LogStack);

				Log.ClearStack();
				Log.Warning("Stack cleared");
				msg = LastMessage;
				Assert.IsNull(msg.LogCurrent);
				Assert.IsNull(msg.LogStack);
			}

			//no message generated.
			Assert.AreEqual(0, _lastMessages.Count);
			Assert.IsNull(msg.LogCurrent);
			Assert.IsNull(msg.LogStack);
		}

		[Test]
		public void TestIsEnabled()
		{
			Assert.IsTrue(Log.IsVerboseEnabled);
			Assert.IsTrue(Log.IsInfoEnabled);
			
			Log.Config.Level = LogLevels.Info;
			NextMessage.ToString();
			Assert.IsFalse(Log.IsVerboseEnabled);
			Assert.IsTrue(Log.IsInfoEnabled);

			_lastTrace = null;
			_lastMessages.Clear();

			Log.Verbose("Test Verbose off");
			Assert.IsNull(_lastTrace);
			Assert.AreEqual(0, _lastMessages.Count);
			
			Log.Config.Level = LogLevels.Warning;
			Assert.IsFalse(Log.IsInfoEnabled);

			Log.Verbose("Test Info off");
			Assert.IsNull(_lastTrace);
			Assert.AreEqual(0, _lastMessages.Count);
	
			Log.Config.Level = LogLevels.None;
			Assert.IsFalse(Log.IsInfoEnabled);
			Assert.IsFalse(Log.IsVerboseEnabled);

			Log.Critical("Test All off");
			Assert.IsNull(_lastTrace);
			Assert.AreEqual(0, _lastMessages.Count);

			Log.Write("Test PassThrough?");
			Assert.AreEqual("Test PassThrough?", LastMessage.Message);

			Log.Config.Level = LogLevels.Verbose;
			Assert.IsTrue(Log.IsVerboseEnabled);
			Assert.IsTrue(Log.IsInfoEnabled);
		}

		[Test]
		public void TestWriteThrough()
		{
			Log.Config.Level = LogLevels.None;
			NextMessage.ToString();
			Assert.IsFalse(Log.IsVerboseEnabled);
			Assert.IsFalse(Log.IsInfoEnabled);

			_lastTrace = null;
			_lastMessages.Clear();

			Log.Critical("Test All off");
			Assert.IsNull(_lastTrace);
			Assert.AreEqual(0, _lastMessages.Count);

			Log.Write("Test PassThrough?");
			EventData data = LastMessage;
			Assert.AreEqual("Test PassThrough?", data.Message);
		}

		[Test]
		public void TestCritical()
		{
			Exception e = new PlatformNotSupportedException();
			Log.Critical(e);
			AssertMessage(LastMessage, LogLevels.Critical, e.Message, e.GetType());

			Log.Critical(e, "a-->{0}", UniqueData);
			AssertMessage(LastMessage, LogLevels.Critical, String.Format("a-->{0}", UniqueData), e.GetType());

			Log.Critical("b-->{0}", UniqueData);
			AssertMessage(LastMessage, LogLevels.Critical, String.Format("b-->{0}", UniqueData), null);

			Log.Config.Level = LogLevels.None;
			LastMessage.ToString();

			Log.Critical(e);
			Assert.AreEqual(0, _lastMessages.Count);
			Log.Critical(e, "a-->{0}", UniqueData);
			Assert.AreEqual(0, _lastMessages.Count);
			Log.Critical("b-->{0}", UniqueData);
			Assert.AreEqual(0, _lastMessages.Count);
		}

		[Test]
		public void TestError()
		{
			Exception e = new PlatformNotSupportedException();
			Log.Error(e);
			AssertMessage(LastMessage, LogLevels.Error, e.Message, e.GetType());

			Log.Error(e, "a-->{0}", UniqueData);
			AssertMessage(LastMessage, LogLevels.Error, String.Format("a-->{0}", UniqueData), e.GetType());

			Log.Error("b-->{0}", UniqueData);
			AssertMessage(LastMessage, LogLevels.Error, String.Format("b-->{0}", UniqueData), null);

			Log.Config.Level = LogLevels.None;
			LastMessage.ToString();

			Log.Error(e);
			Assert.AreEqual(0, _lastMessages.Count);
			Log.Error(e, "a-->{0}", UniqueData);
			Assert.AreEqual(0, _lastMessages.Count);
			Log.Error("b-->{0}", UniqueData);
			Assert.AreEqual(0, _lastMessages.Count);
		}

		[Test]
		public void TestWarning()
		{
			Exception e = new PlatformNotSupportedException();
			Log.Warning(e);
			AssertMessage(LastMessage, LogLevels.Warning, e.Message, e.GetType());

			Log.Warning(e, "a-->{0}", UniqueData);
			AssertMessage(LastMessage, LogLevels.Warning, String.Format("a-->{0}", UniqueData), e.GetType());

			Log.Warning("b-->{0}", UniqueData);
			AssertMessage(LastMessage, LogLevels.Warning, String.Format("b-->{0}", UniqueData), null);

			Log.Config.Level = LogLevels.None;
			LastMessage.ToString();

			Log.Warning(e);
			Assert.AreEqual(0, _lastMessages.Count);
			Log.Warning(e, "a-->{0}", UniqueData);
			Assert.AreEqual(0, _lastMessages.Count);
			Log.Warning("b-->{0}", UniqueData);
			Assert.AreEqual(0, _lastMessages.Count);
		}

		[Test]
		public void TestInfo()
		{
			Exception e = new PlatformNotSupportedException();
			Log.Info(e);
			AssertMessage(LastMessage, LogLevels.Info, e.Message, e.GetType());

			Log.Info(e, "a-->{0}", UniqueData);
			AssertMessage(LastMessage, LogLevels.Info, String.Format("a-->{0}", UniqueData), e.GetType());

			Log.Info("b-->{0}", UniqueData);
			AssertMessage(LastMessage, LogLevels.Info, String.Format("b-->{0}", UniqueData), null);

			Log.Config.Level = LogLevels.None;
			LastMessage.ToString();

			Log.Info(e);
			Assert.AreEqual(0, _lastMessages.Count);
			Log.Info(e, "a-->{0}", UniqueData);
			Assert.AreEqual(0, _lastMessages.Count);
			Log.Info("b-->{0}", UniqueData);
			Assert.AreEqual(0, _lastMessages.Count);
		}

		[Test]
		public void TestVerbose()
		{
			Exception e = new PlatformNotSupportedException();
			Log.Verbose(e);
			AssertMessage(LastMessage, LogLevels.Verbose, e.Message, e.GetType());

			Log.Verbose(e, "a-->{0}", UniqueData);
			AssertMessage(LastMessage, LogLevels.Verbose, String.Format("a-->{0}", UniqueData), e.GetType());

			Log.Verbose("b-->{0}", UniqueData);
			AssertMessage(LastMessage, LogLevels.Verbose, String.Format("b-->{0}", UniqueData), null);

			Log.Config.Level = LogLevels.None;
			LastMessage.ToString();

			Log.Verbose(e);
			Assert.AreEqual(0, _lastMessages.Count);
			Log.Verbose(e, "a-->{0}", UniqueData);
			Assert.AreEqual(0, _lastMessages.Count);
			Log.Verbose("b-->{0}", UniqueData);
			Assert.AreEqual(0, _lastMessages.Count);
		}
	}
}
