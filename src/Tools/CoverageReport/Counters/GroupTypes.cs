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

namespace CSharpTest.Net.CoverageReport.Counters
{
	class GroupByFile : ICodeMetricGroup
	{
		public string Name { get { return "file"; } }

		public string NameOfGroupItem(CodeMetric obj)
		{ return obj.SrcFile; }

		public bool Equals(CodeMetric x, CodeMetric y)
		{
			return StringComparer.OrdinalIgnoreCase.Equals(x.SrcFile, y.SrcFile);
		}

		public int Compare(CodeMetric x, CodeMetric y)
		{
			return StringComparer.OrdinalIgnoreCase.Compare(x.SrcFile, y.SrcFile);
		}

		public int GetHashCode(CodeMetric obj)
		{
			return StringComparer.OrdinalIgnoreCase.GetHashCode(obj.SrcFile);
		}
	}

	class GroupByModule : ICodeMetricGroup
	{
		public string Name { get { return "module"; } }

		public string NameOfGroupItem(CodeMetric obj)
		{ return obj.AssemblyName; }

		public bool Equals(CodeMetric x, CodeMetric y)
		{
			return StringComparer.Ordinal.Equals(x.AssemblyName, y.AssemblyName);
		}

		public int Compare(CodeMetric x, CodeMetric y)
		{
			return StringComparer.Ordinal.Compare(x.AssemblyName, y.AssemblyName);
		}

		public int GetHashCode(CodeMetric obj)
		{
			return StringComparer.Ordinal.GetHashCode(obj.AssemblyName);
		}
	}

	class GroupByNamespace : ICodeMetricGroup
	{
		public virtual string Name { get { return "namespace"; } }

		public virtual string NameOfGroupItem(CodeMetric obj)
		{ return obj.Namespace; }

		public virtual bool Equals(CodeMetric x, CodeMetric y)
		{
			return StringComparer.Ordinal.Equals(x.Namespace, y.Namespace);
		}

		public virtual int Compare(CodeMetric x, CodeMetric y)
		{
			return StringComparer.Ordinal.Compare(x.Namespace, y.Namespace);
		}

		public virtual int GetHashCode(CodeMetric obj)
		{
			return StringComparer.Ordinal.GetHashCode(obj.Namespace);
		}
	}

	class GroupByClass : GroupByNamespace
	{
		public override string Name { get { return "class"; } }

		public override string NameOfGroupItem(CodeMetric obj)
		{ return String.Format("{0}.{1}", base.NameOfGroupItem(obj), obj.Class); }

		public override bool Equals(CodeMetric x, CodeMetric y)
		{
			return base.Equals(x, y) && StringComparer.Ordinal.Equals(x.Class, y.Class);
		}

		public override int Compare(CodeMetric x, CodeMetric y)
		{
			int result = base.Compare(x, y);
			return result != 0 ? result : StringComparer.Ordinal.Compare(x.Class, y.Class);
		}

		public override int GetHashCode(CodeMetric obj)
		{
			return (base.GetHashCode(obj) & 0x4FFFFFFF) + StringComparer.Ordinal.GetHashCode(obj.Class);
		}
	}

	class GroupByMethod : GroupByClass
	{
		public override string Name { get { return "member"; } }

		public override string NameOfGroupItem(CodeMetric obj)
		{ return String.Format("{0}.{1}", base.NameOfGroupItem(obj), obj.MethodName); }

		public override bool Equals(CodeMetric x, CodeMetric y)
		{
			return base.Equals(x, y) && StringComparer.Ordinal.Equals(x.MethodName, y.MethodName);
		}

		public override int Compare(CodeMetric x, CodeMetric y)
		{
			int result = base.Compare(x, y);
			return result != 0 ? result : StringComparer.Ordinal.Compare(x.MethodName, y.MethodName);
		}

		public override int GetHashCode(CodeMetric obj)
		{
			return (base.GetHashCode(obj) & 0x4FFFFFFF) + StringComparer.Ordinal.GetHashCode(obj.MethodName);
		}
	}
}
