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
using System.IO;
using System.Threading;
using CSharpTest.Net.IpcChannel;
using Microsoft.Win32;
using NUnit.Framework;

namespace CSharpTest.Net.Library.Test
{
    [TestFixture]
    public class TestIpcChannel
    {
        const string KeyPath = @"CSharpTest.Net\IpcChannelTest";
        readonly string _channel = Guid.NewGuid().ToString();
        IIpcChannelRegistrar _registrar;

        #region TestFixture SetUp/TearDown
        [TestFixtureSetUp]
        public virtual void Setup()
        {
            _registrar = new IpcChannelRegistrar(Registry.CurrentUser, KeyPath);
        }

        [TestFixtureTearDown]
        public virtual void Teardown()
        {
            _registrar = null;
            Registry.CurrentUser.DeleteSubKeyTree(KeyPath);
        }
        #endregion

        [Test]
        public void TestChannelName()
        {
            using (IpcEventChannel channel = new IpcEventChannel(_registrar, _channel))
                Assert.AreEqual(_channel, channel.ChannelName);
        }

        [Test]
        public void TestChannelRegistrar()
        {
            using (IpcEventChannel channel = new IpcEventChannel(_registrar, _channel))
                Assert.IsTrue(ReferenceEquals(_registrar, channel.Registrar));
        }

        [Test]
        public void TestChannelInCallFalse()
        {
            using (IpcEventChannel channel = new IpcEventChannel(_registrar, _channel))
                Assert.IsFalse(channel.InCall);
        }

        [Test]
        public void TestChannelRaiseLocalInCallTrue()
        {
            bool wasInCall = false;
            using (IpcEventChannel channel = new IpcEventChannel(_registrar, _channel))
            {
                channel["Test"].OnEvent += delegate(object o, IpcSignalEventArgs e)
                { wasInCall = true; };
                channel.RaiseLocal("Test", new string[0]);
            }
            Assert.IsTrue(wasInCall);
        }

        [Test]
        public void TestChannelEventList()
        {
            using (IpcEventChannel channel = new IpcEventChannel(_registrar, _channel))
            {
                Assert.AreEqual("Test", channel["Test"].LocalName);
                Assert.AreEqual("TEST", channel["TEST"].LocalName);
                Assert.AreEqual("test", channel["test"].LocalName);

                int count = 0;
                foreach (IpcEvent e in channel.GetEvents())
                {
                    Assert.AreEqual("test", e.LocalName.ToLower());
                    count++;
                }
                Assert.AreEqual(3, count);
            }
        }

        [Test]
        public void TestChannelStartStop()
        {
            using (IpcEventChannel channel = new IpcEventChannel(_registrar, _channel))
            {
                channel.StartListening();
                channel.StopListening();
                channel.StartListening();
                channel.StopListening();
            }
        }

        [Test]
        public void TestChannelSelfDispatch()
        {
            int count = 0;
            using (IpcEventChannel channel = new IpcEventChannel(_registrar, _channel))
            {
                channel.StartListening();
                channel["Test"].OnEvent += delegate(object o, IpcSignalEventArgs e)
                                               { count++; };

                channel.Broadcast("Test");
                channel.Broadcast("Test");
                channel.Broadcast("Test");
                channel.StopListening();
            }
            Assert.AreEqual(3, count);
        }

        [Test]
        public void TestChannelAbortPending()
        {
            AutoResetEvent finish = new AutoResetEvent(false);
            AutoResetEvent done = new AutoResetEvent(false);
            int count = 0;
            using (IpcEventChannel channel = new IpcEventChannel(_registrar, _channel))
            {
                channel.EnableAsyncSend();
                channel.StartListening();
                channel["Test"].OnEvent += delegate(object o, IpcSignalEventArgs e)
                { finish.WaitOne(); count++; done.Set(); };

                Assert.AreEqual(1, channel.Broadcast(10000, "Test"));
                Assert.AreEqual(0, channel.Broadcast(0, "Test"));
                finish.Set();
                done.WaitOne();
            }
            Assert.AreEqual(1, count);
        }

        [Test]
        public void TestChannelSenderReciever()
        {
            int count = 0;
            using (IpcEventChannel sender = new IpcEventChannel(_registrar, _channel))
            using (IpcEventChannel reciever = new IpcEventChannel(_registrar, _channel))
            {
                reciever.StartListening();
                reciever["Test"].OnEvent += delegate(object o, IpcSignalEventArgs e)
                { count++; };

                for(int i=0; i < 1000; i++ )
                    Assert.AreEqual(1, sender.Broadcast(10000, "Test"));
    
                reciever.StopListening();
            }
            Assert.AreEqual(1000, count);
        }

        [Test]
        public void TestChannelNamedInstance()
        {
            int ch1count = 0, ch2count = 0;
            using (IpcEventChannel ch1 = new IpcEventChannel(_registrar, _channel))
            using (IpcEventChannel ch2 = new IpcEventChannel(_registrar, _channel))
            using (IpcEventChannel sender = new IpcEventChannel(_registrar, _channel))
            {
                ch1.StartListening("ch1");
                ch2.StartListening("ch2");
                sender.EnableAsyncSend();

                ch1["Test"].OnEvent += delegate(object o, IpcSignalEventArgs e)
                { ch1count++; };
                ch2["Test"].OnEvent += delegate(object o, IpcSignalEventArgs e)
                { ch2count++; };

                for (int i = 0; i < 1000; i++)
                    Assert.AreEqual(1, sender.SendTo(10000, i % 2 == 0 ? "ch1" : "ch2", "Test"));

                sender.StopAsyncSending(true, -1);
                ch1.StopListening();
                ch2.StopListening();
            }
            Assert.AreEqual(500, ch1count);
            Assert.AreEqual(500, ch2count);
        }

        [Test]
        public void TestBroadcastWithNonResponsive()
        {
            ManualResetEvent wait = new ManualResetEvent(false);
            int ch1count = 0, ch2count = 0;
            using (IpcEventChannel ch1 = new IpcEventChannel(_registrar, _channel))
            using (IpcEventChannel ch2 = new IpcEventChannel(_registrar, _channel))
            using (IpcEventChannel sender = new IpcEventChannel(_registrar, _channel))
            {
                ch1.DefaultTimeout = 100;
                ch2.DefaultTimeout = 100;
                sender.ExecutionTimeout = 0;

                ch1.StartListening("ch1");
                ch2.StartListening("ch2");
                sender.EnableAsyncSend();

                ch1["Test"].OnEvent += delegate(object o, IpcSignalEventArgs e)
                { ch1count++; };
                ch2["Test"].OnEvent += delegate(object o, IpcSignalEventArgs e)
                { ch2count++; wait.WaitOne(); };

                sender.Broadcast(100, "Test");
                sender.Broadcast(100, "Test");
                sender.Broadcast(100, "Test");
                sender.StopAsyncSending(false, 0);
                ch1.StopListening();
            }
            //We don't need to release the thread by wait.Set(), the thread was aborted by the 
            //Dispose()... Had we called ch2.StopListening() instead, the program would hang.  
            //thus to gracefully shutdown one calls StopListening()
            Assert.AreEqual(3, ch1count);
            Assert.AreEqual(1, ch2count);
        }
        
        [Test]
        public void TestIpcChannelInstances()
        {
            using (IpcEventChannel ch1 = new IpcEventChannel(@"CSharpTest.Net\IpcChannelTest", _channel))
            using (IpcEventChannel ch2 = new IpcEventChannel(@"CSharpTest.Net\IpcChannelTest", _channel))
            {
                Assert.AreNotEqual(ch1.InstanceId, ch2.InstanceId);
            }
        }
        
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestInvalidEventName()
        {
            using (IpcEventChannel ch1 = new IpcEventChannel(_registrar, _channel))
            {
                ch1["_-Can_not_start_with_this"].OnEvent += delegate { };
                Assert.Fail();
            }
        }

        [Test]
        public void TestUnsubscribeFromEvent()
        {
            using (IpcEventChannel ch1 = new IpcEventChannel(_registrar, _channel))
            {
                int count = 0;
                EventHandler<IpcSignalEventArgs> handler = delegate(object o, IpcSignalEventArgs e) { count++; };
                ch1["name"].OnEvent += handler;

                ch1.RaiseLocal("name");
                Assert.AreEqual(1, count);

                ch1["name"].OnEvent -= handler;

                ch1.RaiseLocal("name");
                Assert.AreEqual(1, count);
            }
        }

        [Test]
        public void TestRaiseLocalUnknown()
        {
            using (IpcEventChannel ch1 = new IpcEventChannel(_registrar, _channel))
                ch1.RaiseLocal("UnknownName");
        }

        [Test]
        public void TestIpcEventArgs()
        {
            Exception error = null;
            using (IpcEventChannel ch1 = new IpcEventChannel(_registrar, _channel))
            using (IpcEventChannel sender = new IpcEventChannel(_registrar, _channel))
            {
                ch1.StartListening("ch1");
                bool recieved = false;
                ch1.OnError += 
                    delegate(object o, ErrorEventArgs e) { error = e.GetException(); };
                ch1["TestArgs"].OnEvent += 
                    delegate(object o, IpcSignalEventArgs e)
                    {
                        recieved = true;
                        Assert.AreEqual(ch1, o);
                        Assert.AreEqual(ch1, e.EventChannel);
                        Assert.AreEqual("TestArgs", e.EventName);
                        Assert.AreEqual("1,2,3", String.Join(",", e.Arguments));
                        e.Arguments[0] = "a";
                        Assert.AreEqual("1,2,3", String.Join(",", e.Arguments));
                    };

                recieved = false;
                ch1.RaiseLocal("TestArgs", "1", "2", "3");
                Assert.IsTrue(recieved);
                if (error != null)
                    throw new AssertionException("Event Raised Error", error);

                recieved = false;
                sender.Broadcast(100, "TestArgs", "1", "2", "3");
                ch1.StopListening(1000);
                Assert.IsTrue(recieved);
                if (error != null)
                    throw new AssertionException("Event Raised Error", error);
            }
        }

        [Test]
        public void TestEventHandlerThrows()
        {
            Exception error = null;
            using (IpcEventChannel ch1 = new IpcEventChannel(_registrar, _channel))
            using (IpcEventChannel sender = new IpcEventChannel(_registrar, _channel))
            {
                ch1.StartListening("ch1");
                ch1.OnError += delegate(object o, ErrorEventArgs e) { error = e.GetException(); };
                ch1["Throw"].OnEvent += delegate(object o, IpcSignalEventArgs e) 
                { throw new Exception("Throw"); };
                
                ch1.RaiseLocal("Throw");
                Assert.IsNotNull(error);
                Assert.AreEqual(typeof(Exception), error.GetType());
                Assert.AreEqual("Throw", error.Message);

                error = null;
                sender.Broadcast(100, "Throw");
                ch1.StopListening(1000);
                Assert.IsNotNull(error);
                Assert.AreEqual(typeof(Exception), error.GetType());
                Assert.AreEqual("Throw", error.Message);
            }
        }

        [Test]
        public void TestListenerDies()
        {
            using (IpcEventChannel ch1 = new IpcEventChannel(_registrar, _channel))
            using (IpcEventChannel sender = new IpcEventChannel(_registrar, _channel))
            {
                ch1.StartListening("ch1");

                ManualResetEvent ready = new ManualResetEvent(false);
                ManualResetEvent die = new ManualResetEvent(false);
                ch1["Message"].OnEvent += delegate(object o, IpcSignalEventArgs e)
                {
                    ready.Set();
                    die.WaitOne();
                    Thread.CurrentThread.Abort();
                };

                sender.Broadcast(100, "Message");
                Assert.IsTrue(ready.WaitOne(1000, false));

                sender.EnableAsyncSend();
                sender.Broadcast(0, "Message");
                die.Set();
                sender.StopAsyncSending(true, 1000);
                ch1.StopListening();
            }
        }

        [Test]
        public void TestChannelSendOverloads()
        {
            using (IpcEventChannel sender = new IpcEventChannel(_registrar, _channel))
            using (IpcEventChannel ch1 = new IpcEventChannel(_registrar, _channel))
            using (IpcEventChannel ch2 = new IpcEventChannel(_registrar, _channel))
            {
                ch1.StartListening("ch1");
                ch2.StartListening("ch2");

                int ch1Count = 0, ch2Count = 0;
                ch1["Message"].OnEvent += delegate(object o, IpcSignalEventArgs e) { ch1Count++; };
                ch2["Message"].OnEvent += delegate(object o, IpcSignalEventArgs e) { ch2Count++; };

                sender.ExecutionTimeout = 1000;
                Assert.AreEqual(1, sender.SendTo("CH1", "Message"));
                Assert.AreEqual(1, sender.SendTo(new string[] { "CH2" }, "Message"));
                Assert.AreEqual(2, sender.SendTo(1000, new string[] { "ch1", "ch2" }, "Message"));

                ch1.StopListening();
                Assert.AreEqual(2, ch1Count);
                ch2.StopListening();
                Assert.AreEqual(2, ch2Count);
            }
        }

    }
}
