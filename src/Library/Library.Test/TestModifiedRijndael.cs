#region Copyright 2010-2012 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Security.Cryptography;
using System.Text;
using CSharpTest.Net.Formatting;
using NUnit.Framework;
using CSharpTest.Net.Crypto;

namespace CSharpTest.Net.Library.Test
{
	[TestFixture]
	public class TestModifiedRijndael
	{
        
		IEnumerable<int> AllKeySizes(params KeySizes[] sizes)
		{
			foreach (KeySizes sz in sizes)
			{
				int inc = Math.Max(1, sz.SkipSize);
				for (int size = sz.MinSize; size <= sz.MaxSize; size += inc)
					yield return size;
			}
		}

		void TestEncryptDecrypt(SymmetricAlgorithm ealg, SymmetricAlgorithm dalg)
		{
			System.Diagnostics.Trace.WriteLine(String.Format("b={0}, k={1}, r={2}", ealg.BlockSize, ealg.KeySize, 
				ealg is ModifiedRijndael ? ((ModifiedRijndael)ealg).Rounds : ((ModifiedRijndael)dalg).Rounds));
			byte[] test = Encoding.ASCII.GetBytes("Testing 123");

			byte[] copy;
			using (ICryptoTransform enc = ealg.CreateEncryptor())
				copy = enc.TransformFinalBlock(test, 0, test.Length);

			using (ICryptoTransform dec = dalg.CreateDecryptor())
				copy = dec.TransformFinalBlock(copy, 0, copy.Length);

			Assert.AreEqual(0, BinaryComparer.Compare(test, copy));
		}

		[Test]
		public void TestCycleAll()
		{
			ModifiedRijndael r = new ModifiedRijndael();

			foreach (int blksz in AllKeySizes(r.LegalBlockSizes))
			{
				r.BlockSize = blksz;
				r.GenerateIV();
				Assert.AreEqual(blksz, r.IV.Length*8);

				foreach (int keysz in AllKeySizes(r.LegalKeySizes))
				{
					r.KeySize = keysz;
					r.GenerateKey();
					Assert.AreEqual(keysz, r.Key.Length*8);

					r.Rounds = r.NormalRounds;
					TestEncryptDecrypt(r, r);
					r.Rounds = r.MaxRounds;
					TestEncryptDecrypt(r, r);
				}
			}
		}

		[Test]
		public void EnsureCompatAes()
		{
			RijndaelManaged r = new RijndaelManaged();

			foreach (int blksz in AllKeySizes(r.LegalBlockSizes))
			{
				r.BlockSize = blksz;
				r.GenerateIV();

				foreach (int keysz in AllKeySizes(r.LegalKeySizes))
				{
					r.KeySize = keysz;
					r.GenerateKey();

                    ModifiedRijndael modified = ModifiedRijndael.Create(typeof(ModifiedRijndael).FullName);
					modified.BlockSize = r.BlockSize;
					modified.KeySize = r.KeySize;
					modified.IV = r.IV;
					modified.Key = r.Key;
					modified.Padding = r.Padding;
					modified.Mode = r.Mode;

					TestEncryptDecrypt(r, modified);
					TestEncryptDecrypt(modified, r);
				}
			}
		}

		private void TestAes256CBC(bool enc, string key, string iv, string input, string expect)
		{
			ModifiedRijndael r = ModifiedRijndael.Create();
			r.Mode = CipherMode.CBC;
			r.Padding = PaddingMode.None;
			r.BlockSize = iv.Length / 2 * 8;
            r.IV = HexEncoding.DecodeBytes(iv);
			r.KeySize = key.Length / 2 * 8;
            r.Key = HexEncoding.DecodeBytes(key);
			ICryptoTransform xf = enc ? r.CreateEncryptor() : r.CreateDecryptor();
            byte[] result = xf.TransformFinalBlock(HexEncoding.DecodeBytes(input), 0, input.Length / 2);
            byte[] test = HexEncoding.DecodeBytes(expect);
			Assert.AreEqual(0, BinaryComparer.Compare(test, result));
		}

		[Test]
		public void KatTestEncrypt()
		{
			TestAes256CBC(true, "c47b0294dbbbee0fec4757f22ffeee3587ca4730c3d33b691df38bab076bc558", "00000000000000000000000000000000", "00000000000000000000000000000000", "46f2fb342d6f0ab477476fc501242c5f");
			TestAes256CBC(true, "28d46cffa158533194214a91e712fc2b45b518076675affd910edeca5f41ac64", "00000000000000000000000000000000", "00000000000000000000000000000000", "4bf3b0a69aeb6657794f2901b1440ad4");
			TestAes256CBC(true, "c1cc358b449909a19436cfbb3f852ef8bcb5ed12ac7058325f56e6099aab1a1c", "00000000000000000000000000000000", "00000000000000000000000000000000", "352065272169abf9856843927d0674fd");
			TestAes256CBC(true, "984ca75f4ee8d706f46c2d98c0bf4a45f5b00d791c2dfeb191b5ed8e420fd627", "00000000000000000000000000000000", "00000000000000000000000000000000", "4307456a9e67813b452e15fa8fffe398");
			TestAes256CBC(true, "b43d08a447ac8609baadae4ff12918b9f68fc1653f1269222f123981ded7a92f", "00000000000000000000000000000000", "00000000000000000000000000000000", "4663446607354989477a5c6f0f007ef4");
			TestAes256CBC(true, "1d85a181b54cde51f0e098095b2962fdc93b51fe9b88602b3f54130bf76a5bd9", "00000000000000000000000000000000", "00000000000000000000000000000000", "531c2c38344578b84d50b3c917bbb6e1");
			TestAes256CBC(true, "dc0eba1f2232a7879ded34ed8428eeb8769b056bbaf8ad77cb65c3541430b4cf", "00000000000000000000000000000000", "00000000000000000000000000000000", "fc6aec906323480005c58e7e1ab004ad");
			TestAes256CBC(true, "f8be9ba615c5a952cabbca24f68f8593039624d524c816acda2c9183bd917cb9", "00000000000000000000000000000000", "00000000000000000000000000000000", "a3944b95ca0b52043584ef02151926a8");
			TestAes256CBC(true, "797f8b3d176dac5b7e34a2d539c4ef367a16f8635f6264737591c5c07bf57a3e", "00000000000000000000000000000000", "00000000000000000000000000000000", "a74289fe73a4c123ca189ea1e1b49ad5");
			TestAes256CBC(true, "6838d40caf927749c13f0329d331f448e202c73ef52c5f73a37ca635d4c47707", "00000000000000000000000000000000", "00000000000000000000000000000000", "b91d4ea4488644b56cf0812fa7fcf5fc");
			TestAes256CBC(true, "ccd1bc3c659cd3c59bc437484e3c5c724441da8d6e90ce556cd57d0752663bbc", "00000000000000000000000000000000", "00000000000000000000000000000000", "304f81ab61a80c2e743b94d5002a126b");
			TestAes256CBC(true, "13428b5e4c005e0636dd338405d173ab135dec2a25c22c5df0722d69dcc43887", "00000000000000000000000000000000", "00000000000000000000000000000000", "649a71545378c783e368c9ade7114f6c");
			TestAes256CBC(true, "07eb03a08d291d1b07408bf3512ab40c91097ac77461aad4bb859647f74f00ee", "00000000000000000000000000000000", "00000000000000000000000000000000", "47cb030da2ab051dfc6c4bf6910d12bb");
			TestAes256CBC(true, "90143ae20cd78c5d8ebdd6cb9dc1762427a96c78c639bccc41a61424564eafe1", "00000000000000000000000000000000", "00000000000000000000000000000000", "798c7c005dee432b2c8ea5dfa381ecc3");
			TestAes256CBC(true, "b7a5794d52737475d53d5a377200849be0260a67a2b22ced8bbef12882270d07", "00000000000000000000000000000000", "00000000000000000000000000000000", "637c31dc2591a07636f646b72daabbe7");
			TestAes256CBC(true, "fca02f3d5011cfc5c1e23165d413a049d4526a991827424d896fe3435e0bf68e", "00000000000000000000000000000000", "00000000000000000000000000000000", "179a49c712154bbffbe6e7a84a18e220");
		}

		[Test]
		public void KatTestDecrypt()
		{
			TestAes256CBC(false, "c47b0294dbbbee0fec4757f22ffeee3587ca4730c3d33b691df38bab076bc558", "00000000000000000000000000000000", "46f2fb342d6f0ab477476fc501242c5f", "00000000000000000000000000000000");
			TestAes256CBC(false, "28d46cffa158533194214a91e712fc2b45b518076675affd910edeca5f41ac64", "00000000000000000000000000000000", "4bf3b0a69aeb6657794f2901b1440ad4", "00000000000000000000000000000000");
			TestAes256CBC(false, "c1cc358b449909a19436cfbb3f852ef8bcb5ed12ac7058325f56e6099aab1a1c", "00000000000000000000000000000000", "352065272169abf9856843927d0674fd", "00000000000000000000000000000000");
			TestAes256CBC(false, "984ca75f4ee8d706f46c2d98c0bf4a45f5b00d791c2dfeb191b5ed8e420fd627", "00000000000000000000000000000000", "4307456a9e67813b452e15fa8fffe398", "00000000000000000000000000000000");
			TestAes256CBC(false, "b43d08a447ac8609baadae4ff12918b9f68fc1653f1269222f123981ded7a92f", "00000000000000000000000000000000", "4663446607354989477a5c6f0f007ef4", "00000000000000000000000000000000");
			TestAes256CBC(false, "1d85a181b54cde51f0e098095b2962fdc93b51fe9b88602b3f54130bf76a5bd9", "00000000000000000000000000000000", "531c2c38344578b84d50b3c917bbb6e1", "00000000000000000000000000000000");
			TestAes256CBC(false, "dc0eba1f2232a7879ded34ed8428eeb8769b056bbaf8ad77cb65c3541430b4cf", "00000000000000000000000000000000", "fc6aec906323480005c58e7e1ab004ad", "00000000000000000000000000000000");
			TestAes256CBC(false, "f8be9ba615c5a952cabbca24f68f8593039624d524c816acda2c9183bd917cb9", "00000000000000000000000000000000", "a3944b95ca0b52043584ef02151926a8", "00000000000000000000000000000000");
			TestAes256CBC(false, "797f8b3d176dac5b7e34a2d539c4ef367a16f8635f6264737591c5c07bf57a3e", "00000000000000000000000000000000", "a74289fe73a4c123ca189ea1e1b49ad5", "00000000000000000000000000000000");
			TestAes256CBC(false, "6838d40caf927749c13f0329d331f448e202c73ef52c5f73a37ca635d4c47707", "00000000000000000000000000000000", "b91d4ea4488644b56cf0812fa7fcf5fc", "00000000000000000000000000000000");
			TestAes256CBC(false, "ccd1bc3c659cd3c59bc437484e3c5c724441da8d6e90ce556cd57d0752663bbc", "00000000000000000000000000000000", "304f81ab61a80c2e743b94d5002a126b", "00000000000000000000000000000000");
			TestAes256CBC(false, "13428b5e4c005e0636dd338405d173ab135dec2a25c22c5df0722d69dcc43887", "00000000000000000000000000000000", "649a71545378c783e368c9ade7114f6c", "00000000000000000000000000000000");
			TestAes256CBC(false, "07eb03a08d291d1b07408bf3512ab40c91097ac77461aad4bb859647f74f00ee", "00000000000000000000000000000000", "47cb030da2ab051dfc6c4bf6910d12bb", "00000000000000000000000000000000");
			TestAes256CBC(false, "90143ae20cd78c5d8ebdd6cb9dc1762427a96c78c639bccc41a61424564eafe1", "00000000000000000000000000000000", "798c7c005dee432b2c8ea5dfa381ecc3", "00000000000000000000000000000000");
			TestAes256CBC(false, "b7a5794d52737475d53d5a377200849be0260a67a2b22ced8bbef12882270d07", "00000000000000000000000000000000", "637c31dc2591a07636f646b72daabbe7", "00000000000000000000000000000000");
			TestAes256CBC(false, "fca02f3d5011cfc5c1e23165d413a049d4526a991827424d896fe3435e0bf68e", "00000000000000000000000000000000", "179a49c712154bbffbe6e7a84a18e220", "00000000000000000000000000000000");
		}
	}
}
