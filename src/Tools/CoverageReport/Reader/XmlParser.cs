#region Copyright 2009-2012 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Xml;
using CSharpTest.Net.CoverageReport.Counters;
using System.Text.RegularExpressions;

namespace CSharpTest.Net.CoverageReport.Reader
{
	class XmlParser
	{
		readonly Regex[] _excluded;

		public XmlParser(params string[] regexclude)
		{
			List<Regex> expressions = new List<Regex>();

			foreach (string rx in regexclude)
			{
				try
				{
					expressions.Add(new Regex(rx, RegexOptions.Compiled));
				}
				catch (ArgumentException e)
				{
					Log.Error("Invalid expression: {0}, error: {1}", rx, e.Message);
					throw;
				}
			}

			_excluded = expressions.ToArray();
		}

		//Reader state:
		DateTime _when = DateTime.MaxValue;
		string _version = String.Empty, _driverVersion = String.Empty;

		public readonly MetricGroup ByFile = new MetricGroup(new GroupByFile());
		public readonly MetricGroup ByModule = new MetricGroup(new GroupByModule());
		public readonly MetricGroup ByNamespace = new MetricGroup(new GroupByNamespace());
		public readonly MetricGroup ByClass = new MetricGroup(new GroupByClass());
		public readonly MetricGroup ByMethod = new MetricGroup(new GroupByMethod());
		public MetricReport Hierarchy
		{
			get
			{
				return new MetricReport(ByModule, ByNamespace, ByClass, ByMethod);
			}
		}

		public DateTime StartTime { get { return _when; } }
		public string VersionInfo { get { return _version; } }
		public string VersionDriver { get { return _driverVersion; } }

		public void Parse(string file)
		{
			using (XmlTextReader rdr = new XmlTextReader(file))
			{
				foreach (XmlData data in XmlData.Read(rdr))
				{
					_version = data.File.profilerVersion;
					_driverVersion = data.File.driverVersion;
					if (_when > data.File.startTime)
						_when = data.File.startTime;

					CodeMetric metric = new CodeMetric(data);

					if (ByFile.AddMetric(metric))//if this is a new statement
					{
						ByModule.AddMetric(metric);
						ByNamespace.AddMetric(metric);
						ByClass.AddMetric(metric);
						bool newMethod = ByMethod.AddMetric(metric);

						if (newMethod && Exclude(metric))
							metric.Excluded = true;
					}
				}
			}
		}

		private bool Exclude(CodeMetric metric)
		{
			foreach (Regex regex in _excluded)
			{
				foreach( string fmtName in new string[] { "{0}.{1}", "{0}.{1}.{2}" } )
				{
					string fullname = String.Format(fmtName, metric.Namespace, metric.Class, metric.MethodName);
					if (regex.IsMatch(fullname))
					{
						//Log.Info("Ignoring '{0}' from rule '{1}'", fullname, regex.ToString());
						return true;
					}
				}
			}
			return false;
		}

		public void Complete()
		{
			foreach (MetricCounter counter in ByMethod)
			{
				List<CodeMetric> metrics = counter.Seqpnts;
				bool anyInstrumented = false;
				foreach (CodeMetric metric in metrics)
					anyInstrumented |= metric.Instrumented;

				if (anyInstrumented)
				{
					foreach (CodeMetric metric in metrics)
					{
						if (metric.Instrumented == false && metric.VisitCount == 0)
							metric.Excluded = true;
					}
				}
			}
		}

		public long TotalFiles { get { return ByFile.Count; } }
		public long TotalClasses { get { return ByClass.Count; } }
		public long TotalMembers { get { return ByMethod.Count; } }
		public long TotalLines
		{
			get
			{
				long lines = 0;
				foreach (MetricCounter cntr in ByModule)
					lines += cntr.TotalLines;
				return lines;
			}
		}
		public long TotalSeqpnts
		{
			get
			{
				long seqpnts = 0;
				foreach (MetricCounter cntr in ByModule)
					seqpnts += cntr.TotalSeqpnts;
				return seqpnts;
			}
		}

		public long UnvisitedMembers
		{
			get
			{
				long unvisited = 0;
				foreach (MetricCounter cntr in ByMethod)
				{
					if (cntr.TotalSeqpnts > 0 && cntr.Unvisited >= cntr.TotalSeqpnts)
					{
						Log.Verbose("Method not visited: {0}", cntr.Name);
						unvisited++;
					}
				}
				return unvisited;
			}
		}
		public long UnvisitedSeqpnts
		{
			get
			{
				long seqpnts = 0;
				foreach (MetricCounter cntr in ByModule)
					seqpnts += cntr.Unvisited;
				return seqpnts;
			}
		}

		public double SeqpntCoverage { get { return CodeMetric.MakePercent(TotalSeqpnts, UnvisitedSeqpnts); } }
		public double MemberCoverage { get { return CodeMetric.MakePercent(TotalMembers, UnvisitedMembers); } }
	}
}
