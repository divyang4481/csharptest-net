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
	class MetricCounter : IComparable<MetricCounter>
	{
		public readonly string Name;
		Dictionary<CodeMetric, CodeMetric> _entries;

		public MetricCounter( string name )
		{
			this.Name = name;
			_entries = new Dictionary<CodeMetric, CodeMetric>();
		}

		public bool AddMetric(CodeMetric metric)
		{
			CodeMetric actual;
			if (!_entries.TryGetValue(metric, out actual))
			{
				_entries.Add(metric, metric);
				return true;
			}

			actual.Add(metric);
			return false;
		}

		public CodeMetric Group { get { foreach (CodeMetric cm in _entries.Keys) return cm; throw new ArgumentException(); } }

		public ICollection<CodeMetric> NonExcludedLines
		{
			get
			{
				List<CodeMetric> metrics = new List<CodeMetric>();
				foreach (CodeMetric metric in _entries.Values)
				{
					if (metric.Excluded == false)
						metrics.Add(metric);
				}
				return metrics;
			}
		}

		public bool Excluded { get { return TotalSeqpnts == 0; } }

		public List<CodeMetric> Seqpnts
		{
			get
			{
				List<CodeMetric> metrics = new List<CodeMetric>(_entries.Values);
				metrics.Sort();
				return metrics;
			}
		}

		public int Count { get { return _entries.Count; } }
		public long TotalSeqpnts { get { return NonExcludedLines.Count; } }

		public long Unvisited
		{
			get
			{
				long unvisited = 0;
				foreach (CodeMetric metric in NonExcludedLines)
				{
					if (metric.VisitCount == 0 && !metric.Excluded)
						unvisited++;
				}
				return unvisited;
			}
		}

		public double Coverage { get { return CodeMetric.MakePercent(TotalSeqpnts, Unvisited); } }

		public long TotalLines
		{
			get
			{
				long lines = 0;
				foreach (CodeMetric metric in NonExcludedLines)
					lines += 1 + (metric.EndLine - metric.Line);
				return lines;
			}
		}

		public int CompareTo(MetricCounter other)
		{
			return StringComparer.Ordinal.Compare(this.Name, other.Name);
		}
	}
}
