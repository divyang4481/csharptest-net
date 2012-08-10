#region Copyright 2008-2012 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Diagnostics;
using System.Threading;
using System.IO;

#pragma warning disable 1591
namespace CSharpTest.Net.Logging.Test
{
	[TestFixture]
	[Category("ThreadedLogTest")]
	public partial class ThreadedLogTest : BasicLogTest
	{
		IDisposable _app = null;

		public override void StartTest()
		{
			base.StartTest();
			_app = Log.AppStart("Test Start");
			NextMessage.ToString();
			NextMessage.ToString();
		}

		public override void StopTest()
		{
			_app.Dispose();
			base.StopTest();
		}

		ManualResetEvent _isBlocked = new ManualResetEvent(false);
		ManualResetEvent _releaseBlock = new ManualResetEvent(false);


		[Test]
		public void TestThreading()
		{
			Log.LogWrite += new LogEventHandler(Block_LogWrite);
			try
			{
				using (Log.Start("Blocking Test"))
				{
					//the start above should already get us in a blocked state:
					Assert.IsTrue(_isBlocked.WaitOne(300, false));

					//regaurdless, we should still get the first message
					EventData msg = NextMessage;
					Assert.AreEqual("Start Blocking Test", msg.Message);

					//Now let's just go nuts on the logger...
					for (int i = 0; i < 100; i++)
						Log.Write("Buffering at {0}%.", i);

					Thread.Sleep(100);
					Assert.AreEqual(0, _lastMessages.Count);
					_releaseBlock.Set();
					Thread.Sleep(100);
					Assert.IsFalse(_isBlocked.WaitOne(0, false));

					for (int i = 0; i < 100; i++)
						Assert.IsTrue(NextMessage.Message.StartsWith("Buffering at"));
				}
			}
			finally
			{
				Log.LogWrite -= new LogEventHandler(Block_LogWrite);
			}
		}

		void Block_LogWrite(object sender, LogEventArgs args)
		{
			_isBlocked.Set();
			_releaseBlock.WaitOne(5000, false);
			_isBlocked.Reset();
		}

	}
}
