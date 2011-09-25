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
using System.CodeDom.Compiler;
using System.Threading;
using CSharpTest.Net.Threading;

namespace CSharpTest.Net.IpcChannel
{
    class IpcEventMessage : IWaitAndContinue
    {
        readonly IpcEventChannel _channel;
        readonly string _identity, _eventName;
        readonly string[] _arguments;

        readonly long _expiresOn;
        Mutex _listener;
        Mutex _sender;
        EventWaitHandle _ready;
        IWorkState<IpcEventMessage> _state;

        public IpcEventMessage(IpcEventChannel channel, int execTimeout, string identity, string eventName, string[] arguments)
        {
            _channel = channel;
            _identity = identity;
            _eventName = eventName;
            _arguments = arguments;
            _expiresOn = DateTime.UtcNow.AddMilliseconds(execTimeout).Ticks;
            
            Transition(StartState);
        }

        public void Dispose()
        {
            if (_listener != null) _listener.Close();
            if (_sender != null) _sender.Close();
            if (_ready != null) _ready.Close();
            if (_state != SuccessState)
                _state = FailedState;
        }

        private void Transition(IWorkState<IpcEventMessage> state)
        { _state = state.Transition(this); }

        public bool CanCompleteNow { get { return _channel.CanCall(_identity); } }
        public bool Successful { get { return _state.GetSuccess(this); } }
        public bool Completed { get { return _state.GetCompleted(this); } }
        public int HandleCount { get { return _state.GetHandleCount(this); } }

        public void CopyHandles(WaitHandle[] array, int offset) { _state.CopyHandles(this, array, offset); }
        public void ContinueProcessing(WaitHandle handleSignaled) { _state.ContinueProcessing(this, handleSignaled); }

        interface IWorkState<T>
        {
            IWorkState<T> Transition(T instance);
            bool GetSuccess(T instance);
            bool GetCompleted(T instance);
            int GetHandleCount(T instance);
            void CopyHandles(T instance, WaitHandle[] array, int offset);
            void ContinueProcessing(T instance, WaitHandle handleSignaled);
        }

        #region StartState
        static readonly IWorkState<IpcEventMessage> StartState = new CStartState();
        class CStartState : CTransitionalState, IWorkState<IpcEventMessage>
        {
            [Obsolete]
            public bool GetSuccess(IpcEventMessage msg) { return false; }
            [Obsolete]
            public bool GetCompleted(IpcEventMessage msg) { return false; }
            public IWorkState<IpcEventMessage> Transition(IpcEventMessage msg)
            {
                bool canCall = msg.CanCompleteNow;

                bool noListener, listenerLock = false;
                msg._listener = new Mutex(false, String.Format("{0}.{1}._-LOCK-_", msg._channel.ChannelName, msg._identity), out noListener);

                if (canCall)
                {
                    try { if (!noListener) noListener = listenerLock = msg._listener.WaitOne(0, false); }
                    catch (AbandonedMutexException) { listenerLock = noListener = true; }
                    finally
                    { if (listenerLock) msg._listener.ReleaseMutex(); }
                }
                if (noListener)
                {
                    msg._channel.Registrar.UnregisterInstance(msg._channel.ChannelName, msg._identity);
                    return FailedState;
                }

                msg._ready = new EventWaitHandle(false, EventResetMode.ManualReset, String.Format("{0}.{1}._-READY-_", msg._channel.ChannelName, msg._identity), out noListener);
                if (noListener)
                    return FailedState;
                bool aquired;
                msg._sender = new Mutex(canCall, String.Format("{0}.{1}._-SEND-_", msg._channel.ChannelName, msg._identity), out aquired);

                if (canCall && aquired)
                {
                    WaitingSenderState.ContinueProcessing(msg, msg._sender);
                    return msg._state;
                }

                return WaitingSenderState;
            }
        }
        #endregion

        #region WaitingSenderState
        static readonly IWorkState<IpcEventMessage> WaitingSenderState = new CWaitingSenderState();
        class CWaitingSenderState : IWorkState<IpcEventMessage>
        {
            public bool GetSuccess(IpcEventMessage msg) { return false; }
            public bool GetCompleted(IpcEventMessage msg) { return msg._expiresOn < DateTime.UtcNow.Ticks; }
            public int GetHandleCount(IpcEventMessage msg) { return msg.CanCompleteNow ? 2 : 0; }
            public void CopyHandles(IpcEventMessage msg, WaitHandle[] array, int offset)
            {
                array[offset + 0] = msg._listener;
                array[offset + 1] = msg._sender;
            }

            public void ContinueProcessing(IpcEventMessage msg, WaitHandle handleSignaled)
            {
                if (ReferenceEquals(msg._sender, handleSignaled))
                {
                    IWorkState<IpcEventMessage> newState = FailedState;
                    try
                    {
                        if (!msg._ready.WaitOne(0, false))
                        {
                            newState = WaitingReadyState;
                            return;
                        }

                        if (!msg._channel.Registrar.WriteParameters(msg._channel.ChannelName, msg._identity, msg._eventName, msg._arguments))
                            return;

                        bool noListener;
                        string eventId = String.Format("{0}.{1}.{2}", msg._channel.ChannelName, msg._identity, msg._eventName);
                        using (EventWaitHandle signal = new EventWaitHandle(false, EventResetMode.ManualReset, eventId, out noListener))
                        {
                            if (noListener)
                                return;
                            else
                            {
                                msg._ready.Reset();
                                signal.Set();
                                newState = SuccessState;
                            }
                        }
                    }
                    finally
                    {
                        msg._sender.ReleaseMutex();
                        msg.Transition(newState);
                    }
                }
                else
                {
                    if (ReferenceEquals(msg._listener, handleSignaled))
                        msg._listener.ReleaseMutex();
                    msg.Transition(FailedState);
                }
            }

            public IWorkState<IpcEventMessage> Transition(IpcEventMessage msg)
            { return this; }
        }
        #endregion

        #region WaitingReadyState
        static readonly IWorkState<IpcEventMessage> WaitingReadyState = new CWaitingReadyState();
        class CWaitingReadyState : IWorkState<IpcEventMessage>
        {
            [Obsolete]
            public bool GetSuccess(IpcEventMessage msg) { return false; }
            [Obsolete]
            public bool GetCompleted(IpcEventMessage msg) { return false; }
            public int GetHandleCount(IpcEventMessage msg) { return 2; }
            public void CopyHandles(IpcEventMessage msg, WaitHandle[] array, int offset)
            {
                array[offset + 0] = msg._listener;
                array[offset + 1] = msg._ready;
            }

            public void ContinueProcessing(IpcEventMessage msg, WaitHandle handleSignaled)
            {
                if (ReferenceEquals(msg._ready, handleSignaled))
                {
                    msg.Transition(WaitingSenderState);
                }
                else
                {
                    if (ReferenceEquals(msg._listener, handleSignaled))
                        msg._listener.ReleaseMutex();
                    msg.Transition(FailedState);
                }
            }

            public IWorkState<IpcEventMessage> Transition(IpcEventMessage msg)
            { return this; }
        }
        #endregion

        #region FailedState
        static readonly IWorkState<IpcEventMessage> FailedState = new CFailedState();
        class CFailedState : CTransitionalState, IWorkState<IpcEventMessage>
        {
            [Obsolete]
            public bool GetSuccess(IpcEventMessage msg) { return false; }
            [Obsolete]
            public bool GetCompleted(IpcEventMessage msg) { return true; }
            public IWorkState<IpcEventMessage> Transition(IpcEventMessage msg)
            {
                msg.Dispose();
                return this;
            }
        }
        #endregion

        #region SuccessState
        static readonly IWorkState<IpcEventMessage> SuccessState = new CSuccessState();
        class CSuccessState : CTransitionalState, IWorkState<IpcEventMessage>
        {
            public bool GetSuccess(IpcEventMessage msg) { return true; }
            public bool GetCompleted(IpcEventMessage msg) { return true; }
            public IWorkState<IpcEventMessage> Transition(IpcEventMessage msg)
            {
                msg.Dispose();
                return this;
            }
        }
        #endregion

        #region CTransitionalState
        abstract class CTransitionalState
        {
            public int GetHandleCount(IpcEventMessage msg) { return 0; }
            public void CopyHandles(IpcEventMessage msg, WaitHandle[] array, int offset) { }
            public void ContinueProcessing(IpcEventMessage msg, WaitHandle handleSignaled) { }
        }
        #endregion

        #region Constraints
        static IpcEventMessage()
        {
            //Ensure correct startstate behavior:
            if (StartState.GetSuccess(null) || StartState.GetCompleted(null) || StartState.GetHandleCount(null) > 0)
                throw new ApplicationException("Code constraints failed.");
            StartState.CopyHandles(null, null, 0);
            StartState.ContinueProcessing(null, null);
        }
        #endregion
    }

}
