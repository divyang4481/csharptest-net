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

namespace CSharpTest.Net.CoverageReport.Counters
{
	interface ICodeMetricGroup : IEqualityComparer<CodeMetric>, IComparer<CodeMetric>
	{
		string Name { get; }
		string NameOfGroupItem(CodeMetric metric);
	}

	class MetricGroup : IEnumerable<MetricCounter>
	{
		ICodeMetricGroup _grouping;
		Dictionary<CodeMetric, MetricCounter> _counters;

		public MetricGroup(ICodeMetricGroup grouping)
		{
			_grouping = grouping;
			_counters = new Dictionary<CodeMetric, MetricCounter>(grouping);
		}

		public ICodeMetricGroup Grouping { get { return _grouping; } }

		public string Name { get { return _grouping.Name; } }

		public MetricCounter this[CodeMetric metic] { get { return _counters[metic]; } }

		public bool AddMetric(CodeMetric metric)
		{
			MetricCounter counter;
			if (!_counters.TryGetValue(metric, out counter))
			{
				counter = new MetricCounter(_grouping.NameOfGroupItem(metric));
				counter.AddMetric(metric);
				_counters.Add(metric, counter);
				return true;
			}

			return counter.AddMetric(metric);
		}

		public int Count { get { return _counters.Count; } }

		public long TotalLines
		{
			get
			{
				long lines = 0;
				foreach (MetricCounter cntr in _counters.Values)
					lines += cntr.TotalLines;
				return lines;
			}
		}
		public long TotalSeqpnts
		{
			get
			{
				long seqpnts = 0;
				foreach (MetricCounter cntr in _counters.Values)
					seqpnts += cntr.TotalSeqpnts;
				return seqpnts;
			}
		}

		public bool Excluded { get { return TotalSeqpnts == 0; } }

		public long Unvisited
		{
			get
			{
				long seqpnts = 0;
				foreach (MetricCounter cntr in _counters.Values)
					seqpnts += cntr.Unvisited;
				return seqpnts;
			}
		}

		public double Coverage { get { return CodeMetric.MakePercent(TotalSeqpnts, Unvisited); } }

		#region IEnumerable<MetricCounter> Members

		public IEnumerator<MetricCounter> GetEnumerator()
		{
			return _counters.Values.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return ((System.Collections.IEnumerable)_counters.Values).GetEnumerator();
		}

		#endregion
	}
}
