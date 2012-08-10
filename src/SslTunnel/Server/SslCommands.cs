#region Copyright 2009 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using CSharpTest.Net.Utils;
using System.Reflection;
using System.ComponentModel;
using CSharpTest.Net.Logging;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using CSharpTest.Net.Commands;
using System.ServiceProcess;
using System.Text;
using System.Security.AccessControl;
using System.Security.Principal;
using CSharpTest.Net.Processes;

namespace CSharpTest.Net.SslTunnel.Server
{
    public static partial class Commands
    {
        [Command(Description = "Remove a certificate from the machine by name.", Visible = false)]
        public static void RemoveCert(
            [Argument(Description = "The qualified host name used to created the certificate.")]
			string name
            )
        {
            if (name.StartsWith("CN=") == false)
                name = String.Format("CN={0}", name);

            StringBuilder sbknown = new StringBuilder();

            X509Certificate2 found = null;
            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);
            try
            {
                foreach (X509Certificate2 cert in store.Certificates)
                {
                    if (cert.Subject == name)
                        found = cert;
                    sbknown.AppendLine(cert.Subject);
                }

                if (found != null)
                {
                    Console.WriteLine("Removing the following certificate:");
                    Console.WriteLine();
                    SslCertValidator.DebugDumpCertificate(found, Console.Out);
                    Console.WriteLine();

                    Console.WriteLine("Are you sure you want to delete this? [y/n]");
                    if (Constants.IsUnitTest || 'y' == Console.ReadKey(true).KeyChar)
                    {
                        store.Remove(found);
                        Console.WriteLine("Removed.");
                    }
                    else
                        Console.WriteLine("Aborted.");
                }
            }
            finally { store.Close(); }

            if (found == null)
            {
                Console.WriteLine("Unable to locate '{0}' in:", name);
                Console.WriteLine(sbknown.ToString());
            }
        }

        [Command(Description = "Creates a self-signed certificate for client or server authentication.")]
        public static void MakeCert(
            [Argument(Description = "The qualified host name of the machine to create the certificate for.")]
			string name
            )
        {
            name = name.Trim();
            //%makecert% -pe -n "CN=%1" -ss my -sr LocalMachine -cy end -h 0 -a sha1 -sky exchange -eku 1.3.6.1.5.5.7.3.1,1.3.6.1.5.5.7.3.2 -in "LocalSslRootAuthority" -is MY -ir LocalMachine -sp "Microsoft RSA SChannel Cryptographic Provider"  -sy 12 %1.cer
            byte[] data;
            using (BinaryReader r = new BinaryReader(typeof(Commands).Assembly.GetManifestResourceStream(typeof(Commands).Namespace + ".makecert.exe")))
                data = r.ReadBytes((int)r.BaseStream.Length);

            string makeCertPath = Path.Combine(Path.GetTempPath(), "makecert.exe");
            File.WriteAllBytes(makeCertPath, data);

            ProcessRunner runner = new ProcessRunner(
                makeCertPath,
                "-r",//					Create a self signed certificate
                "-pe",//				Mark generated private key as exportable
                "-n", "CN={0}",//		Certificate subject X500 name (eg: CN=Fred Dews)
				"-len", "2048",//		Generated Key Length (Bits)
				"-a", "sha1",//			The signature algorithm <md5|sha1>.  Default to 'md5'
                "-b", "01/01/2000",//	Start of the validity period; default to now.
                "-e", "01/01/2036",//	End of validity period; defaults to 2039
                "-eku",//				Comma separated enhanced key usage OIDs
                "1.3.6.1.5.5.7.3.1," +//Server Authentication (1.3.6.1.5.5.7.3.1)
                "1.3.6.1.5.5.7.3.2", // Client Authentication (1.3.6.1.5.5.7.3.2)
                "-ss", "my",//			Subject's certificate store name that stores the output certificate
                "-sr", "LocalMachine",//Subject's certificate store location.
                "-sky", "exchange",//	Subject key type <signature|exchange|<integer>>.
                "-sp",//				Subject's CryptoAPI provider's name
                "Microsoft RSA SChannel Cryptographic Provider",
                "-sy", "12",//			Subject's CryptoAPI provider's type
                "{1}"//					[outputCertificateFile]
                );

            StringWriter swOut = new StringWriter();
            runner.OutputReceived += delegate(object o, ProcessOutputEventArgs e)
            {
                swOut.WriteLine(e.Data);
            };
            string certFile = String.Format("{0}.cer", name.TrimStart('*', '.'));

            if (0 != runner.RunFormatArgs(name, certFile))
                throw new ApplicationException(String.Format("makecert.exe failed to create the certificate:{0}{1}", Environment.NewLine, swOut));

            DumpCert(certFile, null);

            Log.Verbose("Finding private key file for certificate: {0}", certFile);
            //always grants network service the right to this key
            string fqpath = CertUtils.GetKeyFileName(new X509Certificate2(certFile));
            Log.Info("Granting NETWORK SERVICE full control on: {0}", fqpath);
            FileUtils.GrantFullControlForFile(fqpath, WellKnownSidType.NetworkServiceSid);

            Console.WriteLine("Key Access:");
            FileSecurity fsec = new FileSecurity(fqpath, AccessControlSections.Access);
            foreach (FileSystemAccessRule r in fsec.GetAccessRules(true, false, typeof(NTAccount)))
                Console.WriteLine("  {0,6}  {1,32}  {2}", r.AccessControlType, r.IdentityReference.Value, r.FileSystemRights);
        }

        [Command(Description = "Dumps the details of the certificate file specified to the console.")]
        public static void DumpCert(
            [Argument(Description = "The path to the certificate file to inspect.")]
			string file,
            [Argument(Description = "Optional password for the certificate file.", DefaultValue = null)]
			string password)
        {
            file = Path.GetFullPath(file);
            Console.WriteLine("File Path = {0}", file);

            X509Certificate2 cert = new X509Certificate2(file, password);
            SslCertValidator.DebugDumpCertificate(new X509Certificate(file, password), Console.Out);
        }
    }
}
