using System;
using System.Collections.Generic;
using System.Text;
using CSharpTest.Net.Bases;
using CSharpTest.Net.Collections;
using System.Security.Cryptography;
using System.IO;

namespace CSharpTest.Net.Crypto
{
    /// <summary> Represents a comparable, sortable, hash code </summary>
    public sealed class Hash : Comparable<Hash>
    {
        private static readonly byte[] Empty = new byte[0];

        private static Hash Create<T>(byte[] data)
            where T : HashAlgorithm, new()
        {
            using(T algo = new T())
                return new Hash(algo.ComputeHash(data == null ? Empty : data));
        }

        private static Hash Create<T>(Stream data)
            where T : HashAlgorithm, new()
        {
            using (data)
            using (T algo = new T())
                return new Hash(algo.ComputeHash(data == null ? Stream.Null : data));
        }

        /// <summary> Computes an MD5 hash </summary>
        public static Hash MD5(byte[] bytes) { return Create<MD5CryptoServiceProvider>(bytes); }
        /// <summary> Computes an MD5 hash </summary>
        public static Hash MD5(Stream bytes) { return Create<MD5CryptoServiceProvider>(bytes); }

        /// <summary> Computes an SHA1 hash </summary>
        public static Hash SHA1(byte[] bytes) { return Create<SHA1Managed>(bytes); }
        /// <summary> Computes an SHA1 hash </summary>
        public static Hash SHA1(Stream bytes) { return Create<SHA1Managed>(bytes); }

        /// <summary> Computes an SHA256 hash </summary>
        public static Hash SHA256(byte[] bytes) { return Create<SHA256Managed>(bytes); }
        /// <summary> Computes an SHA256 hash </summary>
        public static Hash SHA256(Stream bytes) { return Create<SHA256Managed>(bytes); }

        /// <summary> Computes an SHA384 hash </summary>
        public static Hash SHA384(byte[] bytes) { return Create<SHA384Managed>(bytes); }
        /// <summary> Computes an SHA384 hash </summary>
        public static Hash SHA384(Stream bytes) { return Create<SHA384Managed>(bytes); }

        /// <summary> Computes an SHA512 hash </summary>
        public static Hash SHA512(byte[] bytes) { return Create<SHA512Managed>(bytes); }
        /// <summary> Computes an SHA512 hash </summary>
        public static Hash SHA512(Stream bytes) { return Create<SHA512Managed>(bytes); }

        /// <summary> Creates a comparable Hash object from the given hashcode bytes </summary>
        public static Hash FromBytes(byte[] bytes) { return new Hash((byte[])bytes.Clone()); }
        /// <summary> Creates a comparable Hash object from the base-64 encoded hashcode bytes </summary>
        public static Hash FromString(string encodedBytes) { return new Hash(Convert.FromBase64String(encodedBytes)); }

        private readonly byte[] _hashCode;
        private Hash(byte[] hashCode)
        {
            int sz = Check.NotNull(hashCode).Length;
            Check.Assert<ArgumentOutOfRangeException>(sz == 16 || sz == 20 || sz == 32 || sz == 48 || sz == 64);
            _hashCode = hashCode;
        }

        /// <summary> Returns the OID of the hash algorithm </summary>
        public string AlgorithmOID
        { get { return CryptoConfig.MapNameToOID(AlgorithmName); } }

        /// <summary> Returns the name of the hash algorithm </summary>
        public string AlgorithmName
        {
            get
            {
                switch (_hashCode.Length)
                {
                    case 16: return ("MD5");
                    case 20: return ("SHA1");
                    case 32: return ("SHA256");
                    case 48: return ("SHA384");
                    case 64: return ("SHA512");
                    default: throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary> Returns the length in bytes of the hash code </summary>
        public int Length { get { return _hashCode.Length; } }

        /// <summary> Returns a copy of the hash code bytes </summary>
        public byte[] ToArray() { return (byte[])_hashCode.Clone(); }

        /// <summary> Returns the hash code as a base-64 encoded string </summary>
        public override string ToString()
        {
            return Convert.ToBase64String(_hashCode);
        }

        /// <summary> Compares the hash codes and returns the result </summary>
        public override int CompareTo(Hash other)
        {
            return other == null ? 1 : BinaryComparer.Compare(_hashCode, other._hashCode);
        }

        /// <summary> Returns a hash of the hash code :) </summary>
        protected override int HashCode
        {
            get { return BinaryComparer.GetHashCode(_hashCode); }
        }
    }
}
