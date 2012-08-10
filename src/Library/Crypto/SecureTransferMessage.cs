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
using System.Security.Cryptography;
using CSharpTest.Net.Serialization;
using CSharpTest.Net.IO;

namespace CSharpTest.Net.Crypto
{
    partial class SecureTransfer
    {
        private static readonly Converter<Guid, Salt> NoSession = null;

        private enum TransferState : int
        {
            //step 1: client sends a signed random transferId encrypted with server's public key
            NonceRequest = 0x01,
            //step 2: server responds with our transferId a length-prefixed nonce signed and encrypted with our public key
            NonceResponse = 0x02,
            
            DownloadRequest = 0x80,
            UploadRequest = 0xFF,

            //All message IDs after 0x100 are expected to be using session keys instead of PKI
            StartSessionKey = 0x100,

            DownloadResponse = 0x102,
            DownloadBytesRequest = 0x103,
            DownloadBytesResponse = 0x104,
            DownloadCompleteRequest = 0x105,

            UploadResponse = 0x1000,

            SendBytesRequest = 0x1001,
            SendBytesResponse = 0x1002,

            UploadCompleteRequest = 0x2001,
            UploadCompleteResponse = 0x2002,
        }

        private class Message : IDisposable
        {
            private const int VersionHeader = 0x00010402;
            public static readonly Message EmptyMessage = new Message();

            private Message()
            {
                //EmptyMessage:
                _version = 0;
                _state = 0;
                _transferId = Guid.Empty;
                _salt = null;
                (_protected = new MemoryStream()).Dispose();
                _payload = _protected;
                (_hash = new HashStream(new SHA256Managed())).Dispose();
            }

            private readonly int _version;
            private readonly TransferState _state;
            private readonly Guid _transferId;
            private readonly Salt _salt;
            private readonly MemoryStream _protected;
            private readonly Stream _payload;
            private readonly HashStream _hash;
            //private bool _verified; /* this is a debugging aid used to ensure all messages are signed or verified */

            public Message(TransferState state, Guid transferId, RSAPublicKey key, Converter<Guid, Salt> sessionSecret)
            {
                _version = VersionHeader;
                _state = state;
                _transferId = transferId;
                _salt = new Salt(Salt.Size.b256);
                _protected = new MemoryStream();
                _payload = new NonClosingStream(_protected);
                _hash = new HashStream(new SHA256Managed());
                WriteHeader(_hash);
                Salt secret;

                if (!UsesSessionKey)
                {
                    // Outer encryption is straight PKI based on the remote public key
                    _payload = key.Encrypt(_payload);
                    _hash.ChangeStream(_payload);
                    // Preceed the message with a new, AES key
                    secret = new Salt(Salt.Size.b256);
                    _hash.Write(secret.ToArray(), 0, 32);
                }
                else
                {
                    secret = sessionSecret(_transferId);
                    Check.IsEqual(32, Check.NotNull(secret).Length);
                }

                AESCryptoKey sessionKey = new AESCryptoKey(
                    // Prefix the key with the message's salt and compute a SHA256 hash to be used as the key
                    Hash.SHA256(_salt.GetData(secret.ToArray()).ToStream()).ToArray(),
                    // Compute an IV for this aes key and salt combination
                    IV(secret, _salt)
                );

                _payload = sessionKey.Encrypt(_payload);
                _hash.ChangeStream(_payload);
            }

            public Message(Stream input, RSAPrivateKey key, Converter<Guid, Salt> sessionSecret)
            {
                (_protected = new MemoryStream(0)).Dispose();
                _hash = new HashStream(new SHA256Managed(), input);
                _payload = input;

                ReadHeader(_hash, out _version, out _state, out _transferId, out _salt);
                
                Salt secret;
                if (!UsesSessionKey)
                {
                    // Begin private key decryption
                    _payload = key.Decrypt(input);
                    _hash.ChangeStream(_payload);
                    // Decrypt the aes key used in this package
                    byte[] keybits = IOStream.Read(_hash, 32);
                    secret = Salt.FromBytes(keybits);
                }
                else
                {
                    secret = sessionSecret(_transferId);
                    Check.IsEqual(32, Check.NotNull(secret).Length);
                }

                AESCryptoKey sessionKey = new AESCryptoKey(
                    // Prefix the key with the message's salt and compute a SHA256 hash to be used as the key
                    Hash.SHA256(_salt.GetData(secret.ToArray()).ToStream()).ToArray(),
                    // Compute an IV for this aes key and salt combination
                    IV(secret, _salt)
                );
                
                _payload = sessionKey.Decrypt(_payload);
                _hash.ChangeStream(_payload);
            }

            private byte[] IV(Salt secret, Salt salt)
            {
                // Long story, this has been a little difficult to finally settle upon an algorithm.
                // We know we don't want to the same value each time, and we prefer not to use only
                // the public salt.  My biggest concern is not generating a IV value that interacts
                // with AES in a way that might divulge information unintentionally.  Since we are
                // using the same values (salt+secret) to derive the IV this is a very real risk.
                // To mitigate this risk the primary interaction of secret in the derivation of this
                // value is compressed into a CRC of both the salt and secret.  This value is then 
                // masked with computations of salt to produce the 16 IV bytes needed.
                byte[] sbytes = salt.ToArray();
                byte[] result = new byte[16];
                // compute a mask from CRC32(salt + secret)
                int mask = new Crc32(_salt.GetData(secret.ToArray()).ToArray()).Value;
                // xor part of the mask with the sum of two salt bytes
                for (int i = 0; i < 16; i++)
                    result[i] = (byte)((mask >> i) ^ (sbytes[i] + sbytes[i + 16]));

                return result;
            }

            public void Dispose()
            {
                _protected.Dispose();
                _payload.Dispose();
                _hash.Dispose();
                //Check.Assert<InvalidOperationException>(_verified);
            }

            public TransferState State { get { return _state; } }
            public Guid TransferId { get { return _transferId; } }
            private bool UsesSessionKey { get { return _state >= TransferState.StartSessionKey; } }

            private void WriteHeader(Stream stream)
            {
                PrimitiveSerializer.Int32.WriteTo(_version, stream);
                PrimitiveSerializer.Int32.WriteTo((int)_state, stream);
                stream.Write(_transferId.ToByteArray(), 0, 16);
                stream.Write(_salt.ToArray(), 0, 32);
            }

            public Stream ToStream(RSAPrivateKey signingKey)
            {
                if (ReferenceEquals(EmptyMessage, this))
                    return Stream.Null;

                // Next version's data-length
                Write(0);

                byte[] signature = signingKey.SignHash(_hash.FinalizeHash());
                PrimitiveSerializer.Bytes.WriteTo(signature, _payload);
                //_verified = true;

                _hash.Close();
                _payload.Close();
                _protected.Position = 0;

                MemoryStream header = new MemoryStream();
                WriteHeader(header);
                header.Position = 0;

                return new CombinedStream(header, _protected);
            }

            #region Write(...)
            public void Write(int value)
            {
                PrimitiveSerializer.Int32.WriteTo(value, _hash);
            }

            public void Write(long value)
            {
                PrimitiveSerializer.Int64.WriteTo(value, _hash);
            }

            public void Write(string value)
            {
                PrimitiveSerializer.String.WriteTo(value, _hash);
            }

            public void Write(byte[] value)
            {
                PrimitiveSerializer.Bytes.WriteTo(value, _hash);
            }
            #endregion

            private static void ReadHeader(Stream input, out int ver, out TransferState state, out Guid txid, out Salt salt)
            {
                ver = PrimitiveSerializer.Int32.ReadFrom(input);
                Check.Assert<InvalidDataException>(ver == VersionHeader);

                int istate = PrimitiveSerializer.Int32.ReadFrom(input);
                Check.Assert<InvalidDataException>(Enum.IsDefined(typeof(TransferState), istate));
                state = (TransferState)istate;

                byte[] bytes = new byte[16];
                Check.Assert<InvalidDataException>(bytes.Length == input.Read(bytes, 0, bytes.Length));
                txid = new Guid(bytes);

                bytes = new byte[32];
                Check.Assert<InvalidDataException>(bytes.Length == input.Read(bytes, 0, bytes.Length));
                salt = Salt.FromBytes(bytes);
            }

            public void VerifySignature(RSAPublicKey signingKey)
            {
                // Next version's data-length
                int szBytes = ReadInt32();
                if(Check.InRange(szBytes, 0, short.MaxValue) > 0)
                    IOStream.Read(_hash, szBytes);

                Hash hash = _hash.FinalizeHash();
                byte[] signature = LimitedSerializer.Bytes2048.ReadFrom(_payload);

                Check.Assert<InvalidDataException>(signingKey.VerifyHash(signature, hash));
                //_verified = true;
            }

            #region Read(...)
            public int ReadInt32()
            {
                return PrimitiveSerializer.Int32.ReadFrom(_hash);
            }

            public long ReadInt64()
            {
                return PrimitiveSerializer.Int64.ReadFrom(_hash);
            }

            public string ReadString(int maxLength)
            {
                ISerializer<string> reader = maxLength > 1024
                                         ? new LimitedSerializer(maxLength)
                                         : LimitedSerializer.String1024;
                
                string result = reader.ReadFrom(_hash);
                Check.InRange(result.Length, 0, maxLength);
                return result;
            }

            public byte[] ReadBytes(int maxLength)
            {
                ISerializer<byte[]> reader = maxLength > 2048
                                         ? new LimitedSerializer(maxLength)
                                         : LimitedSerializer.Bytes2048;

                return Check.ArraySize(reader.ReadFrom(_hash), 0, maxLength);
            }
            #endregion
        }
    }
}

