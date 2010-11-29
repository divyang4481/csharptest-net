#region Copyright 2010 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Runtime.InteropServices;
using System.Threading;
using CSharpTest.Net.RpcLibrary.Interop;
using CSharpTest.Net.RpcLibrary.Interop.Structs;

namespace CSharpTest.Net.RpcLibrary
{
    public class RpcServerApi : IDisposable
    {
        public const int MAX_CALL_LIMIT = 255;
        private static readonly UsageCounter _listenerCount = new UsageCounter("RpcApi.Listener.{0}", System.Diagnostics.Process.GetCurrentProcess().Id);
        private bool _isListening;
        private uint _maxCalls;

        public readonly Guid IID;
        private readonly RpcHandle _handle;
        private RpcExecuteHandler _handler;

        public static bool VerboseLogging
        {
            get { return Log.VerboseEnabled; }
            set { Log.VerboseEnabled = value; }
        }

        public RpcServerApi(Guid iid)
        {
            IID = iid;
            _maxCalls = MAX_CALL_LIMIT;
            _handle = new RpcServerHandle();
            ServerRegisterInterface(_handle, IID, RpcEntryPoint);
        }

        public void AddProtocol(RpcProtseq protocol, string endpoint, uint maxCalls)
        {
            ServerUseProtseqEp(protocol, maxCalls, endpoint);
            _maxCalls = Math.Max(_maxCalls, maxCalls);
        }

        public bool AddAuthentication(RpcAuthentication type)
        {
            return AddAuthentication(type, null);
        }

        public bool AddAuthentication(RpcAuthentication type, string serverPrincipalName)
        {
            return ServerRegisterAuthInfo(type, serverPrincipalName);
        }

        public void StartListening()
        {
            if (_isListening)
                return;

            _listenerCount.Increment(ServerListen, _maxCalls);
            _isListening = true;
        }

        public void StopListening()
        {
            if (!_isListening)
                return;

            _isListening = false;
            _listenerCount.Decrement(ServerStopListening);
        }

        private uint RpcEntryPoint(IntPtr clientHandle, uint szInput, IntPtr input, out uint szOutput, out IntPtr output)
        {
            output = IntPtr.Zero;
            szOutput = 0;

            try
            {
                byte[] bytesIn = new byte[szInput];
                Marshal.Copy(input, bytesIn, 0, bytesIn.Length);

                byte[] bytesOut;
                using (RpcClientInfo client = new RpcClientInfo(clientHandle))
                {
                    bytesOut = Execute(client, bytesIn);
                }
                if (bytesOut == null)
                {
                    return (uint) RpcError.RPC_S_NOT_LISTENING;
                }

                szOutput = (uint) bytesOut.Length;
                output = RpcApi.Alloc(szOutput);
                Marshal.Copy(bytesOut, 0, output, bytesOut.Length);

                return (uint) RpcError.RPC_S_OK;
            }
            catch (Exception ex)
            {
                RpcApi.Free(output);
                output = IntPtr.Zero;
                szOutput = 0;

                Log.Error(ex);
                return (uint) RpcError.RPC_E_FAIL;
            }
        }

        public virtual byte[] Execute(IRpcClientInfo client, byte[] input)
        {
            RpcExecuteHandler proc = _handler;
            if (proc != null)
            {
                return proc(client, input);
            }
            return null;
        }

        public void Dispose()
        {
            _handler = null;
            StopListening();
            _handle.Dispose();
        }

        public event RpcExecuteHandler OnExecute
        {
            add
            {
                lock (this)
                {
                    Check.Assert<InvalidOperationException>(_handler == null, "The interface id is already registered.");
                    _handler = value;
                }
            }
            remove
            {
                lock (this)
                {
                    Check.NotNull(value);
                    if (_handler != null)
                        Check.Assert<InvalidOperationException>(
                            Object.ReferenceEquals(_handler.Target, value.Target)
                            && Object.ReferenceEquals(_handler.Method, value.Method)
                            );
                    _handler = null;
                }
            }
        }

        public delegate byte[] RpcExecuteHandler(IRpcClientInfo client, byte[] input);

        /* ********************************************************************
     * WinAPI INTEROP
     * *******************************************************************/

        private class RpcServerHandle : RpcHandle
        {
            protected override void DisposeHandle(ref IntPtr handle)
            {
                if (handle != IntPtr.Zero)
                {
                    RpcServerUnregisterIf(handle, IntPtr.Zero, 1);
                    handle = IntPtr.Zero;
                }
            }
        }

        [DllImport("Rpcrt4.dll", EntryPoint = "RpcServerUnregisterIf", CallingConvention = CallingConvention.StdCall,
            CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern RpcError RpcServerUnregisterIf(IntPtr IfSpec, IntPtr MgrTypeUuid,
                                                             uint WaitForCallsToComplete);

        #region RpcServerXXXX routines

        [DllImport("Rpcrt4.dll", EntryPoint = "RpcServerUseProtseqEpW", CallingConvention = CallingConvention.StdCall,
            CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern RpcError RpcServerUseProtseqEp(String Protseq, uint MaxCalls, String Endpoint,
                                                             IntPtr SecurityDescriptor);

        private static void ServerUseProtseqEp(RpcProtseq protocol, uint maxCalls, String endpoint)
        {
            Log.Verbose("ServerUseProtseqEp({0})", protocol);
            RpcError err = RpcServerUseProtseqEp(protocol.ToString(), maxCalls, endpoint, IntPtr.Zero);
            if (err != RpcError.RPC_S_DUPLICATE_ENDPOINT)
                RpcException.Assert(err);
        }

        [DllImport("Rpcrt4.dll", EntryPoint = "RpcServerRegisterIf", CallingConvention = CallingConvention.StdCall,
            CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern RpcError RpcServerRegisterIf(IntPtr IfSpec, IntPtr MgrTypeUuid, IntPtr MgrEpv);

        private static void ServerRegisterInterface(RpcHandle handle, Guid iid, RpcExecute fnExec)
        {
            Log.Verbose("ServerRegisterInterface({0})", iid);
            Ptr<RPC_SERVER_INTERFACE> sIf = MIDL_SERVER_INFO.Create(handle, iid, RpcApi.TYPE_FORMAT, RpcApi.FUNC_FORMAT,
                                                                    fnExec);
            RpcException.Assert(RpcServerRegisterIf(sIf.Handle, IntPtr.Zero, IntPtr.Zero));
            handle.Handle = sIf.Handle;
        }

        [DllImport("Rpcrt4.dll", EntryPoint = "RpcServerRegisterAuthInfoW",
            CallingConvention = CallingConvention.StdCall,
            CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern RpcError RpcServerRegisterAuthInfo(String ServerPrincName, uint AuthnSvc, IntPtr GetKeyFn,
                                                                 IntPtr Arg);

        private static bool ServerRegisterAuthInfo(RpcAuthentication auth, string serverPrincName)
        {
            Log.Verbose("ServerRegisterAuthInfo({0})", auth);
            RpcError response = RpcServerRegisterAuthInfo(serverPrincName, (uint) auth, IntPtr.Zero, IntPtr.Zero);
            if (response != RpcError.RPC_S_OK)
            {
                Log.Warning("ServerRegisterAuthInfo - unable to register authentication type {0}", auth);
                return false;
            }
            return true;
        }

        #endregion

        #region RpcServerListen & StopListening

        [DllImport("Rpcrt4.dll", EntryPoint = "RpcServerListen", CallingConvention = CallingConvention.StdCall,
            CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern RpcError RpcServerListen(uint MinimumCallThreads, uint MaxCalls, uint DontWait);

        private static void ServerListen(uint maxCalls)
        {
            Log.Verbose("Begin Server Listening");
            RpcError result = RpcServerListen(1, maxCalls, 1);
            if (result == RpcError.RPC_S_ALREADY_LISTENING)
            {
                result = RpcError.RPC_S_OK;
            }
            RpcException.Assert(result);
            Log.Verbose("Server Ready");
        }

        [DllImport("Rpcrt4.dll", EntryPoint = "RpcMgmtStopServerListening",
            CallingConvention = CallingConvention.StdCall,
            CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern RpcError RpcMgmtStopServerListening(IntPtr ignore);

        [DllImport("Rpcrt4.dll", EntryPoint = "RpcMgmtWaitServerListen", CallingConvention = CallingConvention.StdCall,
            CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern RpcError RpcMgmtWaitServerListen();

        private static void ServerStopListening()
        {
            Log.Verbose("Stop Server Listening");
            RpcError result = RpcMgmtStopServerListening(IntPtr.Zero);
            if (result != RpcError.RPC_S_OK)
            {
                Log.Warning("RpcMgmtStopServerListening result = {0}", result);
            }
            result = RpcMgmtWaitServerListen();
            if (result != RpcError.RPC_S_OK)
            {
                Log.Warning("RpcMgmtWaitServerListen result = {0}", result);
            }
        }

        #endregion
    }
}