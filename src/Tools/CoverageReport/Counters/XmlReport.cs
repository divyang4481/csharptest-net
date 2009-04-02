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
using System.IO;
using CSharpTest.Net.CoverageReport.Reader;
using System.Text;

namespace CSharpTest.Net.CoverageReport.Counters
{
	interface IReportWriter : IDisposable
	{
		void WriteItem(MetricGroup group, MetricCounter item);

		void StartItem(MetricGroup group, MetricCounter item);
		void StopItem(MetricGroup group, MetricCounter item);

		void StartGroup(MetricGroup group);
		void StopGroup(MetricGroup group);
	}


	class XmlReport : IReportWriter
	{
		const int ACCEPTABLE = 70;
		TextWriter _writer;
		int _depth;

		public XmlReport(TextWriter txtwtr, XmlParser parser, string title, string[] files)
		{
			_writer = txtwtr;
			WriteHeader(parser, title, files);
		}

		public void Dispose()
		{
			WriteFooter();
			_writer.Flush();
			_writer.Close();
			_writer = null;
		}

		#region IReportWriter
		public void StartGroup(MetricGroup group) { WriteLine("<{0}s>", group.Name); }
		public void StopGroup(MetricGroup group) { WriteLine("</{0}s>", group.Name); }

		public void StartItem(MetricGroup group, MetricCounter item) { WriteItem(group, item, false); }
		public void StopItem(MetricGroup group, MetricCounter item) { WriteLine("</{0}>", group.Name); }

		public void WriteItem(MetricGroup group, MetricCounter item) { WriteItem(group, item, true); }
		#endregion

		void WriteLine(string line, params object[] args)
		{
			if (args.Length > 0) line = String.Format(line, args);
			line = line.Trim();
			bool end = line.StartsWith("</");
			bool start = !end && line.StartsWith("<");

			if (end) _depth--;

			_writer.WriteLine("{0}{1}", new String(' ', _depth), line);

			if (start && !line.EndsWith("/>") && !line.Contains("</")) _depth++;
		}

		void WriteHeader(XmlParser parser, string title, string[] files)
		{
			_writer.WriteLine("<?xml version='1.0' encoding='utf-8'?>");
			_writer.WriteLine("<?xml-stylesheet href='CoverageReport.xsl' type='text/xsl'?>");

			WriteLine("<coverageReport reportTitle='{0}' date='{1}' time='{2}' version='{3}'>", title, parser.StartTime.ToString("D"), parser.StartTime.ToString("T"), parser.VersionInfo);

			WriteLine("<project name='{0}' files='{1}' classes='{2}' members='{3}' nonCommentLines='{4}' " +
							"sequencePoints='{5}' unvisitedPoints='{6}' unvisitedFunctions='{7}' coverage='{8}' " +
							"acceptable='{9}' functionCoverage='{10}' acceptableFunction='{11}' filteredBy='{12}' " +
							"sortedBy='{13}'>",
							title, parser.TotalFiles, parser.TotalClasses, parser.TotalMembers, parser.TotalLines,
							parser.TotalSeqpnts, parser.UnvisitedSeqpnts, parser.UnvisitedMembers, parser.SeqpntCoverage,
							ACCEPTABLE, parser.MemberCoverage, ACCEPTABLE, "None",
							"Name"
							);

			WriteLine("<coverageFiles>");
			foreach (string file in files)
				WriteLine("<coverageFile>{0}</coverageFile>", file);
			WriteLine("</coverageFiles>");
			WriteLine("</project>");
		}

		void WriteFooter()
		{
			WriteLine("</coverageReport>");
		}

		void WriteItem(MetricGroup group, MetricCounter item, bool close)
		{
			WriteLine("<{0} name='{1}' sequencePoints='{2}' unvisitedPoints='{3}' coverage='{4}' acceptable='{5}'{6}>",
				group.Name,
				XmlEncode(item.Name),
				item.TotalSeqpnts,
				item.Unvisited,
				Math.Round(item.Coverage, 4),
				ACCEPTABLE,
				close ? " /" : String.Empty
			);
		}

		string XmlEncode(string text)
		{
			StringBuilder sb = new StringBuilder();
			foreach (char ch in text)
			{
				if (ch == '<') sb.Append("&lt;");
				else if (ch == '\'' || ch == '\"' || ch == '\r' || ch == '\n')
					sb.AppendFormat("&#{0};", (int)ch);
				else
					sb.Append(ch);
			}
			return sb.ToString();
		}
	}
}
