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
using CSharpTest.Net.Threading;

namespace CSharpTest.Net.IpcChannel
{
    /// <summary>
    /// Provides the implmentation to send an event message to a group of instances
    /// </summary>
    partial class IpcEventChannel
    {
        WaitAndContinueWorker _deferred;

        /// <summary>
        /// Enables a background worker thread to continue sending messages that are incomplete 
        /// after the expiration of the timeout specified in the Broadcast/SendTo method.  This
        /// is required to avoid dead-locks if your broadcasting messages within an IpcEvent.OnEvent
        /// event handler.
        /// </summary>
        public void EnableAsyncSend()
        {
            lock (_sync)
                _deferred = _deferred ?? new WaitAndContinueWorker();
        }

        /// <summary>
        /// Shutsdown the worker thread created by a call to EnableAsyncSend() allowing up to
        /// the number of milliseconds in timeout to shutdown the worker and complete any 
        /// pending work.  If timeout is 0, the worker will be aborted.
        /// </summary>
        public void StopAsyncSending(bool completeWork, int timeout)
        {
            WaitAndContinueWorker kill;
            lock (_sync)
            {
                kill = _deferred;
                _deferred = null;
            }
            if (kill != null)
            {
                using (kill)
                    kill.Complete(completeWork, timeout);
            }
        }

        private int InternalSend(int waitTime, IEnumerable<string> identities, string eventName, params string[] arguments)
        {
            int count = 0;
            using (WaitAndContinueList work = new WaitAndContinueList())
            {
                foreach (string identity in Check.NotNull(identities))
                {
                    IpcEventMessage m = new IpcEventMessage(this, ExecutionTimeout, identity, eventName, arguments);
                    if (!m.Completed)
                        work.AddWork(m);
                    else
                        count += m.Successful ? 1 : 0;
                }

                if (!work.IsEmpty)
                {
                    //Waiting while in-call results in dead-locks, so we force these to defer if they do not complete immediatly
                    if (InCall) waitTime = 0;

                    System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
                    if (waitTime > 0)
                        timer.Start();

                    IWaitAndContinue waitItem;
                    int ticksRemaining = waitTime;
                    while (work.PerformWork(ticksRemaining, out waitItem))
                    {
                        count += ((IpcEventMessage) waitItem).Successful ? 1 : 0;
                        if (waitTime > 0 && (ticksRemaining = (int) (waitTime - timer.ElapsedMilliseconds)) < 0)
                            break;
                    }

                    if (!work.IsEmpty)
                    {
                        WaitAndContinueWorker worker = _deferred;
                        if (worker != null)
                            try { worker.AddWork(work); } catch (ObjectDisposedException) { }
                    }
                }
            }
            return count;
        }

        /// <summary> Sends the event to all channel subscribers </summary>
        public int Broadcast(string eventName, params string[] arguments)
        { return Broadcast(ExecutionTimeout, eventName, arguments); }
        /// <summary> Sends the event to all channel subscribers, waiting at most waitTime </summary>
        public int Broadcast(int waitTime, string eventName, params string[] arguments)
        { return InternalSend(waitTime, Registrar.GetRegisteredInstances(ChannelName), eventName, arguments); }

        /// <summary> Sends the event to all channel subscribers with the given identity or name (case-insensitive) </summary>
        public int SendTo(string instance, string eventName, params string[] arguments)
        { return SendTo(ExecutionTimeout, instance, eventName, arguments); }
        /// <summary> Sends the event to all channel subscribers with the given identity or name (case-insensitive) </summary>
        public int SendTo(int waitTime, string instance, string eventName, params string[] arguments)
        { return InternalSend(waitTime, Registrar.GetRegisteredInstances(ChannelName, instance), eventName, arguments); }

        /// <summary> Sends the event to the specified list of instance identities or names (case-insensitive) </summary>
        public int SendTo(IEnumerable<string> instances, string eventName, params string[] arguments)
        { return SendTo(ExecutionTimeout, instances, eventName, arguments); }
        /// <summary> Sends the event to the specified list of instance identities or names (case-insensitive) </summary>
        public int SendTo(int waitTime, IEnumerable<string> instances, string eventName, params string[] arguments)
        { return InternalSend(waitTime, Registrar.GetRegisteredInstances(ChannelName, instances), eventName, arguments); }
    }
}
