#region Copyright 2010-2011 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using CSharpTest.Net.Utils;
using CSharpTest.Net.Logging;
using System.Security.Cryptography.X509Certificates;
using CSharpTest.Net.Commands;
using System.IO;
using System.Reflection;

namespace CSharpTest.Net.SslTunnel.Server
{
    static class SslCertValidator
    {
        public static void DebugDumpCertificate(X509Certificate certificate, TextWriter sw)
        {
            if (certificate != null)
            {
                sw.WriteLine("Issuer = {0}", certificate.Issuer);
                sw.WriteLine("Subject = {0}", certificate.Subject);
                sw.WriteLine("SerialNumber = {0}", certificate.GetSerialNumberString());
                sw.WriteLine("CertHash = {0}", certificate.GetCertHashString());
                sw.WriteLine("EffectiveDate = {0}", certificate.GetEffectiveDateString());
                sw.WriteLine("ExpirationDate = {0}", certificate.GetExpirationDateString());
                sw.WriteLine("Format = {0}", certificate.GetFormat());
                sw.WriteLine("KeyAlgorithm = {0}", certificate.GetKeyAlgorithm());
                sw.WriteLine("KeyParameters = {0}", certificate.GetKeyAlgorithmParametersString());
                sw.WriteLine("PublicKey = {0}", certificate.GetPublicKeyString());
                //sw.WriteLine("RawCert = {0}", certificate.GetRawCertDataString());
            }
            else
                sw.WriteLine("No certificate available.");
        }
    }
	static class Program
	{
        [MTAThread]
        static int Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
            return new RunSslCert().Run(args);
        }

        static System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string name = args.Name;
            if (name.IndexOf(',') > 0)
                name = name.Substring(0, name.IndexOf(','));
            name = String.Format("{0}.{1}.dll", typeof(Commands).Namespace, name);
            using (BinaryReader r = new BinaryReader(typeof(Commands).Assembly.GetManifestResourceStream(name)))
                return Assembly.Load(r.ReadBytes((int)r.BaseStream.Length));
        }
	}

    class RunSslCert
    {
        public int Run(string[] args)
        {
			Log.Config.Output = LogOutputs.LogFile;
			Log.Config.Level = LogLevels.Info;

			string ignore;
			bool nologo = ArgumentList.Remove(ref args, "nologo", out ignore);

			using (Log.AppStart(Environment.CommandLine))
			{
				if (nologo == false)
				{
					Console.WriteLine("SslTunnel.Server.exe");
					Console.WriteLine("Copyright 2009 by Roger Knapp, Licensed under the Apache License, Version 2.0");
					Console.WriteLine("");
				}

				CommandInterpreter ci = new CommandInterpreter(DefaultCommands.Help, typeof(Commands));
				ci.Run(args);
			}

			return Environment.ExitCode;
		}
    }
}
