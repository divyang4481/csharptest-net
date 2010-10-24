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
using System.Security.Cryptography;
using CSharpTest.Net.Delegates;
using CSharpTest.Net.Reflection;

namespace CSharpTest.Net.Crypto
{
	/// <summary>
	/// This class is provided to essentially unlock the Rijndael algorithm from the constraints imposed by the AES standards.
	/// The Rijndael algorithm here supports a wider length of key sizes and allows users to explicitly set the number of
	/// rounds.  This class is fully AES compliant when used with key sizes of 16, 24, or 32 bytes with rounds of 10, 12, and 
	/// 14 respectivly.
	/// </summary>
	public class ModifiedRijndael : SymmetricAlgorithm
	{
		static readonly RandomNumberGenerator StaticRandomNumberGenerator = new RNGCryptoServiceProvider();
		int _numRounds;

		/// <summary>
		/// Creates a ModifiedRijndael which can be used with larger key sizes and a specified number of rounds.
		/// </summary>
		public ModifiedRijndael()
		{
			_numRounds = -1; //defined by keysize
			PaddingValue = PaddingMode.PKCS7;
			ModeValue = CipherMode.CBC;
			KeySizeValue = 256;
			FeedbackSizeValue = BlockSizeValue = 128;
			LegalBlockSizesValue = new KeySizes[] { new KeySizes(128, 256, 64) };
			LegalKeySizesValue = new KeySizes[] { new KeySizes(128, 4096, 64) };
		}

		/// <summary> return a new instance </summary>
		new static public ModifiedRijndael Create()
		{ return new ModifiedRijndael(); }

		/// <summary> return a new instance or throws ArugmentException </summary>
		new static public ModifiedRijndael Create(String algName)
		{ Check.IsEqual(algName, typeof(ModifiedRijndael).FullName); return new ModifiedRijndael(); }

		/// <summary>
		/// When overridden in a derived class, generates a random key (<see cref="P:System.Security.Cryptography.SymmetricAlgorithm.Key"/>) to use for the algorithm.
		/// </summary>
		public override void GenerateKey()
		{
			KeyValue = new byte[KeySizeValue / 8];
			StaticRandomNumberGenerator.GetBytes(KeyValue);
		}

		/// <summary>
		/// When overridden in a derived class, generates a random initialization vector (<see cref="P:System.Security.Cryptography.SymmetricAlgorithm.IV"/>) to use for the algorithm.
		/// </summary>
		public override void GenerateIV()
		{
			IVValue = new byte[BlockSizeValue / 8];
			StaticRandomNumberGenerator.GetBytes(IVValue);
		}

		/// <summary>
		/// Gets or sets the number of rounds the encryption algorithm will use when encrypting/decrypting data.
		/// </summary>
		public int Rounds
		{
			get { return _numRounds > 0 ? _numRounds : NormalRounds; }
			set { _numRounds = Check.InRange(value, 1, MaxRounds); }
		}

		/// <summary>
		/// Returns the Maximum value for Rounds given the current BlockSize and KeySize values
		/// </summary>
		public int MaxRounds { get { return ((30 * (KeySizeValue / 32) / (BlockSizeValue / 32)) - 1); } }

		/// <summary>
		/// Returns the AES standard round numbers for keys of 128, 192, and 256 bit, or provides a 
		/// rough 1/3 of MaxRounds for other key sizes based on a constant block size of 128 bit.
		/// </summary>
		public int NormalRounds 
		{
			get 
			{
				int maxbits = Math.Max(KeySizeValue, BlockSizeValue);
				return maxbits == 128 ? 10 : maxbits == 192 ? 12 : maxbits == 256 ? 14 : Math.Max(14, Math.Min(MaxRounds, (maxbits / 64 * 5))); 
			}
		}

		/// <summary>
		/// When overridden in a derived class, creates a symmetric encryptor object with the specified <see cref="P:System.Security.Cryptography.SymmetricAlgorithm.Key"/> property and initialization vector (<see cref="P:System.Security.Cryptography.SymmetricAlgorithm.IV"/>).
		/// </summary>
		public override ICryptoTransform CreateEncryptor(byte[] keybytes, byte[] iv)
		{
			return ModifyTransform(keybytes, iv, delegate(SymmetricAlgorithm sa) { return sa.CreateEncryptor(); });
		}

		/// <summary>
		/// When overridden in a derived class, creates a symmetric decryptor object with the specified <see cref="P:System.Security.Cryptography.SymmetricAlgorithm.Key"/> property and initialization vector (<see cref="P:System.Security.Cryptography.SymmetricAlgorithm.IV"/>).
		/// </summary>
		public override ICryptoTransform CreateDecryptor(byte[] keybytes, byte[] iv)
		{
			return ModifyTransform(keybytes, iv, delegate(SymmetricAlgorithm sa) { return sa.CreateDecryptor(); });
		}

		private ICryptoTransform ModifyTransform(byte[] keybytes, byte[] iv, Func<SymmetricAlgorithm, ICryptoTransform> open)
		{
			ICryptoTransform xform;
			using(RijndaelManaged algo = new RijndaelManaged())
			{
				algo.BlockSize = BlockSize;
				algo.FeedbackSize = FeedbackSize;
				algo.Mode = Mode;
				algo.Padding = Padding;
				algo.IV = iv;
				algo.Key = keybytes.Length <= 32 ? keybytes : new SHA256Managed().ComputeHash(keybytes);
				xform = open(algo);
			}

			int ikeylen = keybytes.Length / 4;
			int[] encryptKeyExpansion, decryptKeyExpansion;
			GenerateKeyExpansion(BlockSizeValue / 32, Check.InRange(Rounds, 1, MaxRounds), ikeylen, keybytes, out encryptKeyExpansion, out decryptKeyExpansion);

			new PropertyValue<int>(xform, "m_Nk").Value = ikeylen;
			new PropertyValue<int>(xform, "m_Nr").Value = Rounds;
			new PropertyValue<int[]>(xform, "m_encryptKeyExpansion").Value = encryptKeyExpansion;
			new PropertyValue<int[]>(xform, "m_decryptKeyExpansion").Value = decryptKeyExpansion;
			return xform;
		}

		private static void GenerateKeyExpansion(int iblocksize, int rounds, int ikeylen, byte[] keybytes, out int[] m_encryptKeyExpansion, out int[] m_decryptKeyExpansion)
		{
			m_encryptKeyExpansion = new int[iblocksize * (rounds + 1)];

			for (int i = 0; i < keybytes.Length; i += 4)
				m_encryptKeyExpansion[i / 4] = keybytes[i] | keybytes[i+1] << 8 | keybytes[i+2] << 16 | keybytes[i+3] << 24;

			for (int i = ikeylen; i < iblocksize * (rounds + 1); ++i)
			{
				int dword = m_encryptKeyExpansion[i - 1];
				int keymod = i % ikeylen;
				if (keymod == 0)
					dword = SubBox(RotateBy3(dword)) ^ RCon[(i / ikeylen) - 1];
				else if (keymod == 4 && ikeylen > 6)
					dword = SubBox(dword);

				m_encryptKeyExpansion[i] = m_encryptKeyExpansion[i - ikeylen] ^ dword;
			}

			m_decryptKeyExpansion = (int[])m_encryptKeyExpansion.Clone();
			for (int i = iblocksize; i < iblocksize * rounds; ++i)
			{
				int m1 = MulX(m_encryptKeyExpansion[i]);
				int m2 = MulX(m1);
				int m3 = MulX(m2);
				int m4 = m_encryptKeyExpansion[i] ^ m3;
				m_decryptKeyExpansion[i] = m1 ^ m2 ^ m3 ^ RotateBy3(m1 ^ m4) ^ RotateBy2(m2 ^ m4) ^ RotateBy1(m4);
			}
		}

		static int RotateBy1(int val) { return ((val << 8) & ~0x000000FF) | ((val >> 24) & 0x000000FF); }
		static int RotateBy2(int val) { return ((val << 16) & ~0x0000FFFF) | ((val >> 16) & 0x0000FFFF); }
		static int RotateBy3(int val) { return ((val << 24) & ~0x00FFFFFF) | ((val >> 8) & 0x00FFFFFF); }
		static int SubBox(int a) { return SBox[a & 0xFF] | SBox[a >> 8 & 0xFF] << 8 | SBox[a >> 16 & 0xFF] << 16 | SBox[a >> 24 & 0xFF] << 24; }

		private static int MulX(int x)
		{
			unchecked
			{
				int u = x & (int)0x80808080;
				return ((x & 0x7f7f7f7f) << 1) ^ ((u - (u >> 7 & 0x01FFFFFF)) & 0x1b1b1b1b);
			}
		}

		static readonly byte[] SBox = new byte[] 
		{
             99, 124, 119, 123, 242, 107, 111, 197,  48,   1, 103,  43, 254, 215, 171, 118,
            202, 130, 201, 125, 250,  89,  71, 240, 173, 212, 162, 175, 156, 164, 114, 192, 
            183, 253, 147,  38,  54,  63, 247, 204,  52, 165, 229, 241, 113, 216,  49,  21,
              4, 199,  35, 195,  24, 150,   5, 154,   7,  18, 128, 226, 235,  39, 178, 117, 
              9, 131,  44,  26,  27, 110,  90, 160,  82,  59, 214, 179,  41, 227,  47, 132, 
             83, 209,   0, 237,  32, 252, 177,  91, 106, 203, 190,  57,  74,  76,  88, 207,
            208, 239, 170, 251,  67,  77,  51, 133,  69, 249,   2, 127,  80,  60, 159, 168, 
             81, 163,  64, 143, 146, 157,  56, 245, 188, 182, 218,  33,  16, 255, 243, 210,
            205,  12,  19, 236,  95, 151,  68,  23, 196, 167, 126,  61, 100,  93,  25, 115,
             96, 129,  79, 220,  34,  42, 144, 136,  70, 238, 184,  20, 222,  94,  11, 219,
            224,  50,  58,  10,  73,   6,  36,  92, 194, 211, 172,  98, 145, 149, 228, 121, 
            231, 200,  55, 109, 141, 213,  78, 169, 108,  86, 244, 234, 101, 122, 174,   8,
            186, 120,  37,  46,  28, 166, 180, 198, 232, 221, 116,  31,  75, 189, 139, 138, 
            112,  62, 181, 102,  72,   3, 246,  14,  97,  53,  87, 185, 134, 193,  29, 158, 
            225, 248, 152,  17, 105, 217, 142, 148, 155,  30, 135, 233, 206,  85,  40, 223,
            140, 161, 137,  13, 191, 230,  66, 104,  65, 153,  45,  15, 176,  84, 187,  22 
		};

		static readonly int[] RCon = new int[] 
		{
            0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80, 0x1b, 0x36,
            0x6c, 0xd8, 0xab, 0x4d, 0x9a, 0x2f, 0x5e, 0xbc, 0x63, 0xc6, 
            0x97, 0x35, 0x6a, 0xd4, 0xb3, 0x7d, 0xfa, 0xef, 0xc5, 0x91 
		};
	}
}
