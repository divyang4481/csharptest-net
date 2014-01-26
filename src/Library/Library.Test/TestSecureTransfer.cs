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
using CSharpTest.Net.Crypto;
using NUnit.Framework;
using CSharpTest.Net.Serialization.StorageClasses;
using CSharpTest.Net.IO;

#pragma warning disable 1591
namespace CSharpTest.Net.Library.Test
{
    [TestFixture]
    public class TestSecureTransfer
    {
        private RSAPrivateKey _clientKey, _serverKey;
        //WARNING these are small for faster testing only, for production use at LEAST 2048
        RSAPrivateKey ClientKey { get { return _clientKey ?? (_clientKey = new RSAPrivateKey(512)); } }
        RSAPrivateKey ServerKey { get { return _serverKey ?? (_serverKey = new RSAPrivateKey(768)); } }

        [Test]
        public void TestSimpleUpload()
        {
            byte[] src  = new byte[ushort.MaxValue];
            byte[] work = null;
            byte[] dest = null;
            new Random().NextBytes(src);
            Hash hashSent = null;
            Guid transfer = Guid.Empty;

            SecureTransfer.Server server = new SecureTransfer.Server(ServerKey, ClientKey.PublicKey, new RegistryStorage());
            server.MaxInboundFileChunk = 1000;
            server.BeginTransfer += (o, e) => { work = new byte[e.TotalSize]; transfer = e.TransferId; };
            server.CompleteTransfer += (o, e) => { dest = work; hashSent = e.ContentHash; };
            server.BytesReceived +=
                (o, e) =>
                    {
                        Assert.AreEqual(transfer, e.TransferId);
                        Assert.AreEqual(src.Length, e.TotalSize);
                        Assert.AreEqual("bla", e.Location);
                        Array.Copy(e.BytesReceived, 0, work, e.WriteOffset, e.BytesReceived.Length);
                    };

            SecureTransfer.Client client = new SecureTransfer.Client(ClientKey, ServerKey.PublicKey,
                (id, name, stream) => server.Receive(stream));

            client.Upload("bla", src.Length, new MemoryStream(src, false));

            Assert.AreEqual(Hash.SHA256(src), hashSent);
            Assert.AreEqual(Hash.SHA256(src), Hash.SHA256(dest));
        }

        [Test]
        public void TestSimpleDownload()
        {
            byte[] src = new byte[ushort.MaxValue];
            MemoryStream dest = new MemoryStream();
            new Random().NextBytes(src);

            SecureTransfer.Server server = new SecureTransfer.Server(ServerKey, ClientKey.PublicKey, new RegistryStorage());
            server.MaxOutboundFileChunk = 1000;
            server.DownloadBytes +=
                (o, e) =>
                {
                    using (MemoryStream ms = new MemoryStream(e.ReadLength))
                    {
                        ms.Write(src, (int)e.ReadOffset, e.ReadLength);
                        e.SetBytes(src.Length, ms.ToArray());
                    }
                };

            SecureTransfer.Client client = new SecureTransfer.Client(ClientKey, ServerKey.PublicKey,
                (id, name, stream) => server.Receive(stream));

            client.Download("bla", dest);
            Assert.AreEqual(Hash.SHA256(src), Hash.SHA256(dest.ToArray()));
        }

        [Test]
        public void TestDownloadOneChunk()
        {
            byte[] src = new byte[1024];
            MemoryStream dest = new MemoryStream();
            new Random().NextBytes(src);
            Hash expectHash = Hash.SHA256(src);

            SecureTransfer.Server server = new SecureTransfer.Server(ServerKey, ClientKey.PublicKey, new RegistryStorage());
            server.MaxOutboundFileChunk = src.Length;
            server.DownloadBytes +=
                (o, e) =>
                {
                    if (e.ReadLength == 0)
                        e.SetBytes(src.Length, null);
                    else
                    {
                        e.SetBytes(src.Length, src);
                        src = null;
                    }
                };

            SecureTransfer.Client client = new SecureTransfer.Client(ClientKey, ServerKey.PublicKey,
                (id, name, stream) => server.Receive(stream));

            client.Download("bla", dest);
            Assert.IsNull(src);
            Assert.AreEqual(expectHash, Hash.SHA256(dest.ToArray()));
        }

        [Test]
        public void TestDownloadToFile()
        {
            byte[] src = new byte[ushort.MaxValue];
            new Random().NextBytes(src);

            using (TempFile file = new TempFile())
            {
                SecureTransfer.Server server = new SecureTransfer.Server(ServerKey, ClientKey.PublicKey, new RegistryStorage());
                server.MaxOutboundFileChunk = 1000;
                server.DownloadBytes +=
                    (o, e) =>
                    {
                        using (MemoryStream ms = new MemoryStream(e.ReadLength))
                        {
                            ms.Write(src, (int)e.ReadOffset, e.ReadLength);
                            e.SetBytes(src.Length, ms.ToArray());
                        }
                    };

                SecureTransfer.Client client = new SecureTransfer.Client(ClientKey, ServerKey.PublicKey,
                    (id, name, stream) => server.Receive(stream));

                client.Download("bla", file.TempPath);
                Assert.AreEqual(Hash.SHA256(src), Hash.SHA256(file.Read()));
            }
        }

        [Test]
        public void TestAbort()
        {
            byte[] src = new byte[1024];
            SecureTransfer.Server server = new SecureTransfer.Server(ServerKey, ClientKey.PublicKey, new RegistryStorage());
            SecureTransfer.Client client = new SecureTransfer.Client(ClientKey, ServerKey.PublicKey,
                (id, name, stream) => server.Receive(stream));

            client.Abort();
            Assert.IsFalse(client.Upload("bla", src.Length, new MemoryStream(src, false)));
        }

        [Test]
        public void TestAbortByEvent()
        {
            SecureTransfer.Server server = new SecureTransfer.Server(ServerKey, ClientKey.PublicKey, new RegistryStorage());
            SecureTransfer.Client client = new SecureTransfer.Client(ClientKey, ServerKey.PublicKey,
                (id, name, stream) => server.Receive(stream));

            client.ProgressChanged += (o, e) => { throw new OperationCanceledException(); };
            using (TempFile file = new TempFile())
            {
                file.WriteAllBytes(new byte[1024]);
                Assert.IsFalse(client.Upload("bla", file.TempPath));
            }
        }

        [Test]
        public void TestRecieverError()
        {
            Exception error = null;
            SecureTransfer.Server server = new SecureTransfer.Server(ServerKey, ClientKey.PublicKey, new RegistryStorage());
            server.ErrorRaised += (o, e) => error = e.GetException();
            try
            {
                server.Receive(new MemoryStream(new byte[1024]));
                Assert.Fail();
            }
            catch (InvalidDataException) { }
            Assert.IsNotNull(error);
        }

        [Test]
        public void TestDownloadFails()
        {
            SecureTransfer.Server server = new SecureTransfer.Server(ServerKey, ClientKey.PublicKey, new RegistryStorage());
            server.MaxOutboundFileChunk = 1000;
            server.DownloadBytes += (o, e) => e.SetBytes(ushort.MaxValue, null);

            SecureTransfer.Client client = new SecureTransfer.Client(ClientKey, ServerKey.PublicKey,
                (id, name, stream) => server.Receive(stream));

            MemoryStream dest = new MemoryStream();
            try
            {
                client.Download("bla", dest);
                Assert.Fail();
            }
            catch(InvalidDataException) { }
        }

        [Test, ExpectedException(typeof(InvalidDataException))]
        public void TestSendingError()
        {
            int count = 0;
            byte[] src = new byte[1024];
            SecureTransfer.Server server = new SecureTransfer.Server(ServerKey, ClientKey.PublicKey, new RegistryStorage());
            SecureTransfer.Client client = new SecureTransfer.Client(ClientKey, ServerKey.PublicKey,
                (id, name, stream) =>
                {
                    if (++count < 3)
                        return server.Receive(stream);
                    //Bombs after negotiation
                    throw new ApplicationException();
                }
            );

            client.Upload("bla", src.Length, new MemoryStream(src, false));
            Assert.Fail();
        }
    }
}
