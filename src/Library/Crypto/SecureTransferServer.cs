#region Copyright 2011-2014 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using CSharpTest.Net.Serialization;
using CSharpTest.Net.IO;

namespace CSharpTest.Net.Crypto
{
    public static partial class SecureTransfer
    {
        #region Received EventArgs
        /// <summary> Event args that provides details about the start of a transfer </summary>
        public class BeginTransferEventArgs : EventArgs
        {
            private readonly Guid _transferId;
            private readonly string _location;
            private long _totalSize;
            internal BeginTransferEventArgs(Guid transferId, string location, long totalSize)
            {
                _transferId = transferId;
                _location = location;
                _totalSize = totalSize;
            }
            /// <summary> The client-provided unique identifier for this transfer </summary>
            public Guid TransferId { get { return _transferId; } }
            /// <summary> The client-provided name of the transfer </summary>
            public string Location { get { return _location; } }
            /// <summary> The full length of the file being transferred </summary>
            public long TotalSize { get { return _totalSize; } protected set { _totalSize = value; } }
        }
        /// <summary> Event args that provides details about the contents of a transfer </summary>
        public class BytesReceivedEventArgs : BeginTransferEventArgs
        {
            private readonly long _writeOffset;
            private readonly byte[] _bytesReceived;
            internal BytesReceivedEventArgs(Guid transferId, string location, long totalSize, long writeOffset, byte[] bytesReceived)
                : base(transferId, location, totalSize)
            {
                _writeOffset = writeOffset;
                _bytesReceived = bytesReceived;
            }
            /// <summary> The offset at which BytesReceived should be written </summary>
            public long WriteOffset { get { return _writeOffset; } }
            /// <summary> The bytes that should be written at WriteOffset </summary>
            public byte[] BytesReceived { get { return _bytesReceived; } }
        }
        /// <summary> Event args that provides details about the completion of a transfer </summary>
        public class CompleteTransferEventArgs : BeginTransferEventArgs
        {
            private readonly Hash _contentHash;
            internal CompleteTransferEventArgs(Guid transferId, string location, long totalSize, Hash contentHash)
                : base(transferId, location, totalSize)
            {
                _contentHash = contentHash;
            }
            /// <summary> The SHA-256 hash of the entire content file transferred </summary>
            public Hash ContentHash { get { return _contentHash; } }
        }
        /// <summary> Event args that provides details about the contents of a transfer </summary>
        public class DownloadBytesEventArgs : BeginTransferEventArgs
        {
            private readonly long _readOffset;
            private readonly int _readLength;
            private byte[] _bytesRead;

            internal DownloadBytesEventArgs(Guid transferId, string location, long readOffset, int readLength)
                : base(transferId, location, -1)
            {
                _readOffset = readOffset;
                _readLength = readLength;
                _bytesRead = null;
            }

            /// <summary> The offset at which the bytes should be read </summary>
            public long ReadOffset { get { return _readOffset; } }
            /// <summary> The number of bytes to read </summary>
            public int ReadLength { get { return _readLength; } }
            /// <summary>
            /// Returns the number of bytes specified in ReadLenght from the offset of ReadOffset
            /// </summary>
            public void SetBytes(long totalSize, byte[] bytesRead)
            {
                TotalSize = totalSize;
                _bytesRead = bytesRead;
            }

            internal void WriteTo(Stream output)
            {
                if (ReadLength > 0)
                {
                    Check.Assert<InvalidDataException>(_bytesRead != null && _bytesRead.Length == ReadLength);
                    output.Write(_bytesRead, 0, ReadLength);
                }
            }
        }
        #endregion
        /// <summary>
        /// Provides a file transfer handler for the server-side (receiver) of file transfers. 
        /// </summary>
        public class Server
        {
            private readonly RSAPrivateKey _privateKey;
            private readonly RSAPublicKey _clientKey;
            private readonly INameValueStore _storage;

            /// <summary>
            /// Constructs/reconstructs the server-side receiver to process one or more messages.  This class 
            /// maintains all state in the INameValueStore so it may be destroyed between requests, or there 
            /// may be multiple instances handling requests, provided that all instances have access to the
            /// underlying storage provided by the INameValueStore instance.
            /// </summary>
            /// <param name="privateKey">The private key used for this server</param>
            /// <param name="clientKey">The public key of the client to allow</param>
            /// <param name="storage">The state storage used between requests</param>
            public Server(RSAPrivateKey privateKey, RSAPublicKey clientKey, INameValueStore storage)
            {
                _privateKey = privateKey;
                _clientKey = clientKey;
                _storage = storage;

                NonceSize = 32;
                KeyBytes = 32;
                MaxInboundFileChunk = ushort.MaxValue;
                MaxOutboundFileChunk = 1000*1024;
            }

            /// <summary>
            /// The amount of random data returned from the server to generate a session key
            /// </summary>
            protected int KeyBytes { get; set; }
            /// <summary> The number of random bytes to use for a nonce </summary>
            public int NonceSize { get; set; }
            /// <summary> 
            /// The maximum number of bytes from the file to send, the actual message size will be longer by 
            /// 100 or so bytes + SHA256 signature length (privateKey.ExportParameters().Modulus.Length).
            /// To be certain a client does not exceed a specific size, allow for an addition 2500 bytes.
            /// </summary>
            public int MaxInboundFileChunk { get; set; }
            /// <summary> 
            /// The maximum number of bytes from the file to send, the actual message size will be longer by 
            /// 100 or so bytes + SHA256 signature length (privateKey.ExportParameters().Modulus.Length).
            /// To be certain a client does not exceed a specific size, allow for an addition 2500 bytes.
            /// </summary>
            public int MaxOutboundFileChunk { get; set; }

            #region State Management
            /// <summary> returns true if the value exists </summary>
            protected virtual bool HasState(Guid transferId, string name)
            {
                string value;
                return _storage.Read(transferId.ToString("N"), name, out value);
            }
            /// <summary> returns the value identified </summary>
            protected virtual string ReadState(Guid transferId, string name)
            {
                string value;
                if (!_storage.Read(transferId.ToString("N"), name, out value))
                    throw new InvalidDataException();
                return value;
            }
            /// <summary> stores the value identified </summary>
            protected virtual void WriteState(Guid transferId, string name, string value)
            {
                _storage.Write(transferId.ToString("N"), name, value);
            }
            /// <summary> removes the value identified </summary>
            protected virtual void DeleteState(Guid transferId, string name)
            {
                _storage.Delete(transferId.ToString("N"), name);
            }
            /// <summary> removes all values for a give transfer </summary>
            protected virtual void Delete(Guid transferId)
            {
                _storage.Delete(transferId.ToString("N"), "start-time");
                _storage.Delete(transferId.ToString("N"), "nonce");
                _storage.Delete(transferId.ToString("N"), "session-key");
                _storage.Delete(transferId.ToString("N"), "total-length");
                _storage.Delete(transferId.ToString("N"), "location");
            }
            #endregion

            private Salt SessionKey(Guid transferId) { return Salt.FromString(ReadState(transferId, "session-key")); }

            #region Events
            /// <summary>Raised when an error occurs</summary>
            public event ErrorEventHandler ErrorRaised;
            /// <summary>Raised when a transfer begins</summary>
            public event EventHandler<BeginTransferEventArgs> BeginTransfer;
            /// <summary>Raised when bytes are received</summary>
            public event EventHandler<BytesReceivedEventArgs> BytesReceived;
            /// <summary>Raised when a transfer completes</summary>
            public event EventHandler<CompleteTransferEventArgs> CompleteTransfer;
            /// <summary>Raised durring a download request</summary>
            public event EventHandler<DownloadBytesEventArgs> DownloadBytes;

            private void OnErrorRaised(Exception error)
            {
                ErrorEventHandler handler = ErrorRaised;
                if (handler != null)
                    handler(this, new ErrorEventArgs(error));
            }

            private void OnBeginTransfer(Guid transferId, string destanation, long length)
            {
                if (BeginTransfer != null)
                    BeginTransfer(this, new BeginTransferEventArgs(transferId, destanation, length));
            }

            private void OnBytesReceived(Guid transferId, string destanation, long totalSize, long offset, byte[] bytes)
            {
                if (BytesReceived != null)
                    BytesReceived(this, new BytesReceivedEventArgs(transferId, destanation, totalSize, offset, bytes));
            }

            private void OnCompleteTransfer(Guid transferId, string location, long totalSize, Hash contentHash)
            {
                if (CompleteTransfer != null)
                    CompleteTransfer(this, new CompleteTransferEventArgs(transferId, location, totalSize, contentHash));
            }

            private void OnDownloadBytes(Guid transferId, string location, out long totalSize, long offset, int length, Stream output)
            {
                DownloadBytesEventArgs args = new DownloadBytesEventArgs(transferId, location, offset, length);
                if (DownloadBytes != null)
                    DownloadBytes(this, args);
                Check.Assert<InvalidOperationException>(args.TotalSize >= 0);
                totalSize = args.TotalSize;
                args.WriteTo(output);
            }
            #endregion
            /// <summary>
            /// Processes an inbound message and returns the result
            /// </summary>
            /// <exception cref="InvalidDataException">Raised for any internal error</exception>
            public Stream Receive(Stream data)
            {
                try
                {
                    using (Message req = new Message(data, _privateKey, SessionKey))
                    {
                        VerifyMesage(req);
                        switch (req.State)
                        {
                            case TransferState.NonceRequest:
                                return NonceRequest(req).ToStream(_privateKey);
                            case TransferState.UploadRequest:
                                return TransferRequest(req).ToStream(_privateKey);
                            case TransferState.SendBytesRequest:
                                return SendBytesRequest(req).ToStream(_privateKey);
                            case TransferState.UploadCompleteRequest:
                                return CompleteRequest(req).ToStream(_privateKey);
                            case TransferState.DownloadRequest:
                                return DownloadRequest(req).ToStream(_privateKey);
                            case TransferState.DownloadBytesRequest:
                                return DownloadBytesRequest(req).ToStream(_privateKey);
                            case TransferState.DownloadCompleteRequest:
                                return DownloadCompleteRequest(req).ToStream(_privateKey);
                            default:
                                throw new InvalidDataException();
                        }
                    }
                }
                catch (Exception error)
                {
                    OnErrorRaised(error);
                    Thread.Sleep(new Random().Next(10, 100));
                    throw new InvalidDataException();
                }
            }

            private void VerifyMesage(Message msg)
            {
                Check.Assert<InvalidDataException>(HasState(msg.TransferId, "start-time") == (msg.State != TransferState.NonceRequest));
                if (msg.State > TransferState.NonceRequest)
                {
                    long startUtcTicks = long.Parse(ReadState(msg.TransferId, "start-time"));
                    DateTime started = new DateTime(startUtcTicks, DateTimeKind.Utc);
                    if ((DateTime.UtcNow - started).TotalHours > 2 || 
                        (msg.State == TransferState.UploadRequest && (DateTime.UtcNow - started).TotalMinutes > 2))
                    {
                        Delete(msg.TransferId);
                        throw new TimeoutException();
                    }
                }
                if (msg.State >= TransferState.StartSessionKey)
                    Check.Assert<InvalidDataException>(HasState(msg.TransferId, "session-key"));
            }

            private Message NonceRequest(Message req)
            {
                req.VerifySignature(_clientKey);

                Check.Assert<InvalidDataException>(HasState(req.TransferId, "start-time") == false);
                WriteState(req.TransferId, "start-time", DateTime.UtcNow.Ticks.ToString());

                byte[] nonce = new byte[NonceSize];
                new Random().NextBytes(nonce);
                WriteState(req.TransferId, "nonce", Convert.ToBase64String(nonce));

                byte[] keydata = new byte[KeyBytes];
                new System.Security.Cryptography.RNGCryptoServiceProvider().GetBytes(keydata);
                WriteState(req.TransferId, "server-key", Convert.ToBase64String(keydata));

                Message response = new Message(TransferState.NonceResponse, req.TransferId, _clientKey, NoSession);
                response.Write(nonce);
                response.Write(keydata);
                return response;
            }

            private static Salt SessionSecret(byte[] clientKeyBits, byte[] serverKeyBits)
            {
                Salt sessionSecret = Salt.FromBytes(
                    Hash.SHA256(
                        new CombinedStream(
                            new MemoryStream(clientKeyBits, false),
                            new MemoryStream(serverKeyBits, false)
                            )
                        ).ToArray()
                    );
                return sessionSecret;
            }

            private Message TransferRequest(Message req)
            {
                Check.Assert<InvalidDataException>(HasState(req.TransferId, "start-time")
                    && HasState(req.TransferId, "nonce")
                    && HasState(req.TransferId, "server-key")
                    && !HasState(req.TransferId, "session-key"));

                byte[] nonceExpected = Convert.FromBase64String(ReadState(req.TransferId, "nonce"));
                DeleteState(req.TransferId, "nonce");

                byte[] serverKeyBits = Convert.FromBase64String(ReadState(req.TransferId, "server-key"));
                DeleteState(req.TransferId, "server-key");

                byte[] hnonce = req.ReadBytes(32);
                long length = req.ReadInt64();
                string name = req.ReadString(1024);
                byte[] clientKeyBits = req.ReadBytes(32);
                req.VerifySignature(_clientKey);

                Check.Assert<InvalidDataException>(
                    Hash.SHA256(nonceExpected).Equals(Hash.FromBytes(hnonce))
                );

                OnBeginTransfer(req.TransferId, name, length);

                WriteState(req.TransferId, "total-length", length.ToString());
                WriteState(req.TransferId, "location", name ?? String.Empty);
                WriteState(req.TransferId, "session-key", SessionSecret(clientKeyBits, serverKeyBits).ToString());

                Message response = new Message(TransferState.UploadResponse, req.TransferId, _clientKey, SessionKey);
                response.Write(MaxInboundFileChunk);
                return response;
            }

            private Message SendBytesRequest(Message req)
            {
                long position = req.ReadInt64();
                byte[] bytes = req.ReadBytes(MaxInboundFileChunk);
                req.VerifySignature(_clientKey);

                string location = ReadState(req.TransferId, "location");
                long totalSize = long.Parse(ReadState(req.TransferId, "total-length"));
                Check.InRange(position, 0, totalSize - bytes.Length);

                OnBytesReceived(req.TransferId, location, totalSize, position, bytes);

                Message response = new Message(TransferState.SendBytesResponse, req.TransferId, _clientKey, SessionKey);
                response.Write(position);
                return response;
            }

            private Message CompleteRequest(Message req)
            {
                try
                {
                    string name = req.ReadString(1024);
                    Hash contentHash = Hash.FromBytes(req.ReadBytes(32));
                    req.VerifySignature(_clientKey);

                    long totalSize = long.Parse(ReadState(req.TransferId, "total-length"));
                    string location = ReadState(req.TransferId, "location");
                    Check.Assert<InvalidDataException>(location == name);

                    OnCompleteTransfer(req.TransferId, location, totalSize, contentHash);

                    Message response = new Message(TransferState.UploadCompleteResponse, req.TransferId, _clientKey, SessionKey);
                    return response;
                }
                finally
                {
                    Delete(req.TransferId);
                }
            }

            private Message DownloadRequest(Message req)
            {
                Check.Assert<InvalidDataException>(HasState(req.TransferId, "start-time")
                    && HasState(req.TransferId, "nonce")
                    && HasState(req.TransferId, "server-key")
                    && !HasState(req.TransferId, "session-key"));

                byte[] nonceExpected = Convert.FromBase64String(ReadState(req.TransferId, "nonce"));
                DeleteState(req.TransferId, "nonce");

                byte[] serverKeyBits = Convert.FromBase64String(ReadState(req.TransferId, "server-key"));
                DeleteState(req.TransferId, "server-key");

                byte[] hnonce = req.ReadBytes(32);
                string name = req.ReadString(1024);
                byte[] clientKeyBits = req.ReadBytes(32);
                req.VerifySignature(_clientKey);

                Check.Assert<InvalidDataException>(
                    Hash.SHA256(nonceExpected).Equals(Hash.FromBytes(hnonce))
                );

                Salt sessionKey = SessionSecret(clientKeyBits, serverKeyBits);
                long length;
                OnDownloadBytes(req.TransferId, name, out length, 0, 0, Stream.Null);

                Message response = new Message(TransferState.DownloadResponse, req.TransferId, _clientKey, s => sessionKey);
                response.Write(MaxOutboundFileChunk);
                response.Write(length);
                if(length <= MaxOutboundFileChunk)
                {
                    Delete(req.TransferId);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        OnDownloadBytes(req.TransferId, name, out length, 0, (int)length, ms);
                        Check.Assert<InvalidDataException>(ms.Position == length);
                        response.Write(ms.ToArray());
                    }
                }
                else
                {
                    WriteState(req.TransferId, "total-length", length.ToString());
                    WriteState(req.TransferId, "location", name ?? String.Empty);
                    WriteState(req.TransferId, "session-key", sessionKey.ToString());
                    response.Write(new byte[0]);
                }

                return response;
            }

            private Message DownloadBytesRequest(Message req)
            {
                string location = req.ReadString(1024);
                long position = req.ReadInt64();
                int count = req.ReadInt32();
                req.VerifySignature(_clientKey);

                Check.Assert<InvalidDataException>(location == ReadState(req.TransferId, "location"));
                long totalSize = long.Parse(ReadState(req.TransferId, "total-length"));
                Check.InRange(position, 0, totalSize);
                Check.InRange(position + count, 0, totalSize);

                Message response = new Message(TransferState.DownloadBytesResponse, req.TransferId, _clientKey, SessionKey);

                using (MemoryStream ms = new MemoryStream())
                {
                    long length;
                    OnDownloadBytes(req.TransferId, location, out length, position, count, ms);
                    Check.Assert<InvalidDataException>(length == totalSize);
                    Check.Assert<InvalidDataException>(ms.Position == count);
                    response.Write(ms.ToArray());
                }
                return response;
            }

            private Message DownloadCompleteRequest(Message req)
            {
                req.VerifySignature(_clientKey);
                Delete(req.TransferId);
                return Message.EmptyMessage;
            }
        }
    }
}