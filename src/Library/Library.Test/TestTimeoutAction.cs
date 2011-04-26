#region Copyright 2010-2011 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Threading;
using CSharpTest.Net.Delegates;
using NUnit.Framework;

namespace CSharpTest.Net.Library.Test
{
	[TestFixture]
	public class TestTimeoutAction
	{
		[Test]
		public void TestTimeoutOccurs()
		{
			ManualResetEvent called = new ManualResetEvent(false);
			new TimeoutAction(TimeSpan.FromMilliseconds(100), delegate { called.Set(); });
			Assert.IsFalse(called.WaitOne(0, false));
			Assert.IsTrue(called.WaitOne(1000, false));
		}

		[Test]
		public void TestTimeoutOccursForDispose()
		{
			bool called = false;
			TimeoutAction action = new TimeoutAction(TimeSpan.FromHours(1), delegate { called = true; });
			Assert.AreEqual(false, called);
			action.Dispose();
			Assert.AreEqual(true, called);
		}

		[Test]
		public void TestTimeoutCalledOnce()
		{
			int callCount = 0;
			TimeoutAction action = new TimeoutAction(TimeSpan.FromHours(1), delegate { callCount++; });
			Assert.AreEqual(0, callCount);
			action.Dispose();
			Assert.AreEqual(1, callCount);
			action.Dispose();
			Assert.AreEqual(1, callCount);
		}

		[Test]
		public void TestTimeoutWithNoReference()
		{
			ManualResetEvent h = new ManualResetEvent(false);
			new TimeoutAction(TimeSpan.FromMilliseconds(200), delegate { h.Set(); });
			Assert.AreEqual(false, h.WaitOne(0, false));
			//ensure that even if we fail to keep a reference to the object, the timer reference should
			//keep the object alive until the timer expires.
			GC.Collect(0, GCCollectionMode.Forced);
			GC.WaitForPendingFinalizers();
			Assert.AreEqual(false, h.WaitOne(0, false));
			//and it still will be called once we complete the timeout period
			Assert.IsTrue(h.WaitOne(1000, false));
		}

		[Test]
		public void TestTimeoutExceptionDefaultHandler()
		{
			bool wasThrown = false;
			new TimeoutAction(TimeSpan.FromHours(1), delegate { wasThrown = true; throw new ApplicationException("Should be caught"); })
			.Dispose();

			Assert.IsTrue(wasThrown);
		}

		[Test]
		public void TestTimeoutExceptionHandler()
		{
			Exception appErr = null;
			bool wasThrown = false;
			new TimeoutAction(TimeSpan.FromHours(1), 
				delegate { wasThrown = true; throw new ApplicationException("Should be caught"); },
				delegate(Exception ex) { appErr = ex; })
			.Dispose();

			Assert.IsTrue(wasThrown);
			Assert.IsNotNull(appErr);
			Assert.AreEqual("Should be caught", appErr.Message);
		}

		[Test]
		public void TestTimeoutStart()
		{
			ManualResetEvent h = new ManualResetEvent(false);
			TimeoutAction.Start(TimeSpan.FromMilliseconds(1), delegate { h.Set(); });
			Assert.IsTrue(h.WaitOne(1000, false));
		}

		[Test]
		public void TestTimeoutStartWithArg()
		{
			ManualResetEvent h = new ManualResetEvent(false);
			TimeoutAction.Start(TimeSpan.FromMilliseconds(1), delegate(ManualResetEvent mre) { mre.Set(); }, h);
			Assert.IsTrue(h.WaitOne(1000, false));
		}

		[Test]
		public void TestTimeoutStartWithErrorHandler()
		{
			ManualResetEvent h = new ManualResetEvent(false);
			TimeoutAction.Start(TimeSpan.FromMilliseconds(1), delegate { throw new ApplicationException(); }, delegate(Exception err) { h.Set(); });
			Assert.IsTrue(h.WaitOne(1000, false));
		}

		[Test]
		public void TestTimeoutStartWithArgAndErrorHandler()
		{
			ManualResetEvent h1 = new ManualResetEvent(false);
			ManualResetEvent h2 = new ManualResetEvent(false);
			TimeoutAction.Start(TimeSpan.FromMilliseconds(1), delegate(ManualResetEvent mre) { mre.Set(); throw new ApplicationException(); }, h1, delegate(Exception err) { h2.Set(); });
			Assert.IsTrue(h1.WaitOne(1000, false));
			Assert.IsTrue(h2.WaitOne(1000, false));
		}

		[Test]
		public void TestTimeoutFiresOnUnload()
		{
			ManualResetEvent h = new ManualResetEvent(false);
			TimeoutAction action = new TimeoutAction(TimeSpan.FromDays(1), delegate { h.Set(); });
			Action delete = (Action)Delegate.CreateDelegate(typeof(Action), action, "Finalize", true, true);

			Assert.IsFalse(h.WaitOne(0, false));
			delete();
			Assert.IsTrue(h.WaitOne(0, false));
		}
	}
}
