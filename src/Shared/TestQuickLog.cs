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
using NUnit.Framework;

#pragma warning disable 1591
namespace CSharpTest.Net.Shared.Test
{
	[TestFixture]
	[Category("TestQuickLog")]
	public partial class TestQuickLog
	{
		protected static String _lastTrace;
		protected static String _lastMessage;
		private TraceListener _myListener;

		#region TestFixture SetUp/TearDown
		[TestFixtureSetUp]
		public virtual void Setup()
		{
			Log.LogWrite += new Log.LogEventHandler(Log_LogWrite);
			System.Diagnostics.Trace.Listeners.Add(_myListener = new TraceListener());
		}

		void Log_LogWrite(System.Reflection.MethodBase method, System.Diagnostics.TraceLevel level, string message)
		{
			_lastMessage = message;
		}

		[TestFixtureTearDown]
		public virtual void Teardown()
		{
			System.Diagnostics.Trace.Listeners.Remove(_myListener);
		}
		#endregion

		class TraceListener : System.Diagnostics.TraceListener
		{
			public override void Write(string message) { _lastTrace = message; }
			public override void WriteLine(string message) { Write(message); }
		}

		[Test]
		public void Test()
		{
			string prefix = String.Format("{0}: {1:D2}", this.GetType().FullName, System.Threading.Thread.CurrentThread.ManagedThreadId);

			using (Log.AppStart("Hi", "one", "two", null))
				Assert.AreEqual(prefix + " Verbose - Start Hi   at Void Test()", _lastTrace);

			using (Log.Start("what", "one", "two", null))
			{
				Assert.AreEqual(prefix + " Verbose - Start what   at Void Test()", _lastTrace);
				
				Log.Error(new Exception());
				Assert.AreEqual(prefix + "   Error - System.Exception: Exception of type 'System.Exception' was thrown.   at Void Test()", _lastTrace);
				
				Log.Warning(new Exception());
				Assert.AreEqual(prefix + " Warning - System.Exception: Exception of type 'System.Exception' was thrown.   at Void Test()", _lastTrace);
				
				Log.Error("Test {0}", System.Diagnostics.TraceLevel.Error);
				Assert.AreEqual(prefix + "   Error - Test Error   at Void Test()", _lastTrace);
				
				Log.Warning("Test {0}", System.Diagnostics.TraceLevel.Warning);
				Assert.AreEqual(prefix + " Warning - Test Warning   at Void Test()", _lastTrace);
				
				Log.Info("Test {0}", System.Diagnostics.TraceLevel.Info);
				Assert.AreEqual(prefix + "    Info - Test Info   at Void Test()", _lastTrace);
				
				Log.Verbose("Test {0}", System.Diagnostics.TraceLevel.Verbose);
				Assert.AreEqual(prefix + " Verbose - Test Verbose   at Void Test()", _lastTrace);

				Log.Write(System.Diagnostics.TraceLevel.Info, "Test {0}", System.Diagnostics.TraceLevel.Info);
				Assert.AreEqual(prefix + "    Info - Test Info   at Void Test()", _lastTrace);

				Log.Write("Test Boom! {5}", 1, 2, 3);
			}

			Log.AppStart("{5}", 1, 2).Dispose();
			Log.Start("{5}", 1, 2).Dispose();

			Log.Error((Exception)null);
			Assert.AreEqual(prefix + "   Error -    at Void Test()", _lastTrace);

			Log.Close();
		}

		[Test]
		public void TestWithRemote()
		{
			Log.ILog Log = global::Log.RemoteLog;
			string prefix = String.Format("{0}: {1:D2}", this.GetType().FullName, System.Threading.Thread.CurrentThread.ManagedThreadId);

			using (Log.AppStart("Hi", "one", "two", null))
				Assert.AreEqual(prefix + " Verbose - Start Hi   at Void TestWithRemote()", _lastTrace);

			using (Log.Start("what", "one", "two", null))
			{
				Assert.AreEqual(prefix + " Verbose - Start what   at Void TestWithRemote()", _lastTrace);

				Log.Error(new Exception());
				Assert.AreEqual(prefix + "   Error - System.Exception: Exception of type 'System.Exception' was thrown.   at Void TestWithRemote()", _lastTrace);

				Log.Warning(new Exception());
				Assert.AreEqual(prefix + " Warning - System.Exception: Exception of type 'System.Exception' was thrown.   at Void TestWithRemote()", _lastTrace);

				Log.Error("Test {0}", System.Diagnostics.TraceLevel.Error);
				Assert.AreEqual(prefix + "   Error - Test Error   at Void TestWithRemote()", _lastTrace);

				Log.Warning("Test {0}", System.Diagnostics.TraceLevel.Warning);
				Assert.AreEqual(prefix + " Warning - Test Warning   at Void TestWithRemote()", _lastTrace);

				Log.Info("Test {0}", System.Diagnostics.TraceLevel.Info);
				Assert.AreEqual(prefix + "    Info - Test Info   at Void TestWithRemote()", _lastTrace);

				Log.Verbose("Test {0}", System.Diagnostics.TraceLevel.Verbose);
				Assert.AreEqual(prefix + " Verbose - Test Verbose   at Void TestWithRemote()", _lastTrace);

				Log.Write("Test Boom! {5}", 1, 2, 3);
			}

			Log.AppStart("{5}", 1, 2).Dispose();
			Log.Start("{5}", 1, 2).Dispose();

			Log.Error((Exception)null);
			Assert.AreEqual(prefix + "   Error -    at Void TestWithRemote()", _lastTrace);
		}


		[Test]
		public void TestReOpenClose()
		{
			Log.Open();
			Log.Open();
			Log.Close();
			Log.Close();
			Log.Close();
			Log.Open();
			Log.Close();
			Log.Close();
		}
	}
}
