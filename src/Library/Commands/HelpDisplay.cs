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
using System.Text;

namespace CSharpTest.Net.Commands
{
	partial class CommandInterpreter
	{
		/// <summary> Display the Help text to Console.Out </summary>
		[Command("Help", "-?", "/?", "?", Category = "Built-in", Description = "Gets the help for a specific command or lists available commands.")]
		public void Help(
			[Argument("name", "command", "c", "option", "o", Description = "The name of the command or option to show help for.", DefaultValue = null)] 
			string name
			)
		{
			ICommand cmd;
			IOption opt;
			if (name != null && _commands.TryGetValue(name, out cmd))
				cmd.Help();
			else if (name != null && _options.TryGetValue(name, out opt))
				opt.Help();
			else
			{
				List<IDisplayInfo> list = new List<IDisplayInfo>(Options);
				list.AddRange(Commands);
				ShowHelp(list.ToArray());
			}
		}

		private void ShowHelp(IDisplayInfo[] items)
		{
			Dictionary<string, List<IDisplayInfo>> found = new Dictionary<string, List<IDisplayInfo>>(StringComparer.OrdinalIgnoreCase);
			foreach (IDisplayInfo item in items)
			{
				if (!item.Visible)
					continue;

				List<IDisplayInfo> list;
				string group = item is Option ? "Options" : "Commands"/*item.Category*/;

				if (!found.TryGetValue(group, out list))
					found.Add(group, list = new List<IDisplayInfo>());
				if (!list.Contains(item))
					list.Add(item);
			}

			string fmt = "  {0,8}:  {1}";

			List<string> categories = new List<string>(found.Keys);
			categories.Sort();
			foreach (string cat in categories)
			{
				Console.Out.WriteLine("{0}:", cat);
				found[cat].Sort(new OrderByName<IDisplayInfo>());
				foreach (IDisplayInfo info in found[cat])
					Console.Out.WriteLine(fmt, info.DisplayName.ToUpper(), info.Description);
				Console.WriteLine();
			}
		}
	}

	partial class Command
	{
		public void Help()
		{
			Console.WriteLine();
			foreach (string name in this.AllNames)
			{
				Console.Write("{0} ", name.ToUpper());
				bool nameRequred = false;
				foreach (Argument arg in Arguments)
				{
					nameRequred |= arg.IsInterpreter | arg.IsAllArguments;
					if (arg.IsInterpreter)
						continue;
					if (arg.IsAllArguments)
					{
						Console.Write("[argument1] [argument2] [etc]"); 
						continue;
					}

					Console.Write("{0} ", arg.FormatSyntax(arg.DisplayName));
				}

				Console.WriteLine();
			}

			//Console.WriteLine();
			//Console.WriteLine("Category: {0}", this.Category);
			//Console.WriteLine("Type: {0}", this.target);
			//Console.WriteLine("Prototype: {0}", this.method);
			Console.WriteLine();
			Console.WriteLine(this.Description);
			Console.WriteLine();

			bool startedArgs = false;
			foreach (Argument arg in Arguments)
			{
				if (arg.IsInterpreter | arg.IsAllArguments)
					continue;
				if (!startedArgs)
				{
					Console.WriteLine("Arguments:");
					Console.WriteLine();
					startedArgs = true;
				}
				arg.Help();
			}
		}
	}

	partial class Argument
	{
		public string FormatSyntax(string name)
		{
			StringBuilder sb = new StringBuilder();
			if (!Required) sb.Append('[');
			if (!IsFlag) sb.Append('[');
			sb.Append('/');
			sb.Append(name);
			if (!IsFlag) sb.AppendFormat("=]{0}", Type.Name);
			if (!Required) sb.Append(']');
			return sb.ToString();
		}

		public void Help()
		{
			Console.Write("  {0}", FormatSyntax(DisplayName));

			if(!Required && !IsFlag)
				Console.Write(" ({0})", this.DefaultValue);

			List<string> alt = new List<string>(AllNames);
			alt.Remove(DisplayName);
			if( alt.Count > 0 )
				Console.Write(" [/{0}{1}]", String.Join("=|/", alt.ToArray()), IsFlag ? String.Empty : "=");

			Console.Write(" {0}", this.Description);
			Console.WriteLine();
		}
	}

	partial class Option
	{
		public void Help()
		{
			Console.WriteLine();
			foreach (string name in this.AllNames)
			{
				Console.WriteLine("GET {0}", name.ToUpper());
				Console.WriteLine("SET {0} [value]", name.ToUpper());
			}

			//Console.WriteLine();
			//Console.WriteLine("Category: {0}", this.Category);
			//Console.WriteLine("Type: {0}", this.target);
			//Console.WriteLine("Prototype: {0}", this.Property);
			Console.WriteLine();
			Console.WriteLine(this.Description);
			Console.WriteLine();
		}
	}

}
