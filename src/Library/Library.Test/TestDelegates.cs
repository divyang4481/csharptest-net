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
using System.Windows.Forms;
using System.Threading;
using CSharpTest.Net.Delegates;
using System.Diagnostics;
using System.Security.Permissions;
using System.Xml.Serialization;
using System.IO;
using System.Xml;

#pragma warning disable 1591

namespace CSharpTest.Net.Library.Test
{
	[TestFixture]
	[Category("TestDelegates")]
	public partial class TestDelegates
	{
		delegate void Action();
		#region TestFixture SetUp/TearDown
		[TestFixtureSetUp]
		public virtual void Setup()
		{
		}

		[TestFixtureTearDown]
		public virtual void Teardown()
		{
		}
		#endregion

		TestForm _form;
		Thread _thread;

		[SetUp]
		public void SetupTest() 
		{
			_form = new TestForm();
			_thread = new Thread(
				delegate() 
				{ 
					try 
					{ 
						using (_form) _form.ShowDialog(); 
					} 
					catch (Exception e) 
					{
						Console.Error.WriteLine(e.ToString());
						throw;
					} 
				}
			);
			_thread.SetApartmentState(ApartmentState.STA);
			_thread.Name = "UI Thread";
		}
		[TearDown]
		public void TeardownTest()
		{
			try
			{
				_form.Dispose();
			}
			catch (Exception e)
			{
				Console.Error.WriteLine(e.ToString());
			}
			_form = null;
			try
			{
				_thread.Abort();
			}
			catch (Exception e)
			{
				Console.Error.WriteLine(e.ToString());
			}
			_thread = null;
		}

		[Test][ExpectedException(typeof(ArgumentNullException))]
		public void TestNullControlExcpetion()
		{
		    VerifiedReceiptEventArgs args = new VerifiedReceiptEventArgs();
		    new EventHandlerForControl<VerifiedReceiptEventArgs>(null, VerifiedReceipt).EventHandler(null, args.Reset());
		}

		[Test][ExpectedException(typeof(ArgumentNullException))]
		public void TestNullDelegateExcpetion()
		{
		    VerifiedReceiptEventArgs args = new VerifiedReceiptEventArgs();
		    new EventHandlerForControl<VerifiedReceiptEventArgs>(_form, (EventHandler<VerifiedReceiptEventArgs>)null).EventHandler(null, args.Reset());
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void TestNullControlExcpetion2()
		{
			VerifiedReceiptEventArgs args = new VerifiedReceiptEventArgs();
			new EventHandlerForControl<VerifiedReceiptEventArgs>(null, (Delegate)new EventHandler<VerifiedReceiptEventArgs>(VerifiedReceipt)).EventHandler(null, args.Reset());
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void TestNullDelegateExcpetion2()
		{
			VerifiedReceiptEventArgs args = new VerifiedReceiptEventArgs();
			new EventHandlerForControl<VerifiedReceiptEventArgs>(_form, (Delegate)null).EventHandler(null, args.Reset());
		}

		[Test][ExpectedException(typeof(ObjectDisposedException))]
		public void TestNotCreated()
		{
		    VerifiedReceiptEventArgs args = new VerifiedReceiptEventArgs();
		    new EventHandlerForControl<VerifiedReceiptEventArgs>(_form, VerifiedReceipt).EventHandler(null, args.Reset());
		}

		[Test][ExpectedException(typeof(ObjectDisposedException))]
		public void TestDisposed()
		{
		    _thread.Start();
		    _form.FormCreated.WaitOne();
		    _form.BeginInvoke((Action)_form.Close);
		    _thread.Join();

		    Assert.IsTrue(_form.IsDisposed);

		    VerifiedReceiptEventArgs args = new VerifiedReceiptEventArgs();
		    new EventHandlerForControl<VerifiedReceiptEventArgs>(_form, VerifiedReceipt).EventHandler(null, args.Reset());
		}

		[Test]
		public void TestVisible()
		{
		    EventHandler<VerifiedReceiptEventArgs> handler;
		    VerifiedReceiptEventArgs args = new VerifiedReceiptEventArgs();
			
		    _thread.Start();
		    _form.FormCreated.WaitOne();

		    handler = new EventHandlerForControl<VerifiedReceiptEventArgs>(_form, VerifiedReceipt);
		    handler(null, args.Reset());
		    Assert.IsTrue(args.Received);
		    Assert.IsTrue(args.OnThread);

		    handler = new EventHandlerForControl<VerifiedReceiptEventArgs>(_form, new VerifiedReceiptEventHandler(VerifiedReceiptRedirect));
		    handler(null, args.Reset());
		    Assert.IsTrue(args.Received);
		    Assert.IsTrue(args.OnThread);

		    _form.BeginInvoke((Action)_form.Close);
		    _thread.Join();
		}

		[Test]
		public void TestWithClose()
		{
		    EventHandler<VerifiedReceiptEventArgs> handler;
		    VerifiedReceiptEventArgs args = new VerifiedReceiptEventArgs();

		    _thread.Start();
		    _form.FormCreated.WaitOne();

		    try
		    {
		        handler = new EventHandlerForControl<VerifiedReceiptEventArgs>(_form, CloseTheForm);
		        handler(null, args);
		    }
		    finally
		    {
		        _thread.Join();
		    }
		}

		[Test]
		public void TestRandomForced()
		{
			ForcedEventHandlerForControl<VerifiedReceiptEventArgs> handler;
			handler = new ForcedEventHandlerForControl<VerifiedReceiptEventArgs>(_form, VerifiedReceipt);
			handler = new ForcedEventHandlerForControl<VerifiedReceiptEventArgs>(_form, new VerifiedReceiptEventHandler(handler.EventHandler));
			VerifiedReceiptEventArgs args = new VerifiedReceiptEventArgs();
			_form.Shown += delegate(object s, EventArgs e) { System.Threading.Thread.Sleep(100); _form.Close(); };
			_thread.Start();
			int total = 0, countOnThread = 0, countOffThread = 0;
			try
			{
				for (total = 1; total < 1000000; total++)
				{
					handler.EventHandler(null, args.Reset());
					Assert.IsTrue(args.Received);
					if (args.OnThread)
						countOnThread++;
					else
						countOffThread++;
					//Assert.IsTrue(args.OnThread);
					//Console.Error.WriteLine("Threaded = {0}", args.OnThread);
					if (_form.IsDisposed)
						break;
				}
				Assert.AreEqual(total, countOnThread + countOffThread);
				Assert.IsTrue(_form.IsDisposed);
				Assert.AreNotEqual(0, countOnThread);
				Assert.AreNotEqual(0, countOffThread);
			}
			catch (Exception e)
			{
				Console.Error.WriteLine(e.ToString());
				throw;
			}
			finally
			{
				_thread.Join();
			}
		}

		[Test]
		public void TestRandomActive()
		{
			EventHandlerForActiveControl<VerifiedReceiptEventArgs> handler;
			handler = new EventHandlerForActiveControl<VerifiedReceiptEventArgs>(_form, VerifiedReceipt);
			handler = new EventHandlerForActiveControl<VerifiedReceiptEventArgs>(_form, new VerifiedReceiptEventHandler(handler.EventHandler));
			VerifiedReceiptEventArgs args = new VerifiedReceiptEventArgs();
			_form.Shown += delegate(object s, EventArgs e) { System.Threading.Thread.Sleep(100); _form.Close(); };
			_thread.Start();
			int total = 0, countOnThread = 0, countOffThread = 0;
			try
			{
				for (total = 1; total < 1000000; total++)
				{
					handler.EventHandler(null, args.Reset());
					if (args.OnThread)
					{
						Assert.IsTrue(args.Received);
						countOnThread++;
					}
					else
					{
						Assert.IsFalse(args.Received);
						countOffThread++;
					}
					//Assert.IsTrue(args.OnThread);
					//Console.Error.WriteLine("Threaded = {0}", args.OnThread);
					if (_form.IsDisposed)
						break;
				}
				Assert.AreEqual(total, countOnThread + countOffThread);
				Assert.IsTrue(_form.IsDisposed);
				Assert.AreNotEqual(0, countOnThread);
				Assert.AreNotEqual(0, countOffThread);
			}
			catch (Exception e)
			{
				Console.Error.WriteLine(e.ToString());
				throw;
			}
			finally
			{
				_thread.Join();
			}
		}

		void CloseTheForm(object sender, VerifiedReceiptEventArgs args)
		{
			_form.Dispose();

			while (!_form.IsDisposed)
				Application.DoEvents();

			VerifiedReceipt(sender, args);
		}

		void VerifiedReceipt(object sender, VerifiedReceiptEventArgs args)
		{
			args.Received = true;
			args.OnThread = Thread.CurrentThread.ManagedThreadId == _thread.ManagedThreadId;
		}

		void VerifiedReceiptRedirect(object sender, VerifiedReceiptEventArgs args)
		{
			EventHandler<VerifiedReceiptEventArgs> handler = new EventHandlerForControl<VerifiedReceiptEventArgs>(_form, VerifiedReceipt);
			System.Threading.Thread.Sleep(500);
			handler(null, args);
			System.Threading.Thread.Sleep(500);
		}

		delegate void VerifiedReceiptEventHandler(object sender, VerifiedReceiptEventArgs args);

		class VerifiedReceiptEventArgs : EventArgs
		{
			public bool Received = false;
			public bool OnThread = false;

			public VerifiedReceiptEventArgs Reset() { Received = false; OnThread = false; return this; }
		}

		class TestForm : Form
		{
			public readonly ManualResetEvent FormCreated = new ManualResetEvent(false);
			public readonly ManualResetEvent FormShown = new ManualResetEvent(false);
			//public readonly ManualResetEvent FormInDispose = new ManualResetEvent(false);

			public readonly ManualResetEvent FormInDestroy = new ManualResetEvent(false);
			public readonly ManualResetEvent ContinueDestroy = new ManualResetEvent(true);

			//bool created = false;

			protected override void CreateHandle()
			{
				//if (!created)
				//{
				//    created = true;
				    base.CreateHandle();
				//}
				//else
				//    throw new Exception();
					//System.Diagnostics.Debugger.Break();
			}

			protected override void OnHandleCreated(EventArgs e)
			{
				base.OnHandleCreated(e);
				FormCreated.Set();
			}

			protected override void OnShown(EventArgs e)
			{
				base.OnShown(e);
				FormShown.Set();
			}

			protected override void DestroyHandle()
			{
				FormInDestroy.Set();
				ContinueDestroy.WaitOne();
				lock(this)
					base.DestroyHandle();
			}
		}
	}
}
