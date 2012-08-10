#region Copyright 2011-2012 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.ComponentModel;
using System.Security.Cryptography;
using System.Threading;
using CSharpTest.Net.IO;
using CSharpTest.Net.Threading;
using CSharpTest.Net.Bases;
using CSharpTest.Net.Interfaces;

namespace CSharpTest.Net.Crypto
{
    /// <summary>
    /// SecureTransfer is a static class that contains two user types, Sender and Receiver.  Each provide 
    /// one-half of a secure file transfer protocol.  The security is based on pre-shared public keys used
    /// to sign all messages between the client and server.  Additionally these keys are used durring the
    /// session negotiation to exchange a 256-bit session key.  The session key is combined with a random
    /// salt for each message to produce an AES-256 cryptographic key.  The file content is then tranfered
    /// with this session key.
    /// </summary>
    public static partial class SecureTransfer
    {
        /// <summary>
        /// Provides a file transfer handler for the client-side of file transfers. 
        /// </summary>
        public class Client
        {
            private readonly RSAPrivateKey _privateKey;
            private readonly RSAPublicKey _publicKey;
            private readonly TransmitMessageAction _sendMessage;
            private readonly ManualResetEvent _abort;

            /// <summary>
            /// Provides the transmission of a stream of bytes to the server/receiver and returns the result stream
            /// </summary>
            public delegate Stream TransmitMessageAction(Guid transferId, string location, Stream request);

            /// <summary>
            /// Constructed to send one or more files to a remove server identified by serverKey.  The transfer
            /// is a blocking call and returns on success or raises an exception.  If Abort() is called durring
            /// the transfer, or if a ProgressChanged event handler raises the OperationCanceledException, the
            /// transfer is silently terminated.
            /// </summary>
            /// <param name="privateKey">The private key for this client</param>
            /// <param name="serverKey">The public key of the server</param>
            /// <param name="sendMessage">A delegate to transfer data to the server and obtain a response</param>
            public Client(RSAPrivateKey privateKey, RSAPublicKey serverKey, TransmitMessageAction sendMessage)
            {
                _privateKey = Check.NotNull(privateKey);
                _publicKey = Check.NotNull(serverKey);
                _sendMessage = Check.NotNull(sendMessage);
                _abort = new ManualResetEvent(false);
                LimitThreads = 10;
            }

            /// <summary> The maximum number of concurrent calls to the server </summary>
            public int LimitThreads { get; set; }

            /// <summary>
            /// Raised after each block of data is transferred to the server.
            /// </summary>
            public event ProgressChangedEventHandler ProgressChanged;
            private void OnProgressChanged(string location, long current, long total)
            {
                ProgressChangedEventHandler handler = ProgressChanged;
                if (handler != null)
                {
                    long percent = (current*100L)/total;
                    try
                    {
                        handler(this, new ProgressChangedEventArgs((int)percent, location));
                    }
                    catch (OperationCanceledException)
                    {
                        Abort();
                    }
                }
            }

            /// <summary> Aborts the transfer </summary>
            public void Abort()
            {
                _abort.Set();
            }

            private Stream SendPayload(Message req, string location, Stream stream)
            {
                return _sendMessage(req.TransferId, location, stream);
            }

            /// <summary>
            /// Called to send a single file to the remove server identified by serverKey.  The transfer
            /// is a blocking call and returns on success or raises an exception.  If Abort() is called durring
            /// the transfer, or if a ProgressChanged event handler raises the OperationCanceledException, the
            /// transfer is silently terminated and the method will return false.
            /// </summary>
            /// <param name="location">A string of up to 1024 bytes in length</param>
            /// <param name="filePath">The file path of the file to transfer</param>
            /// <returns>True if the file was successfully received by the server</returns>
            public bool Upload(string location, string filePath)
            {
                using (Stream input = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    return Upload(location, input.Length, input);
            }

            /// <summary>
            /// Called to send a specific length of bytes to a server identified by serverKey.  The transfer
            /// is a blocking call and returns on success or raises an exception.  If Abort() is called durring
            /// the transfer, or if a ProgressChanged event handler raises the OperationCanceledException, the
            /// transfer is silently terminated and the method will return false.
            /// </summary>
            /// <param name="location">A string of up to 1024 bytes in length</param>
            /// <param name="length">The length in bytes to send from the stream</param>
            /// <param name="rawInput">The stream to read the data from</param>
            /// <returns>True if the file was successfully received by the server</returns>
            public bool Upload(string location, long length, Stream rawInput)
            {
                Guid transferId = Guid.NewGuid();
                int maxMessageLength;
                // STEP 1: Send a NonceRequest, Create
                Salt sessionKey = BeginUpload(transferId, location, length, out maxMessageLength);

                // STEP 2: Send the data
                Hash fullHash;
                bool[] failed = new bool[1];

                using (HashStream input = new HashStream(new SHA256Managed(), rawInput))
                using (WorkQueue queue = new WorkQueue(LimitThreads))
                {
                    queue.OnError += (o, e) => { failed[0] = true; };
                    long pos = 0;
                    while (pos < length && !failed[0] && !_abort.WaitOne(0, false))
                    {
                        int len = (int)Math.Min(length - pos, maxMessageLength);
                        byte[] buffer = new byte[len];
                        IOStream.Read(input, buffer, len);
                        BytesToSend task = new BytesToSend(this, LimitThreads, transferId, sessionKey, location, pos, buffer);
                        queue.Enqueue(task.Send);
                        OnProgressChanged(location, pos, length);
                        pos += len;
                    }

                    queue.Complete(true, failed[0] ? 5000 : 300000);
                    fullHash = input.FinalizeHash();//hash of all data transferred
                }
                if (_abort.WaitOne(0, false))
                    return false;
                Check.Assert<InvalidDataException>(failed[0] == false);

                // STEP 4: Complete the transfer
                CompleteUpload(transferId, sessionKey, location, fullHash);
                OnProgressChanged(location, length, length);
                return true;
            }

            /// <summary>
            /// Called to send a specific length of bytes to a server identified by serverKey.  The transfer
            /// is a blocking call and returns on success or raises an exception.  If Abort() is called durring
            /// the transfer, or if a ProgressChanged event handler raises the OperationCanceledException, the
            /// transfer is silently terminated and the method will return false.
            /// </summary>
            /// <param name="location">A string of up to 1024 bytes in length</param>
            /// <param name="output">Any writable stream that can seek</param>
            /// <returns>True if the file was successfully received by the server</returns>
            public bool Download(string location, Stream output)
            {
                Check.Assert<ArgumentException>(output.CanSeek && output.CanWrite,
                                                "The download stream must be able to seek and write.");

                return Download(location, new StreamCache(new InstanceFactory<Stream>(output), 1));
            }

            /// <summary>
            /// Called to send a specific length of bytes to a server identified by serverKey.  The transfer
            /// is a blocking call and returns on success or raises an exception.  If Abort() is called durring
            /// the transfer, or if a ProgressChanged event handler raises the OperationCanceledException, the
            /// transfer is silently terminated and the method will return false.
            /// </summary>
            /// <param name="location">A string of up to 1024 bytes in length</param>
            /// <param name="filename">The name of the file to write to</param>
            /// <returns>True if the file was successfully received by the server</returns>
            public bool Download(string location, string filename)
            {
                FileStreamFactory file = new FileStreamFactory(filename, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                using (TempFile temp = TempFile.Attach(filename))
                using (StreamCache cache = new StreamCache(file, LimitThreads))
                {
                    temp.Length = 0;
                    if (Download(location, cache))
                    {
                        temp.Detatch();
                        return true;
                    }
                }
                return false;
            }

            private static Salt SessionSecret(Salt clientKeyBits, byte[] serverKeyBits)
            {
                Salt sessionSecret = Salt.FromBytes(
                    Hash.SHA256(
                        new CombinedStream(
                            clientKeyBits.ToStream(),
                            new MemoryStream(serverKeyBits, false)
                            )
                        ).ToArray()
                    );
                return sessionSecret;
            }

            private byte[] GetNonce(Guid transferId, string location, out byte[] serverKeyBits)
            {
                byte[] nonce;
                // STEP 1: Send a NonceRequest
                using (Message req = new Message(TransferState.NonceRequest, transferId, _publicKey, NoSession))
                {
                    Stream response = SendPayload(req, location, req.ToStream(_privateKey));
                    using (Message rsp = new Message(response, _privateKey, NoSession))
                    {
                        Check.Assert<InvalidOperationException>(rsp.State == TransferState.NonceResponse);
                        nonce = rsp.ReadBytes(1024);
                        serverKeyBits = rsp.ReadBytes(1024);
                        rsp.VerifySignature(_publicKey);
                    }
                }
                return nonce;
            }
            private bool Download(string location, StreamCache output)
            {
                int maxMessageLength;
                long fileLength, bytesReceived = 0;
                Guid transferId = Guid.NewGuid();
                byte[] serverKeyBits;
                byte[] nonce = GetNonce(transferId, location, out serverKeyBits);
                Hash hnonce = Hash.SHA256(nonce);

                //STEP 2: Create and send session key
                Salt clientKeyBits = new Salt(Salt.Size.b256);
                Salt sessionKey = SessionSecret(clientKeyBits, serverKeyBits);

                using (Message req = new Message(TransferState.DownloadRequest, transferId, _publicKey, NoSession))
                {
                    req.Write(hnonce.ToArray());
                    req.Write(location);
                    req.Write(clientKeyBits.ToArray());

                    Stream response = SendPayload(req, location, req.ToStream(_privateKey));
                    using (Message rsp = new Message(response, _privateKey, s=>sessionKey))
                    {
                        Check.Assert<InvalidOperationException>(rsp.State == TransferState.DownloadResponse);
                        maxMessageLength = Check.InRange(rsp.ReadInt32(), 0, int.MaxValue);
                        fileLength = Check.InRange(rsp.ReadInt64(), 0, 0x00FFFFFFFFFFFFFFL);
                        byte[] bytes = rsp.ReadBytes(100 * 1000 * 1024);
                        rsp.VerifySignature(_publicKey);

                        using(Stream io = output.Open(FileAccess.Write))
                        {
                            io.SetLength(fileLength);
                            if (bytes.Length > 0)
                            {
                                io.Seek(0, SeekOrigin.Begin);
                                io.Write(bytes, 0, bytes.Length);
                                bytesReceived += bytes.Length;
                            }
                        }
                    }
                }
                //STEP 3...n: Continue downloading other chunks of the file
                if (bytesReceived < fileLength)
                {
                    bool[] failed = new bool[1];
                    using (WorkQueue queue = new WorkQueue(LimitThreads))
                    {
                        queue.OnError += (o, e) => { failed[0] = true; };
                        while (bytesReceived < fileLength && !failed[0] && !_abort.WaitOne(0, false))
                        {
                            int len = (int) Math.Min(fileLength - bytesReceived, maxMessageLength);
                            BytesToRead task = new BytesToRead(
                                this, LimitThreads, transferId, sessionKey, location, output, bytesReceived, len);
                            queue.Enqueue(task.Send);
                            OnProgressChanged(location, bytesReceived, fileLength);
                            bytesReceived += len;
                        }

                        queue.Complete(true, failed[0] ? 5000 : 7200000);
                    }

                    if (_abort.WaitOne(0, false))
                        return false;
                    Check.Assert<InvalidDataException>(failed[0] == false);

                    // STEP 4: Complete the transfer
                    using (Message req = new Message(TransferState.DownloadCompleteRequest, transferId, _publicKey, s => sessionKey))
                    {
                        SendPayload(req, location, req.ToStream(_privateKey)).Dispose();
                    }
                }
                OnProgressChanged(location, fileLength, fileLength);
                return true;
            }

            private Salt BeginUpload(Guid transferId, string location, long length, out int maxMessageLength)
            {
                byte[] serverKeyBits;
                byte[] nonce = GetNonce(transferId, location, out serverKeyBits);
                Hash hnonce = Hash.SHA256(nonce);
                
                //STEP 2: Create and send session key
                Salt clientKeyBits = new Salt(Salt.Size.b256);
                Salt sessionSecret = SessionSecret(clientKeyBits, serverKeyBits);

                using (Message req = new Message(TransferState.UploadRequest, transferId, _publicKey, NoSession))
                {
                    req.Write(hnonce.ToArray());
                    req.Write(length);
                    req.Write(location);
                    req.Write(clientKeyBits.ToArray());

                    Stream response = SendPayload(req, location, req.ToStream(_privateKey));
                    using (Message rsp = new Message(response, _privateKey, s=>sessionSecret))
                    {
                        Check.Assert<InvalidOperationException>(rsp.State == TransferState.UploadResponse);
                        maxMessageLength = Check.InRange(rsp.ReadInt32(), 0, int.MaxValue);
                        rsp.VerifySignature(_publicKey);
                    }
                }
                return sessionSecret;
            }

            private void TransferBytes(Guid transferId, Salt sessionKey, string location, long offset, byte[] bytes)
            {
                // STEP 3...n: Send a block of bytes
                using (Message req = new Message(TransferState.SendBytesRequest, transferId, _publicKey, s => sessionKey))
                {
                    req.Write(offset);
                    req.Write(bytes);

                    Stream response = SendPayload(req, location, req.ToStream(_privateKey));
                    using (Message rsp = new Message(response, _privateKey, s => sessionKey))
                    {
                        Check.Assert<InvalidOperationException>(rsp.State == TransferState.SendBytesResponse);
                        Check.Assert<InvalidOperationException>(rsp.ReadInt64() == offset);
                        rsp.VerifySignature(_publicKey);
                    }
                }
            }

            private void ReadByteRange(Guid transferId, Salt sessionKey, string location, StreamCache streams, long offset, int count)
            {
                using (Message req = new Message(TransferState.DownloadBytesRequest, transferId, _publicKey, s=>sessionKey))
                {
                    req.Write(location);
                    req.Write(offset);
                    req.Write(count);

                    Stream response = SendPayload(req, location, req.ToStream(_privateKey));
                    using (Message rsp = new Message(response, _privateKey, s=>sessionKey))
                    {
                        Check.Assert<InvalidOperationException>(rsp.State == TransferState.DownloadBytesResponse);
                        byte[] bytes = rsp.ReadBytes(100 * 1000 * 1024);
                        Check.Assert<InvalidOperationException>(bytes.Length == count);
                        
                        rsp.VerifySignature(_publicKey);

                        using(Stream io = streams.Open(FileAccess.Write))
                        {
                            io.Seek(offset, SeekOrigin.Begin);
                            io.Write(bytes, 0, count);
                        }
                    }
                }
            }

            private void CompleteUpload(Guid transferId, Salt sessionKey, string location, Hash fullHash)
            {
                // STEP 4: Finalize the transfer
                using (Message req = new Message(TransferState.UploadCompleteRequest, transferId, _publicKey, s => sessionKey))
                {
                    req.Write(location);
                    req.Write(fullHash.ToArray());

                    Stream response = SendPayload(req, location, req.ToStream(_privateKey));
                    using (Message rsp = new Message(response, _privateKey, s => sessionKey))
                    {
                        Check.Assert<InvalidOperationException>(rsp.State == TransferState.UploadCompleteResponse);
                        rsp.VerifySignature(_publicKey);
                    }
                }
            }

            #region BytesToSend
            private class BytesToSend : Disposable
            {
                private bool _aquired;
                private readonly Semaphore _throttle;
                private readonly Client _client;
                private readonly Guid _transferId;
                private readonly Salt _sessionKey;
                private readonly string _location;
                private readonly byte[] _bytes;
                private readonly long _offset;

                public BytesToSend(Client client, int threadLimit, Guid transferId, Salt sessionKey, string location, long offset, byte[] bytes)
                {
                    _client = client;
                    _transferId = transferId;
                    _sessionKey = sessionKey;
                    _location = location;
                    _bytes = bytes;
                    _offset = offset;

                    _throttle = new Semaphore(threadLimit, threadLimit, GetType().FullName);
                    _throttle.WaitOne();
                    _aquired = true;
                }

                protected override void Dispose(bool disposing)
                {
                    if (_aquired)
                    {
                        _aquired = false;
                        _throttle.Release();
                        if (disposing)
                            _throttle.Close();
                    }
                }

                public void Send()
                {
                    using(this)
                        _client.TransferBytes(_transferId, _sessionKey, _location, _offset, _bytes);
                }
            }
            #endregion
            #region BytesToRead
            private class BytesToRead : Disposable
            {
                private bool _aquired;
                private readonly Semaphore _throttle;
                private readonly Client _client;
                private readonly Guid _transferId;
                private readonly Salt _sessionKey;
                private readonly string _location;
                private readonly StreamCache _streams;
                private readonly long _offset;
                private readonly int _count;

                public BytesToRead(Client client, int threadLimit, Guid transferId, Salt sessionKey, string location, StreamCache streams, long offset, int count)
                {
                    _client = client;
                    _transferId = transferId;
                    _sessionKey = sessionKey;
                    _location = location;
                    _streams = streams;
                    _offset = offset;
                    _count = count;

                    _throttle = new Semaphore(threadLimit, threadLimit, GetType().FullName);
                    _throttle.WaitOne();
                    _aquired = true;
                }

                protected override void Dispose(bool disposing)
                {
                    if (_aquired)
                    {
                        _aquired = false;
                        _throttle.Release();
                        if (disposing)
                            _throttle.Close();
                    }
                }

                public void Send()
                {
                    using(this)
                        _client.ReadByteRange(_transferId, _sessionKey, _location, _streams, _offset, _count);
                }
            }
            #endregion
        }
    }
}