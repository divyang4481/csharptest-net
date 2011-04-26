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
using System.Collections.Generic;
using System.Threading;

namespace CSharpTest.Net.IpcChannel
{
    /// <summary>
    /// Implementation of the IpcEventChannel listener
    /// </summary>
    class IpcEventListener : IDisposable
    {
        readonly IpcEventChannel _channel;
        readonly string _identity;
        readonly string _instanceName;
        readonly Mutex _listener;
        readonly EventWaitHandle _ready;
        readonly AutoResetEvent _modified;
        readonly ManualResetEvent _quit;
        readonly Thread _worker;
        readonly Dictionary<string, WaitHandle> _handles;

        public IpcEventListener(IpcEventChannel channel, string instanceId, string instanceName)
        {
            _channel = Check.NotNull(channel);
            _identity = instanceId;
            _instanceName = instanceName ?? String.Empty;
            _handles = new Dictionary<string, WaitHandle>(StringComparer.Ordinal);

            _listener = new Mutex(false, String.Format("{0}.{1}._-LOCK-_", _channel.ChannelName, _identity));
            _ready = new EventWaitHandle(false, EventResetMode.ManualReset, String.Format("{0}.{1}._-READY-_", _channel.ChannelName, _identity));
            _modified = new AutoResetEvent(false);
            _quit = new ManualResetEvent(false);

            _worker = new Thread(Listen);
            _worker.SetApartmentState(ApartmentState.MTA);
            _worker.Name = _channel.ChannelName;
            _worker.IsBackground = true;

            _channel.OnModified += EventsModified;
            StartListening(_channel.DefaultTimeout);
        }

        void EventsModified(object sender, EventArgs e)
        { _modified.Set(); }

        public void Dispose()
        {
            _channel.OnModified -= EventsModified;
            StopListening(_channel.DefaultTimeout);
            _listener.Close();
            _ready.Close();
            _modified.Close();
            _quit.Close();
            foreach (WaitHandle h in _handles.Values)
                h.Close();
            _handles.Clear();
        }

        void StartListening(int mutexWaitLimit)
        {
            try { if (!_listener.WaitOne(mutexWaitLimit, false)) throw new TimeoutException(); }
            catch (AbandonedMutexException) { }
            try
            {
                _quit.Reset();
                _worker.Start();
            }
            finally 
            { _listener.ReleaseMutex(); }

            if (!_ready.WaitOne(_channel.DefaultTimeout, false))
                throw new TimeoutException();
        }

        public void StopListening(int timeout)
        {
            _quit.Set();
            if (!_worker.Join(timeout))
                _worker.Abort();
            _worker.Join();
        }

        void Listen()
        {
            if (!_listener.WaitOne(_channel.DefaultTimeout, false))
                return;
            try
            {
                _channel.Registrar.RegisterInstance(_channel.ChannelName, _identity, _instanceName);

                IpcEvent[] events = null;
                WaitHandle[] waiting = null;
                while (true)
                {
                    if (events == null)
                    {
                        int ix = 0;
                        Dictionary<string, WaitHandle> temp = new Dictionary<string, WaitHandle>(_handles);
                        events = new List<IpcEvent>(_channel.GetEvents()).ToArray();
                        waiting = new WaitHandle[events.Length + 2];

                        foreach (IpcEvent e in events)
                        {
                            WaitHandle h;
                            if (!_handles.TryGetValue(e.LocalName, out h))
                                _handles.Add(e.LocalName, h = new EventWaitHandle(false, EventResetMode.ManualReset, String.Format("{0}.{1}.{2}", _channel.ChannelName, _identity, e.LocalName)));
                            else
                                temp.Remove(e.LocalName);
                            waiting[ix++] = h;
                        }
                        foreach (KeyValuePair<string, WaitHandle> e in temp)
                        {
                            _handles.Remove(e.Key);
                            e.Value.Close();
                        }
                        waiting[ix++] = _modified;
                        waiting[ix] = _quit;
                    }

                    _ready.Set();
                    int handle = WaitHandle.WaitAny(waiting);
                    if (handle < events.Length)
                    {
                        ((EventWaitHandle)waiting[handle]).Reset();
                        string[] args = _channel.Registrar.ReadParameters(_channel.ChannelName, _identity, events[handle].LocalName);
                        _channel.RaiseLocal(events[handle].LocalName, args);
                    }
                    else if (handle == waiting.Length - 1)
                        break;
                    else
                        events = null;
                }
            }
            finally
            {
                _channel.Registrar.UnregisterInstance(_channel.ChannelName, _identity);
                _listener.ReleaseMutex();
            }
        }
    }

}
