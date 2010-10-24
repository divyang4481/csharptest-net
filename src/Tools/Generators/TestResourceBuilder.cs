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
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Resources;
using CSharpTest.Net.IO;
using Microsoft.CSharp;

namespace CSharpTest.Net.Generators.Test
{
    class TestResourceBuilder
    {
        struct Item { public string Name, Value, Comment; }
        private readonly List<Item> _items = new List<Item>();
        public TestResourceBuilder(string nameSpace, string className)
        {
            ResxNamespace = Namespace = nameSpace;
            ClassName = className;
        }

        public readonly string Namespace;
        public readonly string ClassName;
        public string ResxNamespace;
        public bool Public = true;
        public bool Partial = true;
        public bool Test = true;

        public void Add(string name, string value) { Add(name, value, ""); }
        public void Add(string name, string value, string comment)
        {
            Item i = new Item();
            i.Name = name;
            i.Value = value;
            i.Comment = comment;
            _items.Add(i);
        }

        public TestResourceResult Compile()
        {
            using (TempFile resx = TempFile.FromExtension(".resx"))
            using (TempFile resources = TempFile.Attach(Path.ChangeExtension(resx.TempPath, ".resources")))
            using (TempFile rescs = TempFile.Attach(Path.ChangeExtension(resx.TempPath, ".Designer.cs")))
            {
                using (ResourceWriter w1 = new ResourceWriter(resources.TempPath))
                using (ResXResourceWriter w2 = new ResXResourceWriter(resx.TempPath))
                {
                    foreach (Item item in _items)
                    {
                        w1.AddResource(item.Name, item.Value);

                        ResXDataNode node = new ResXDataNode(item.Name, item.Value);
                        node.Comment = item.Comment;
                        w2.AddResource(item.Name, node);
                    }
                }

                TextWriter stdout = Console.Out;
                try
                {
                    using (TextWriter tw = new StreamWriter(rescs.Open()))
                    {
                        Console.SetOut(tw);
                        Commands.ResX(resx.TempPath, Namespace, ClassName, ResxNamespace, Public, Partial, Test);
                    }
                }
                finally
                { Console.SetOut(stdout); }

                Assembly asm = Compile(
                    String.Format("/reference:{0} /resource:{1},{2}.{3}.resources", GetType().Assembly.Location, resources.TempPath, ResxNamespace, ClassName),
                    rescs.TempPath);

                return new TestResourceResult(asm, Namespace, ClassName);
            }
        }

        private static Assembly Compile(string options, params string[] files)
        {
            using (TempFile asm = TempFile.FromExtension(".dll"))
            {
                asm.Delete();

                CSharpCodeProvider csc = new CSharpCodeProvider();
                CompilerParameters args = new CompilerParameters();
                args.GenerateExecutable = false;
                args.IncludeDebugInformation = false;
                args.ReferencedAssemblies.Add("System.dll");
                args.OutputAssembly = asm.TempPath;
                args.CompilerOptions = options;
                CompilerResults results = csc.CompileAssemblyFromFile(args, files);

                StringWriter sw = new StringWriter();
                foreach (CompilerError ce in results.Errors)
                {
                    if (ce.IsWarning) continue;
                    string msg = String.Format("{0}({1},{2}: error {3}: {4}", ce.FileName, ce.Line, ce.Column,
                                               ce.ErrorNumber, ce.ErrorText);
                    System.Diagnostics.Trace.WriteLine(msg);
                    sw.WriteLine(msg);
                }
                string errorText = sw.ToString();
                if (errorText.Length > 0)
                    throw new ApplicationException(errorText);

                if (!asm.Exists)
                    throw new FileNotFoundException(new FileNotFoundException().Message, asm.TempPath);

                return Assembly.Load(asm.ReadAllBytes());
            }
        }
    }
}