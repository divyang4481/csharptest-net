#region Copyright 2009-2014 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
	class MetricReport
	{
		MetricGroup[] _heirarchy;
		ICodeMetricGroup[] _groupings;
		MetricCounter[] _opened;

		public MetricReport(params MetricGroup[] groups)
		{
			Check.NotEmpty(groups);

			_heirarchy = groups;
			_groupings = new ICodeMetricGroup[groups.Length];
			for( int i=0; i < groups.Length; i++ )
				_groupings[i] = groups[i].Grouping;
		}

		public void Write(IReportWriter writer)
		{
			MetricGroup lastGroup = _heirarchy[_heirarchy.Length - 1];

			List<MetricCounter> items = new List<MetricCounter>(lastGroup);
			items.Sort(new Comparer(_groupings));

			_opened = new MetricCounter[_heirarchy.Length];

			writer.StartGroup(_heirarchy[0]);

			foreach (MetricCounter counter in items)
			{
				if (counter.Excluded == false)
					WriteItem(writer, counter);
			}

			//close open
			for (int ix = _groupings.Length - 1; ix >= 0; ix--)
			{
				if (_opened[ix] != null)
					writer.StopItem(_heirarchy[ix], _opened[ix]);
				_opened[ix] = null;
			}

			writer.StopGroup(_heirarchy[0]);
		}

		private void WriteItem(IReportWriter writer, MetricCounter counter)
		{
			CodeMetric current = counter.Group;
			int ixDiff = 0;
			while (ixDiff < _groupings.Length && _opened[ixDiff] != null && _groupings[ixDiff].Equals(current, _opened[ixDiff].Group))
				ixDiff++;

			//close open
			for (int ix = _groupings.Length - 1; ix >= ixDiff; ix--)
			{
				if(_opened[ix] != null)
					writer.StopItem(_heirarchy[ix], _opened[ix]);
				_opened[ix] = null;
			}

			//open new
			for (int ix = ixDiff; ix < _opened.Length - 1 && _opened[ix] == null; ix++)
				writer.StartItem(_heirarchy[ix], _opened[ix] = _heirarchy[ix][counter.Group]);

			writer.WriteItem(_heirarchy[_heirarchy.Length - 1], counter);
		}

		private class Comparer : IComparer<MetricCounter>
		{
			ICodeMetricGroup[] _groupings;

			public Comparer(ICodeMetricGroup[] groupings)
			{
				_groupings = groupings;
			}

			public int Compare(MetricCounter x, MetricCounter y)
			{
				int result;
				foreach (ICodeMetricGroup grp in _groupings)
				{
					result = grp.Compare(x.Group, y.Group);
					if (result != 0) return result;
				}

				return 0;
			}
		}
	}
}
