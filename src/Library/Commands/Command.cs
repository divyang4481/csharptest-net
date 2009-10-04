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
using System.Reflection;
using CSharpTest.Net.Utils;
using System.Diagnostics;

namespace CSharpTest.Net.Commands
{
	[System.Diagnostics.DebuggerDisplay("{Method}")]
	partial class Command : DisplayInfoBase, ICommand
	{
		Dictionary<string, int> _names;
		Argument[] _arguments;

		public static ICommand Make(object target, MethodInfo mi)
		{
			ICommand cmd;
			if (CommandFilter.TryCreate(target, mi, out cmd))
				return cmd;
			return new Command(target, mi);
		}

		protected Command(object target, MethodInfo mi)
			: base(target, mi)
		{
			ParameterInfo[] paramList = mi.GetParameters();

			_names = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			List<Argument> tempList = new List<Argument>();

			foreach (ParameterInfo pi in paramList)
			{
				Argument arg = new Argument(target, pi);
				foreach(string name in arg.AllNames)
					_names.Add(name, tempList.Count);
				tempList.Add(arg);
			}
			_arguments = tempList.ToArray();
		}

		public IArgument[] Arguments { get { return (Argument[])_arguments.Clone(); } }

		private MethodInfo Method { get { return (MethodInfo)base.Member; } }

		public virtual void Run(ICommandInterpreter interpreter, string[] arguments)
		{
			ArgumentList args = new ArgumentList(arguments);

			//translate ordinal referenced names
			for (int i = 0; i < _arguments.Length && args.Unnamed.Count > 0; i++)
			{
				args.Add(_arguments[i].DisplayName, args.Unnamed[0]);
				args.Unnamed.RemoveAt(0);
			}

			List<object> invokeArgs = new List<object>();
			foreach (Argument arg in _arguments)
			{
				object argValue = arg.GetArgumentValue(interpreter, args, arguments);
				invokeArgs.Add(argValue);
			}

			//make sure we actually used all arguments.
			List<string> names = new List<string>(args.Keys);
			InterpreterException.Assert(names.Count == 0, "Unknown argument(s): {0}", String.Join(", ", names.ToArray()));
			InterpreterException.Assert(args.Unnamed.Count == 0, "Too many arguments supplied.");

			try
			{
				Method.Invoke(Target, invokeArgs.ToArray());
			}
			catch (TargetInvocationException te) 
			{
				Trace.TraceError(te.ToString());
				throw te.InnerException;
			}
		}
	}
}
