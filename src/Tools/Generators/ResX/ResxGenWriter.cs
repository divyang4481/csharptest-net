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
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Resources.Tools;
using Microsoft.CSharp;

namespace CSharpTest.Net.Generators.ResX
{
	class ResxGenWriter
	{
		private static readonly CSharpCodeProvider Csharp = new CSharpCodeProvider();
		private readonly string _fileName;
		private readonly string _nameSpace;
		private readonly string _className;
		private readonly string _resxNameSpace;
        private readonly string _baseException;
		private readonly bool _public, _partial, _sealed;
		private readonly ResxGenItemLookup _items;
		private readonly List<ResXDataNode> _xnodes;

        public ResxGenWriter(string filename, string nameSpace, string resxNameSpace, bool asPublic, bool asPartial, bool asSealed, string className, string baseException)
		{
			_fileName = filename;
			_nameSpace = nameSpace;
			_resxNameSpace = resxNameSpace;
			_public = asPublic;
		    _partial = asPartial;
			_sealed = asSealed;
			_className = className;
            _baseException = baseException;

			//Environment.CurrentDirectory = Path.GetDirectoryName(_fileName);
			Parse(out _items, out _xnodes);
		}

		void Parse(out ResxGenItemLookup items, out List<ResXDataNode> xnodes)
		{
			items = new ResxGenItemLookup();
			xnodes = new List<ResXDataNode>();
			string relPath = Path.GetDirectoryName(_fileName);

			using (ResXResourceReader r = new ResXResourceReader(_fileName))
			{
				r.UseResXDataNodes = true;
				IDictionaryEnumerator enumerator = r.GetEnumerator();
				while (enumerator.MoveNext())
				{
					ResXDataNode node = (ResXDataNode)enumerator.Value;
					if( node.FileRef != null && !Path.IsPathRooted(node.FileRef.FileName))
						node = new ResXDataNode(node.Name, new ResXFileRef(Path.Combine(relPath, node.FileRef.FileName), node.FileRef.TypeName, node.FileRef.TextFileEncoding));

					string identifier = StronglyTypedResourceBuilder.VerifyResourceName(node.Name, Csharp);

					ResxGenItem item = new ResxGenItem(identifier, _nameSpace, node);
					if (!item.Hidden)
						xnodes.Add(node);
					if (!item.Ignored)
					{
						List<ResxGenItem> lst;
						if (!items.TryGetValue(item.ItemName, out lst))
							items.Add(item.ItemName, lst = new List<ResxGenItem>());

						lst.Add(item);
					}
				}
			}
		}

		public void Write(TextWriter output)
		{
			string resxCode = CreateResources();
			int lastBrace = resxCode.LastIndexOf('}') - 1;
			int nextLastBrace = resxCode.LastIndexOf('}', lastBrace) - 1;
            if(String.IsNullOrEmpty(_nameSpace))
                nextLastBrace = lastBrace;

			output.WriteLine(resxCode.Substring(0, nextLastBrace - 1));

			using (CsWriter code = new CsWriter())
			{
				//Add formatting methods
				code.Indent = 2;
				code.WriteLine();

				WriteFormatters(code);
				code.Indent--;
				code.WriteLine();
				code.WriteLine(resxCode.Substring(nextLastBrace, lastBrace - nextLastBrace));

				WriteExceptions(code);

				output.WriteLine(code.ToString());
			}

			output.WriteLine(resxCode.Substring(lastBrace));
		}

        int LineFromText(string text)
        {
            try
            {
                string content = File.ReadAllText(_fileName);
                int pos = content.IndexOf(text);
                if (pos > 0)
                    return content.Substring(0, pos).Split('\n').Length;
                return 0;
            }
            catch { return 0; }
        }

        public bool Test(TextWriter errors)
        {
            bool success = true;
            foreach (List<ResxGenItem> list in _items.Values)
            {
                foreach (ResxGenItem item in list)
                {
                    try
                    {
                        String.Format(item.Value, new object[item.Args.Count]);
                    }
                    catch(Exception err)
                    {
                        success = false;
                        errors.WriteLine("{0}({1}): error: {2} - {3}", _fileName, LineFromText(item.FullName), item.ItemName, err.Message);
                    }
                }
            }
            return success;
        }

		string CreateResources()
		{
			//Now we've loaded our own type data, we need to generate the resource accessors:
			string[] errors;

			Hashtable all = new Hashtable();
			foreach (ResXDataNode node in _xnodes)
				all[node.Name] = node;

			CodeCompileUnit unit = StronglyTypedResourceBuilder.Create(all,
				_className, _nameSpace, _resxNameSpace, Csharp, !_public, out errors);

			foreach (string error in errors)
				Console.Error.WriteLine("Warning: {0}", error);

			CodeGeneratorOptions options = new CodeGeneratorOptions();
			options.BlankLinesBetweenMembers = false;
			options.BracingStyle = "C";
			options.IndentString = "    ";

			using (StringWriter swCode = new StringWriter())
			{
				Csharp.GenerateCodeFromCompileUnit(unit, swCode, options);
				string result = swCode.ToString();

                if (_partial)
                    result = result.Replace(" class ", " partial class ");
			    return result;
			}
		}

		void WriteFormatters(CsWriter code)
		{
			if (_items.Count == 0)
				return;
			List<ResxGenItem> fmt = new List<ResxGenItem>(), exp = new List<ResxGenItem>();
			foreach (List<ResxGenItem> lst in _items.Values)
				foreach (ResxGenItem item in lst)
					if(item.IsException) exp.Add(item);
					else fmt.Add(item);

			if (fmt.Count > 0)
			{
				foreach (ResxGenItem item in fmt)
				{
					string procName = StronglyTypedResourceBuilder.VerifyResourceName(item.ItemName, Csharp);

					code.WriteSummaryXml(item.Value);
					using (code.WriteBlock("public static string {0}({1})", procName, item.Parameters(true)))
					{
						code.WriteLine("return String.Format(FormatStrings.{0}, {1});", item.Identifier, item.Parameters(false));
					}
				}

				code.WriteLine();
				code.WriteSummaryXml("Returns the raw format strings.");
				using (code.WriteBlock("public static {0}class FormatStrings", _partial ? "partial " : ""))
				{
					foreach (ResxGenItem item in fmt)
					{
						code.WriteSummaryXml(item.Value);
						code.WriteLine(
							"public static string {0} {{ get {{ return ResourceManager.GetString({1}, resourceCulture); }} }}",
							item.Identifier, code.MakeString(item.FullName));
					}
				}
			}
			if (exp.Count > 0)
			{
				code.WriteLine();
				code.WriteSummaryXml("Returns the raw exception strings.");
                using (code.WriteBlock("public static {0}class ExceptionStrings", _partial ? "partial " : ""))
				{
					foreach (ResxGenItem item in exp)
					{
						code.WriteSummaryXml(item.Value);
						code.WriteLine(
							"public static string {0} {{ get {{ return ResourceManager.GetString({1}, resourceCulture); }} }}",
							item.Identifier, code.MakeString(item.FullName));
					}
				}
			}
		}

		void WriteExceptions(CsWriter code)
		{
			foreach (List<ResxGenItem> lst in _items.Values)
			{
				ResxGenItem first = lst[0];
				if (!first.IsException)
					continue;
				string exName = StronglyTypedResourceBuilder.VerifyResourceName(first.ItemName, Csharp);

                string baseName = ": " + _baseException;
				foreach (ResxGenItem item in lst)
					if (item.Comments.StartsWith(":"))
						baseName = item.Comments;

				Assembly me = Assembly.GetExecutingAssembly() ?? typeof(Program).Assembly;

				code.WriteSummaryXml("Exception class: {0} {1}\r\n{2}", exName, baseName, first.Value);
				code.WriteLine("[global::System.SerializableAttribute()]");
				code.WriteLine("[global::System.Diagnostics.DebuggerStepThroughAttribute()]");
				code.WriteLine("[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]");
				code.WriteLine("[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]");
                code.WriteLine("[global::System.CodeDom.Compiler.GeneratedCodeAttribute(\"{0}\", \"{1}\")]", me.GetName().Name, me.GetName().Version);

				using (code.WriteBlock("public {2}{3}class {0} {1}", exName, baseName, _sealed ? "sealed " : "", _partial ? "partial " : ""))
				{
					code.WriteSummaryXml("Serialization constructor");
					code.WriteBlock("{1} {0}(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)",
                        exName, _sealed ? "internal" : "protected")
						.Dispose();

					Dictionary<string, ResxGenArgument> publicData = new Dictionary<string, ResxGenArgument>();
					foreach (ResxGenItem item in lst)
						foreach (ResxGenArgument arg in item.Args)
							if (arg.IsPublic)
								publicData[arg.Name] = arg;

					foreach (ResxGenArgument pd in publicData.Values)
					{
                        if (pd.Name == "HResult" || pd.Name == "HelpLink" || pd.Name == "Source")
                            continue;//uses base properties

						code.WriteLine();
						code.WriteSummaryXml("The {0} parameter passed to the constructor", pd.ParamName);
						code.WriteLine("public {1} {0} {{ get {{ if (Data[\"{0}\"] is {1}) return ({1})Data[\"{0}\"]; else return default({1}); }} }}", pd.Name, pd.Type);
					}
					code.WriteLine();

					foreach (ResxGenItem item in lst)
					{
						string formatNm = String.Format("global::{0}.{1}.ExceptionStrings.{2}", _nameSpace, _className, item.Identifier);
						string baseArgs = item.IsFormatter ? "String.Format({0}, {1})" : "{0}";
                        string argList = item.HasArguments ? ", " + item.Parameters(true) : "";

						code.WriteSummaryXml(item.Value);
						code.WriteLine("public {0}({1})", exName, item.Parameters(true));
						using (code.WriteBlock("\t: base({0})", String.Format(baseArgs, formatNm, item.Parameters(false))))
						{
							foreach (ResxGenArgument arg in item.Args)
                                WriteSetProperty(code, arg);
                        }
						code.WriteSummaryXml(item.Value);
                        code.WriteLine("public {0}({1}{2}Exception innerException)", exName, item.Parameters(true), item.HasArguments ? ", " : "");
						using (code.WriteBlock("\t: base({0}, innerException)", String.Format(baseArgs, formatNm, item.Parameters(false))))
						{
							foreach (ResxGenArgument arg in item.Args)
								WriteSetProperty(code, arg);
						}
						code.WriteSummaryXml("if(condition == false) throws {0}", item.Value);
						using (code.WriteBlock("public static void Assert(bool condition{0})", argList))
							code.WriteLine("if (!condition) throw new {0}({1});", exName, item.Parameters(false));
					}
				}
			}
		}

        public void WriteSetProperty(CsWriter code, ResxGenArgument arg)
        {
            if (arg.IsPublic)
            {
                if (arg.Name == "HResult" || arg.Name == "HelpLink" || arg.Name == "Source")
                    code.WriteLine("base.{0} = {1};", arg.Name, arg.ParamName);
                else
                    code.WriteLine("base.Data[\"{0}\"] = {1};", arg.Name, arg.ParamName);
            }
        }
	}
}
