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
using System.IO;
using System.Threading;
using Microsoft.Win32;

namespace CSharpTest.Net.IpcChannel
{
    /// <summary>
    /// Provides a means to send and recieve events (optionally with arguments) across thread/process boundaries 
    /// to a group of listeners of an event channel.  Subscribe to desired events, call Start/StopListening, or 
    /// just send events to other listeners on the same channel name.
    /// </summary>
    public partial class IpcEventChannel : IDisposable
    {
        [ThreadStatic]
        static string _inCall;
        readonly object _sync;
        readonly string _channelName;
        readonly string _instanceId;
        readonly IIpcChannelRegistrar _registrar;

        private int _executionTimeout = 60000;
        private int _defaultTimeout = 60000;

        Event[] _events;
        IpcEventListener _listener;

        /// <summary> Raised when an event subscriber does not handle an exception </summary>
        public event ErrorEventHandler OnError;
        /// <summary> Raised when the collection of event names changes </summary>
        public event EventHandler OnModified;

        /// <summary>
        /// Creates an IpcEventChannel that persists state in Registry.LocalMachine at hklmKeyPath + channelName
        /// </summary>
        /// <param name="hkcuKeyPath">The registry current-user path to all channels ex: @"Software\MyProduct\IpcChannels"</param>
        /// <param name="channelName">The name of the channel to subscribe or send events to</param>
        public IpcEventChannel(string hkcuKeyPath, string channelName) 
            : this(new IpcChannelRegistrar(Registry.CurrentUser, hkcuKeyPath), channelName)
        { }
        /// <summary> Creates an IpcEventChannel that persists state in IChannelRegistrar provided </summary>
        /// <param name="registrar">The serialization provider for registration</param>
        /// <param name="channelName">The name of the channel to subscribe or send events to</param>
        public IpcEventChannel(IIpcChannelRegistrar registrar, string channelName)
        {
            _sync = new object();
            _instanceId = Guid.NewGuid().ToString();
            _registrar = Check.NotNull(registrar);
            _channelName = Check.NotEmpty(channelName);
            _events = new Event[0];
            OnModified += Ignore;
        }

        private static void Ignore(Object o, EventArgs e) { }

        /// <summary> Disposes of all resources used by this channel </summary>
        public void Dispose()
        {
            if (_listener != null)
                StopListening(0);
            if (_deferred != null)
                _deferred.Dispose();
            _events = new Event[0];
        }

        /// <summary> Returns true if the current thread is processing an event </summary>
        public bool InCall { get { return _inCall != null; } }
        /// <summary> Returns true if an event can be dispatched to the target on the current thread </summary>
        internal bool CanCall(string targetInstance) { return _inCall != targetInstance; }
        /// <summary> Returns the channel name of this instance </summary>
        public string ChannelName { get { return _channelName; } }
        /// <summary> Returns the identity of this channel when listening </summary>
        public string InstanceId { get { return _instanceId; } }
        /// <summary> Returns the storage registrar of this channel </summary>
        public IIpcChannelRegistrar Registrar { get { return _registrar; } }

        /// <summary> Gets/Sets the number of milliseconds to wait for an event to complete processing </summary>
        /// <example> channel.ExecutionTimeout = 60000; </example>
        public int ExecutionTimeout { get { return _executionTimeout; } set { _executionTimeout = value; } }
        /// <summary> Gets/Sets the number of milliseconds to wait when starting/stopping threads or waiting for a known state </summary>
        /// <example> channel.DefaultTimeout = 60000; </example>
        public int DefaultTimeout { get { return _defaultTimeout; } set { _defaultTimeout = value; } }

        /// <summary> Returns an enumeration of all known events of this instance </summary>
        public IEnumerable<IpcEvent> GetEvents()
        { return (Event[])_events.Clone(); }

        /// <summary> Registers/Gets an IpcEvent instance for the specified event name </summary>
        public IpcEvent this[string name]
        {
            get 
            {
                Check.NotEmpty(name);
                if (name.StartsWith("_-")) throw new ArgumentException();

                Event eventInfo;
                Event[] ary = _events;
                int pos = Array.BinarySearch(ary, name, IpcEvent.Comparer);
                if (pos >= 0)
                    return ary[pos];
                lock(_sync)
                {
                    ary = _events;
                    pos = Array.BinarySearch(ary, name, IpcEvent.Comparer);
                    if (pos >= 0)
                        return ary[pos];
                    eventInfo = new Event(name);
                    List<Event> events = new List<Event>(ary);
                    events.Insert(~pos, eventInfo);
                    Interlocked.Exchange(ref _events, events.ToArray());
                }
                OnModified(this, EventArgs.Empty);
                return eventInfo;
            }
        }

        /// <summary> Synchronously dispatches the event to this instance's subscribers </summary>
        public void RaiseLocal(string eventName, params string[] args)
        {
            Check.NotEmpty(eventName);
            Event[] ary = _events;
            int pos = Array.BinarySearch(ary, eventName, IpcEvent.Comparer);
            if (pos < 0)
                return;
            string oldCall = _inCall;
            try
            {
                _inCall = _instanceId;
                ary[pos].RaiseEvent(this, args);
            }
            catch (Exception e)
            {
                ErrorEventHandler h = OnError;
                if (h != null) h(this, new ErrorEventArgs(e));
            }
            finally { _inCall = oldCall; }
        }

        /// <summary> Starts listening for events being posted to this channel on a new thread </summary>
        public void StartListening() { StartListening(null); }
        /// <summary> Same as StartListening but specifies a name that can be used to lookup this instance </summary>
        public void StartListening(string instanceName)
        {
            lock (_sync)
            {
                Check.Assert<InvalidOperationException>(_listener == null, "The channel is already listening.");
                _listener = new IpcEventListener(this, _instanceId, instanceName);
            }
        }
        /// <summary> Stops listening to incoming events on the channel </summary>
        public void StopListening() { StopListening(DefaultTimeout); }
        /// <summary> Stops listening to incoming events on the channel </summary>
        public void StopListening(int timeout)
        {
            IpcEventListener kill;
            lock (_sync)
            {
                kill = _listener;
                _listener = null;
            }
            if (kill != null)
            {
                kill.StopListening(timeout);
                kill.Dispose();
            }
        }

        class Event : IpcEvent
        {
            public Event(string localName) : base(localName) { }
            public new void RaiseEvent(IpcEventChannel channel, params string[] args) { base.RaiseEvent(channel, args); }
        }
    }
}
