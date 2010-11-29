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
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CSharpTest.Net.RpcLibrary.Interop;
using CSharpTest.Net.RpcLibrary.Interop.Structs;

namespace CSharpTest.Net.RpcLibrary
{
    [System.Diagnostics.DebuggerDisplay("{_handle} @{_binding}")]
    public class RpcClientApi : IDisposable
    {
        public readonly Guid IID;
        private readonly RpcProtseq _protocol;
        private readonly string _binding;
        private readonly RpcHandle _handle;
        private bool _authenticated;

        public RpcClientApi(Guid iid, RpcProtseq protocol, string server, string endpoint)
        {
            _handle = new RpcClientHandle();
            IID = iid;
            _protocol = protocol;
            Log.Verbose("RpcClient('{0}:{1}')", server, endpoint);

            _binding = StringBindingCompose(protocol, server, endpoint, null);
            Connect();
        }

        public static NetworkCredential Anonymous
        {
            get { return new NetworkCredential("ANONYMOUS LOGON", "", "NT_AUTHORITY"); }
        }

        public static NetworkCredential Self
        {
            get { return null; }
        }

        public RpcProtseq Protocol
        {
            get { return _protocol; }
        }

        private void Connect()
        {
            BindingFromStringBinding(_handle, _binding);
            Log.Verbose("RpcClient.Connect({0} = {1})", _handle.Handle, _binding);
        }

        public void AuthenticateAs(NetworkCredential credentials)
        {
            AuthenticateAs(null, credentials);
        }

        public void AuthenticateAs(string serverPrincipalName, NetworkCredential credentials)
        {
            RpcAuthentication[] types = new RpcAuthentication[] { RpcAuthentication.RPC_C_AUTHN_GSS_NEGOTIATE, RpcAuthentication.RPC_C_AUTHN_WINNT };
            RpcProtectionLevel protect = RpcProtectionLevel.RPC_C_PROTECT_LEVEL_PKT_PRIVACY;

            bool isAnon = (credentials != null && credentials.UserName == Anonymous.UserName && credentials.Domain == Anonymous.Domain);
            if (isAnon)
            {
                protect = RpcProtectionLevel.RPC_C_PROTECT_LEVEL_DEFAULT;
                types = new RpcAuthentication[] { RpcAuthentication.RPC_C_AUTHN_NONE };
            }

            AuthenticateAs(serverPrincipalName, credentials, protect, types);
        }
        
        public void AuthenticateAs(string serverPrincipalName, NetworkCredential credentials, RpcProtectionLevel level, params RpcAuthentication[] authTypes)
        {
            if (!_authenticated)
            {
                BindingSetAuthInfo(level, authTypes, _handle, serverPrincipalName, credentials);
                _authenticated = true;
            }
        }

        public byte[] Execute(byte[] input)
        {
            if (!_authenticated)
            {
                Log.Warning("AuthenticateAs was not called, assuming Anonymous.");
                AuthenticateAs(Anonymous);
            }
            Log.Verbose("RpcExecute(byte[{0}])", input.Length);
            return InvokeRpc(_handle, IID, input);
        }

        public void Dispose()
        {
            Log.Verbose("RpcClient('{0}').Dispose()", _binding);
            _handle.Dispose();
        }

        /* ********************************************************************
     * WinAPI INTEROP
     * *******************************************************************/

        private class RpcClientHandle : RpcHandle
        {
            protected override void DisposeHandle(ref IntPtr handle)
            {
                if (handle != IntPtr.Zero)
                {
                    RpcException.Assert(RpcBindingFree(ref Handle));
                    handle = IntPtr.Zero;
                }
            }
        }

        [DllImport("Rpcrt4.dll", EntryPoint = "RpcStringFreeW", CallingConvention = CallingConvention.StdCall,
            CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern RpcError RpcStringFree(ref IntPtr lpString);

        [DllImport("Rpcrt4.dll", EntryPoint = "RpcBindingFree", CallingConvention = CallingConvention.StdCall,
            CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern RpcError RpcBindingFree(ref IntPtr lpString);

        #region RpcStringBindingCompose

        [DllImport("Rpcrt4.dll", EntryPoint = "RpcStringBindingComposeW", CallingConvention = CallingConvention.StdCall,
            CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern RpcError RpcStringBindingCompose(
            String ObjUuid, String ProtSeq, String NetworkAddr, String Endpoint, String Options,
            out IntPtr lpBindingString
            );

        private static String StringBindingCompose(RpcProtseq ProtSeq, String NetworkAddr, String Endpoint,
                                                   String Options)
        {
            IntPtr lpBindingString;
            RpcError result = RpcStringBindingCompose(null, ProtSeq.ToString(), NetworkAddr, Endpoint, Options,
                                                      out lpBindingString);
            RpcException.Assert(result);

            try
            {
                return Marshal.PtrToStringUni(lpBindingString);
            }
            finally
            {
                RpcException.Assert(RpcStringFree(ref lpBindingString));
            }
        }

        #endregion

        #region RpcBindingFromStringBinding

        [DllImport("Rpcrt4.dll", EntryPoint = "RpcBindingFromStringBindingW",
            CallingConvention = CallingConvention.StdCall,
            CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern RpcError RpcBindingFromStringBinding(String bindingString, out IntPtr lpBinding);

        private static void BindingFromStringBinding(RpcHandle handle, String bindingString)
        {
            RpcError result = RpcBindingFromStringBinding(bindingString, out handle.Handle);
            RpcException.Assert(result);
        }

        #endregion

        #region RpcBindingSetAuthInfo

        [DllImport("Rpcrt4.dll", EntryPoint = "RpcBindingSetAuthInfoW", CallingConvention = CallingConvention.StdCall,
            CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern RpcError RpcBindingSetAuthInfo(IntPtr Binding, String ServerPrincName,
                                                             RpcProtectionLevel AuthnLevel, RpcAuthentication AuthnSvc,
                                                             [In] ref SEC_WINNT_AUTH_IDENTITY AuthIdentity,
                                                             uint AuthzSvc);

        [DllImport("Rpcrt4.dll", EntryPoint = "RpcBindingSetAuthInfoW", CallingConvention = CallingConvention.StdCall,
            CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern RpcError RpcBindingSetAuthInfo2(IntPtr Binding, String ServerPrincName,
                                                              RpcProtectionLevel AuthnLevel, RpcAuthentication AuthnSvc,
                                                              IntPtr p, uint AuthzSvc);

        private static void BindingSetAuthInfo(RpcProtectionLevel level, RpcAuthentication[] authTypes, 
            RpcHandle handle, string serverPrincipalName, NetworkCredential credentails)
        {
            if (credentails == null)
            {
                foreach (RpcAuthentication atype in authTypes)
                {
                    RpcError result = RpcBindingSetAuthInfo2(handle.Handle, serverPrincipalName, level, atype, IntPtr.Zero, 0);
                    if (result != RpcError.RPC_S_OK)
                        Log.Warning("Unable to register {0}, result = {1}", atype, new RpcException(result).Message);
                }
            }
            else
            {
                SEC_WINNT_AUTH_IDENTITY pSecInfo = new SEC_WINNT_AUTH_IDENTITY(credentails);
                foreach (RpcAuthentication atype in authTypes)
                {
                    RpcError result = RpcBindingSetAuthInfo(handle.Handle, serverPrincipalName, level, atype, ref pSecInfo, 0);
                    if (result != RpcError.RPC_S_OK)
                        Log.Warning("Unable to register {0}, result = {1}", atype, new RpcException(result).Message);
                }
            }
        }

        #endregion

        #region NdrClientCall2/InvokeRpc

        [DllImport("Rpcrt4.dll", EntryPoint = "NdrClientCall2", CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr NdrClientCall2x86(IntPtr pMIDL_STUB_DESC, IntPtr formatString, IntPtr args);

        [DllImport("Rpcrt4.dll", EntryPoint = "NdrClientCall2", CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr NdrClientCall2x64(IntPtr pMIDL_STUB_DESC, IntPtr formatString, IntPtr Handle,
                                                       int DataSize, IntPtr Data, [Out] out int ResponseSize,
                                                       [Out] out IntPtr Response);

        [MethodImpl(MethodImplOptions.NoInlining | (MethodImplOptions)64 /* MethodImplOptions.NoOptimization undefined in 2.0 */)]
        private static byte[] InvokeRpc(RpcHandle handle, Guid iid, byte[] input)
        {
            Log.Verbose("InvokeRpc on {0}, sending {1} bytes", handle.Handle, input.Length);
            Ptr<MIDL_STUB_DESC> pStub;
            if (!handle.GetPtr(out pStub))
            {
                pStub =
                    handle.CreatePtr(new MIDL_STUB_DESC(handle, handle.Pin(new RPC_CLIENT_INTERFACE(iid)),
                                                        RpcApi.TYPE_FORMAT,
                                                        true));
            }
            int szResponse = 0;
            IntPtr response, result;

            using (Ptr<byte[]> pInputBuffer = new Ptr<byte[]>(input))
            {
                if (RpcApi.Is64BitProcess)
                {
                    try
                    {
                        result = NdrClientCall2x64(pStub.Handle, RpcApi.FUNC_FORMAT_PTR.Handle, handle.Handle,
                                                   input.Length,
                                                   pInputBuffer.Handle, out szResponse, out response);
                    }
                    catch (SEHException ex)
                    {
                        Log.Error(ex);
                        RpcException.Assert(ex.ErrorCode);
                        throw;
                    }
                }
                else
                {
                    using (Ptr<Int32[]> pStack32 = new Ptr<Int32[]>(new Int32[10]))
                    {
                        pStack32.Data[0] = handle.Handle.ToInt32();
                        pStack32.Data[1] = input.Length;
                        pStack32.Data[2] = pInputBuffer.Handle.ToInt32();
                        pStack32.Data[3] = pStack32.Handle.ToInt32() + (sizeof (int)*6);
                        pStack32.Data[4] = pStack32.Handle.ToInt32() + (sizeof (int)*8);
                        pStack32.Data[5] = 0; //reserved
                        pStack32.Data[6] = 0; //output: int dwSizeResponse
                        pStack32.Data[8] = 0; //output: byte* lpResponse

                        try
                        {
                            result = NdrClientCall2x86(pStub.Handle, RpcApi.FUNC_FORMAT_PTR.Handle, pStack32.Handle);
                        }
                        catch (SEHException ex)
                        {
                            Log.Error(ex);
                            RpcException.Assert(ex.ErrorCode);
                            throw;
                        }

                        szResponse = pStack32.Data[6];
                        response = new IntPtr(pStack32.Data[8]);
                    }
                }
                GC.KeepAlive(pInputBuffer);
            }
            RpcException.Assert(result.ToInt32());
            Log.Verbose("InvokeRpc.InvokeRpc response on {0}, recieved {1} bytes", handle.Handle, szResponse);
            byte[] output = new byte[szResponse];
            if (szResponse > 0 && response != IntPtr.Zero)
            {
                Marshal.Copy(response, output, 0, output.Length);
            }
            RpcApi.Free(response);

            return output;
        }

        #endregion
    }
}