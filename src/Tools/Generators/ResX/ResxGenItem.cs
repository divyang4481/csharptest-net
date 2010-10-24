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
using System.Collections.Generic;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;
using CSharpTest.Net.Utils;

namespace CSharpTest.Net.Generators.ResX
{
	class ResxGenItem
	{
		static readonly System.Reflection.AssemblyName[] AllowedNames = new System.Reflection.AssemblyName[] { };
		static readonly Regex ExceptionMatch = new Regex(@"Exception(\(|$)");
		static readonly Regex FormatingMatch = RegexPatterns.FormatSpecifier;

		public readonly List<ResxGenArgument> Args;
		public readonly bool IsFormatter;
		public readonly bool IsException;

		public readonly string Namespace;
		public readonly string Identifier;

		public readonly string FullName;
		public readonly string ItemName;
		public readonly string Comments;
		public readonly string Value;
		public readonly bool Ignored;

		public ResxGenItem(string identifier, string nameSpace, ResXDataNode node)
		{
			Ignored = true;//and clear upon complete...
			Args = new List<ResxGenArgument>();
			try 
			{
				Type type = Type.GetType(node.GetValueTypeName(AllowedNames));
				if (type == null || type != typeof(String))
					return;
				Value = (String)node.GetValue(AllowedNames);
			}
			catch { return; }

			Namespace = nameSpace;
			Identifier = identifier;
			FullName = ItemName = node.Name;
			Comments = node.Comment;
			string rawArgs = null;

			IsFormatter = FormatingMatch.IsMatch(Value);
			IsException = ExceptionMatch.IsMatch(node.Name);
			if (!IsFormatter && !IsException)
				return;

			int pos;
			if ((pos = ItemName.IndexOf('(')) > 0)
			{
				rawArgs = ItemName.Substring(pos);
				ItemName = ItemName.Substring(0, pos);
			}
			else if (Comments.StartsWith("(") && (pos = Comments.IndexOf(')')) > 0)
			{
				rawArgs = Comments.Substring(0, 1 + pos);
				Comments = Comments.Substring(pos + 1).Trim();
			}
			if (!String.IsNullOrEmpty(rawArgs))
				Args.AddRange(new ResxGenArgParser(rawArgs));

			//now thats out of the way... let's transform the format string into something usable:
			Value = StringUtils.Transform(Value, FormatingMatch,
				delegate(Match m)
				{
					return "{" + GetArg(null, m.Groups["field"].Value) + m.Groups["suffix"].Value + "}";
				}
			);

			Ignored = false;
		}

		public bool Hidden { get { return IsFormatter || IsException; } }

		public string Parameters(bool includeTypes)
		{
			StringBuilder sbArgs = new StringBuilder();
			int count = 0;
			foreach (ResxGenArgument kv in Args)
			{
				if (count++ > 0) sbArgs.Append(", ");
				if (includeTypes) sbArgs.AppendFormat("{0} ", kv.Type);
				sbArgs.Append(kv.ParamName);
			}
			return sbArgs.ToString();
		}

		public int GetArg(string type, string name)
		{
			type = String.IsNullOrEmpty(type) ? "object" : type;

			int ordinal;
			if (int.TryParse(name, out ordinal))
			{
				for (int add = Args.Count; add < ordinal; add++)
					Args.Add(new ResxGenArgument("object",  "_" + add));
				if(Args.Count == ordinal)
					Args.Add(new ResxGenArgument(type, "_" + ordinal));
				return ordinal;
			}

			for(int i=0; i < Args.Count; i++)
				if(Args[i].Name == name) return i;

			Args.Add(new ResxGenArgument(type, name));
			return Args.Count - 1;
		}
	}
}