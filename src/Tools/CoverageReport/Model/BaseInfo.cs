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
using CSharpTest.Net.CoverageReport.Reader;

namespace CSharpTest.Net.CoverageReport.Model
{
	abstract class BaseInfo<TChild> : IEnumerable<TChild>
	{
		public readonly string Name;
		public long VisitCount { get { return _visits; } }
		public long SequencePoints { get { return _sequencePoints; } }
		public long UnvisitedPoints { get { return _unvisitedPoints; } }
		public long VisitedPoints { get { return _sequencePoints - _unvisitedPoints; } }
		public double CoveragePercent { get { return SequencePoints > 0 ? (VisitedPoints / (SequencePoints / 100.0)) : 0; } }

		readonly Dictionary<string, TChild> _children;

		long _visits, _sequencePoints, _unvisitedPoints;

		public BaseInfo(string name)
		{
			this.Name = name;

			_children = new Dictionary<string,TChild>();
			_visits = 0;
		}

		public TChild this[string name]
		{
			get
			{
				TChild child;
				if (!_children.TryGetValue(name, out child))
					_children.Add(name, child = MakeChild(name));

				return child;
			}
		}

		protected abstract TChild MakeChild(string name);

		public virtual void Add(XmlData item)
		{
			_sequencePoints++;
			
			if (item.Seqpnt.visitcount == 0)
				_unvisitedPoints++;
			else
				_visits += item.Seqpnt.visitcount;
		}	

		public IEnumerator<TChild> GetEnumerator()
		{
			return _children.Values.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return ((System.Collections.IEnumerable)_children.Values).GetEnumerator();
		}
	}
}
