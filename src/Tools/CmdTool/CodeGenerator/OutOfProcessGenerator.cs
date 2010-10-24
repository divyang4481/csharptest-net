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
using System.IO;
using CSharpTest.Net.CustomTool.XmlConfig;
using CSharpTest.Net.IO;
using CSharpTest.Net.Processes;
using CSharpTest.Net.Utils;

namespace CSharpTest.Net.CustomTool.CodeGenerator
{
	class OutOfProcessGenerator : ICodeGenerator
	{
        private readonly GeneratorConfig _config;
        public OutOfProcessGenerator(GeneratorConfig config)
        {
            _config = Check.NotNull(config); 
            Check.NotNull(config.Script);
        }

        public override string ToString()
        {
			if (!String.IsNullOrEmpty(_config.Script.Include))
				return _config.Script.Include;
            return _config.Script.Text.Trim();
        }

		public void Generate(IGeneratorArguments input)
		{
			//Couple of assertions about PowerShell
			if (_config.Script.Type == ScriptEngine.Language.PowerShell &&
			    (_config.StandardInput.Redirect || _config.Arguments.Length > 0))
				throw new ApplicationException(
					@"Currently PowerShell integration does not support input streams or arguments.
Primarily this is due to circumventing the script-signing requirements. By 
using the '-Command -' argument we avoid signing or setting ExecutionPolicy.");

			using (DebuggingOutput debug = new DebuggingOutput(_config.Debug, input.WriteLine))
			using (new SetCurrentDirectory(_config.BaseDirectory))
			{
				debug.WriteLine("Environment.CurrentDirectory = {0}", Environment.CurrentDirectory);
				input.ConfigDir = _config.BaseDirectory;

				//Inject arguments into the script
				string script = input.ReplaceVariables(Check.NotNull(_config.Script).Text.Trim());
				if (!String.IsNullOrEmpty(_config.Script.Include))
					script = File.ReadAllText(_config.Script.Include);

				StringWriter swOutput = new StringWriter();

				List<string> arguments = new List<string>();
				foreach (GeneratorArgument arg in _config.Arguments)
					arguments.Add(input.ReplaceVariables(arg.Text ?? String.Empty));

				debug.WriteLine("Prepared Script:{0}{1}{0}{2}{0}{1}",
					Environment.NewLine,
					"---------------------------------------------",
					script
				);

			    string lastErrorMessage = null;
				using (ScriptRunner runner = new ScriptRunner(_config.Script.Type, script))
				{
					runner.OutputReceived +=
						delegate(object o, ProcessOutputEventArgs args)
							{
								if (args.Error)
                                    input.WriteLine(lastErrorMessage = args.Data);
								else if (_config.StandardOut != null)
								{
									debug.WriteLine("std::out: {0}", args.Data);
									swOutput.WriteLine(args.Data);
								}
								else
									input.WriteLine(args.Data);
							};

					input.WriteLine("Executing {0} {1}", runner, ArgumentList.Join(arguments.ToArray()));

					runner.Start(arguments.ToArray());
					if (_config.StandardInput.Redirect)
					{
						debug.WriteLine("Writing std::in from {0}", input.InputPath);
						string contents = File.ReadAllText(input.InputPath);
						runner.StandardInput.Write(contents);
					}
					runner.StandardInput.Close();
					runner.WaitForExit();
					debug.WriteLine("Exited = {0}", runner.ExitCode);

				    if (_config.StandardOut != null)
				    {
					    string target = Path.ChangeExtension(input.InputPath, _config.StandardOut.Extension);
					    using (TempFile file = TempFile.FromExtension(_config.StandardOut.Extension))
					    {
						    file.WriteAllText(swOutput.ToString());
						    File.Copy(file.TempPath, target, true);
						    input.AddOutputFile(target);
					    }
				    }

					if (runner.ExitCode != 0)
					{
					    string message = "The script returned a non-zero result: " + runner.ExitCode;
                        input.WriteLine(message);
                        throw new ApplicationException(String.IsNullOrEmpty(lastErrorMessage) ? message : lastErrorMessage);
					}
				}

				EnumOutputFiles(input, input.AddOutputFile);
			}
		}

		public void EnumOutputFiles(IGeneratorArguments input, Action<string> outputFile)
		{
			if (_config.StandardOut != null)
				outputFile(Path.ChangeExtension(input.InputPath, _config.StandardOut.Extension));
			foreach (GeneratorOutput output in _config.Output)
				outputFile(Path.ChangeExtension(input.InputPath, output.Extension));
		}
	}
}
