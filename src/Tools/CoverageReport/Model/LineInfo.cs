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
	class LineInfo : BaseInfo<object>
	{
		Dictionary<string, Dictionary<long, long>> _lineNumbers;

		public LineInfo(string name)
			: base(name)
		{
			_lineNumbers = new Dictionary<string, Dictionary<long, long>>();
		}

		protected override object MakeChild(string name)
		{ throw new NotImplementedException(); }

		public override void Add(XmlData item)
		{
			base.Add(item);
			if (item.Seqpnt.document != null)
			{
				for (long line = item.Seqpnt.line; line <= item.Seqpnt.endline; line++)
					_lineNumbers[item.Seqpnt.document][line]++;
			}
		}
	}
}
