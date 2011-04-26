#region Copyright 2009-2011 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using CSharpTest.Net.CoverageReport.Reader;

namespace CSharpTest.Net.CoverageReport.Counters
{
	class CodeMetric : IComparable<CodeMetric>
	{
		private bool _instrumented;
		private long _counter;

		public CodeMetric(XmlData data)
		{
			this.ModuleName = data.Module.name;
			this.AssemblyName = data.Module.assembly;
			this.FileName = data.Module.filename;
			this.AssemblyFullName = data.Module.assemblyIdentity;

			this.Namespace = data.Method.Namespace;
			this.Class = data.Method.Class;
			this.MethodName = data.Method.name;

			this.SrcFile = data.Seqpnt.document;
			this.Line = data.Seqpnt.line;
			this.Column = data.Seqpnt.column;
			this.EndLine = data.Seqpnt.endline;
			this.EndColumn = data.Seqpnt.endcolumn;
			
			this.Excluded = data.Seqpnt.excluded | data.Method.excluded;

			_instrumented = data.Method.instrumented;
			_counter = data.Seqpnt.visitcount;
		}

		public readonly string ModuleName;
		public readonly string AssemblyName;
		public readonly string FileName;
		public readonly string AssemblyFullName;

		public readonly string Namespace;
		public readonly string Class;
		public readonly string MethodName;

		public readonly string SrcFile;

		public readonly int Line;
		public readonly int Column;
		public readonly int EndLine;
		public readonly int EndColumn;

		public bool Excluded;

		public bool Instrumented { get { return _instrumented; } }
		public long VisitCount { get { return _counter; } }

		public void Add(CodeMetric other)
		{ 
			this._counter += other._counter; 
			this._instrumented |= other._instrumented;
			this.Excluded |= other.Excluded;
		}

		public static double MakePercent(long total, long unvisited)
		{
			long visited = Math.Max(0L, total - unvisited);
			return total > 0 ? (visited / (total / 100.0)) : 0;
		}
		
		public override bool Equals(object obj)
		{
			if (obj is CodeMetric)
				return 0 == CompareTo((CodeMetric)obj);
			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			long result = 0;
			if (this.SrcFile == null)
			{
				result += StringComparer.Ordinal.GetHashCode(this.Namespace);
				result += StringComparer.Ordinal.GetHashCode(this.Class);
				result += StringComparer.Ordinal.GetHashCode(this.MethodName);
			}
			else
				result += StringComparer.OrdinalIgnoreCase.GetHashCode(this.SrcFile);

			result += this.Line;
			result += this.Column;
			return (int)(result & 0x7FFFFFFF);
		}

		public int CompareTo(CodeMetric other)
		{
			int result;
			if (this.SrcFile == null)
			{
				result = StringComparer.Ordinal.Compare(this.Namespace, other.Namespace);
				if (result != 0) return result;
				result = StringComparer.Ordinal.Compare(this.Class, other.Class);
				if (result != 0) return result;
				result = StringComparer.Ordinal.Compare(this.MethodName, other.MethodName);
			}
			else
				result = StringComparer.OrdinalIgnoreCase.Compare(this.SrcFile, other.SrcFile);
			if (result != 0) return result;

			if (this.Line != other.Line)
                return Line.CompareTo(other.Line);
			if (this.Column != other.Column)
                return Column.CompareTo(other.Column);

			return 0;
		}
	}
}
