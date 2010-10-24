#region Copyright 2009-2010 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Security.Cryptography.X509Certificates;
using System.Runtime.InteropServices;
using System.IO;

namespace CSharpTest.Net.SslTunnel.Server
{
	class CertUtils
	{
		internal static string GetKeyFileName(X509Certificate2 cert)
		{
			IntPtr hProvider = IntPtr.Zero;
			bool freeProvider = false;
			uint acquireFlags = 0;
			int _keyNumber = 0;
			string keyFileName = null;
			byte[] keyFileBytes = null;

			if (CryptAcquireCertificatePrivateKey(cert.Handle, acquireFlags, IntPtr.Zero, ref hProvider, ref _keyNumber, ref freeProvider))
			{
				IntPtr pBytes = IntPtr.Zero;
				int cbBytes = 0;
				try
				{
					if (CryptGetProvParam(hProvider, PP_UNIQUE_CONTAINER, IntPtr.Zero, ref cbBytes, 0))
					{
						pBytes = Marshal.AllocHGlobal(cbBytes);
						if (CryptGetProvParam(hProvider, PP_UNIQUE_CONTAINER, pBytes, ref cbBytes, 0))
						{
							keyFileBytes = new byte[cbBytes];
							Marshal.Copy(pBytes, keyFileBytes, 0, cbBytes);
							keyFileName = System.Text.Encoding.ASCII.GetString(keyFileBytes, 0, keyFileBytes.Length - 1);
						}
					}
				}
				finally
				{
					if (freeProvider)
						CryptReleaseContext(hProvider, 0);
					if (pBytes != IntPtr.Zero)
						Marshal.FreeHGlobal(pBytes);
				}
			}

			if (keyFileName == null)
				throw new FileNotFoundException("Private key not found.");

			return GetKeyFilePath(keyFileName);
		}

		private static string GetKeyFilePath(string keyFileName)
		{
			//Machine key storage location...
			string allUserProfile = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
			string machineKeyDir = Path.Combine(allUserProfile, @"Microsoft\Crypto\RSA\MachineKeys");
			string[] filenames = Directory.GetFiles(machineKeyDir, keyFileName);

			foreach (string file in filenames)
				return file;

			//User key storage location...
			//string currUserProfile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			//string userKeyDir = Path.Combine(currUserProfile, @"Microsoft\Crypto\RSA");

			//filenames = Directory.GetFiles(userKeyDir, keyFileName, SearchOption.AllDirectories);
			//foreach (string file in filenames)
			//    return file;

			throw new FileNotFoundException("Private key not found.", keyFileName);
		}

		const int PP_UNIQUE_CONTAINER = 36;

		[DllImport("crypt32", CharSet = CharSet.Unicode, SetLastError = true)]
		private extern static bool CryptAcquireCertificatePrivateKey(IntPtr pCert, uint dwFlags, IntPtr pvReserved, ref IntPtr phCryptProv, ref int pdwKeySpec, ref bool pfCallerFreeProv);

		[DllImport("advapi32", CharSet = CharSet.Unicode, SetLastError = true)]
		private extern static bool CryptGetProvParam(IntPtr hCryptProv, int dwParam, IntPtr pvData, ref int pcbData, uint dwFlags);

		[DllImport("advapi32", SetLastError = true)]
		private extern static bool CryptReleaseContext(IntPtr hProv, uint dwFlags);
	}
}