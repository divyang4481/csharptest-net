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
using System.IO;
using CSharpTest.Net.Html;
using System.Net;
using CSharpTest.Net.Collections;
using CSharpTest.Net.IO;

namespace CSharpTest.Net.XhtmlValidate
{
    static class Program
    {
        static int DoHelp()
        {
            Console.WriteLine("");
            Console.WriteLine("Usage:");
            Console.WriteLine("    XhtmlValidate.exe file1.htm [file2*.*] [/nologo] [/wait]");
            Console.WriteLine("");
            Console.WriteLine("        file1.htm One or more file specifications");
            Console.WriteLine("");
            Console.WriteLine("        /nologo Hide the startup message");
            Console.WriteLine("");
            Console.WriteLine("        /wait after processing wait for user input");
            return 0;
        }

        [STAThread]
        static int Main(string[] raw)
        {
            ArgumentList args = new ArgumentList(raw);

            using (DisposingList dispose = new DisposingList())
            using (Log.AppStart(Environment.CommandLine))
            {
                if (args.Contains("nologo") == false)
                {
                    Console.WriteLine("XhtmlValidate.exe");
                    Console.WriteLine("Copyright 2010 by Roger Knapp, Licensed under the Apache License, Version 2.0");
                    Console.WriteLine("");
                }

                if ((args.Unnamed.Count == 0) || args.Contains("?") || args.Contains("help"))
                    return DoHelp();

                try
                {
                    FileList files = new FileList();
                    files.RecurseFolders = true;
                    foreach (string spec in args.Unnamed)
                    {
                        Uri uri;
                        if (Uri.TryCreate(spec, UriKind.Absolute, out uri) && !(uri.IsFile || uri.IsUnc))
                        {
                            using(WebClient wc = new WebClient())
                            {
                                TempFile tfile = new TempFile();
                                dispose.Add(tfile);
                                wc.DownloadFile(uri, tfile.TempPath);
                                files.Add(tfile.Info);
                            }
                        }
                        else
                            files.Add(spec);
                    }
                    if( files.Count == 0 )
                        return 1 + DoHelp();

                    XhtmlValidation validator = new XhtmlValidation(XhtmlDTDSpecification.Any);
                    foreach (FileInfo f in files)
                        validator.Validate(f.FullName);
                }
                catch (ApplicationException ae)
                {
                    Log.Error(ae);
                    Console.Error.WriteLine();
                    Console.Error.WriteLine(ae.Message);
                    Environment.ExitCode = -1;
                }
                catch (Exception e)
                {
                    Log.Error(e);
                    Console.Error.WriteLine();
                    Console.Error.WriteLine(e.ToString());
                    Environment.ExitCode = -1;
                }
            }

            if (args.Contains("wait"))
            {
                Console.WriteLine();
                Console.WriteLine("Press [Enter] to continue...");
                Console.ReadLine();
            }

            return Environment.ExitCode;
        }
    }
}
