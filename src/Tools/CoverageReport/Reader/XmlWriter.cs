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
using CSharpTest.Net.CoverageReport.Counters;
using System.Xml;
using System.Text;

namespace CSharpTest.Net.CoverageReport.Reader
{
	class XmlCoverageWriter : IReportWriter, IDisposable
	{
		readonly TextWriter _writer;
		int _depth;
		int _moduleId;

		const string TRUE = "true";
		const string FALSE = "false";

		public XmlCoverageWriter(TextWriter txtwtr, XmlParser parser)
		{
			_writer = txtwtr;
			_moduleId = 1;
			_depth = 0;

			WriteHeader(parser);
		}

		public void Dispose()
		{
			WriteFooter();
			_writer.Flush();
			_writer.Close();
		}

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

		#region IReportWriter
		public void StartGroup(MetricGroup group)
		{}
		public void StopGroup(MetricGroup group) 
		{}

		public void StartItem(MetricGroup group, MetricCounter item) 
		{
			Check.IsEqual("module", group.Name);
			CodeMetric info = item.Group;

			WriteLine("<module moduleId='{0}' name='{1}' assembly='{2}' assemblyIdentity='{3}'>",
				_moduleId++,
				XmlEncode(info.FileName),
				XmlEncode(info.AssemblyName),
				XmlEncode(info.AssemblyFullName)
			);
		}
		
		public void StopItem(MetricGroup group, MetricCounter item) 
		{
			Check.IsEqual("module", group.Name);
			WriteLine("</module>");
		}

		public void WriteItem(MetricGroup group, MetricCounter item) { WriteItem(group, item, true); }
		#endregion

		private void WriteHeader(XmlParser parser)
		{
			MetricGroup methods = parser.ByMethod;
			_writer.WriteLine("<?xml version='1.0' encoding='utf-8'?>");
			_writer.WriteLine("<?xml-stylesheet href='coverage.xsl' type='text/xsl'?>");
			WriteLine("<coverage profilerVersion='{0}' driverVersion='{1}' startTime='{2}' measureTime='{3}'>",
				parser.VersionInfo, parser.VersionDriver,
				XmlConvert.ToString(parser.StartTime, XmlDateTimeSerializationMode.RoundtripKind),
				XmlConvert.ToString( parser.StartTime,  XmlDateTimeSerializationMode.RoundtripKind)
				);
		}

		void WriteFooter()
		{
			_writer.WriteLine("</coverage>");
		}

		void WriteItem(MetricGroup group, MetricCounter item, bool close)
		{
			CodeMetric info = item.Group;

			List<CodeMetric> seqpnts = item.Seqpnts;

			bool anyInstruments = false;
			bool allExcluded = true;
			foreach (CodeMetric metric in seqpnts)
			{
				anyInstruments |= metric.Instrumented;
				allExcluded &= metric.Excluded;
			}

			WriteLine("<method name='{0}' excluded='{5}' instrumented='{4}' class='{1}{2}{3}'>",
				XmlEncode(info.MethodName),
				XmlEncode(info.Namespace),
				String.IsNullOrEmpty(info.Namespace) ? String.Empty : ".",
				XmlEncode(info.Class),
				anyInstruments ? TRUE : FALSE,
				allExcluded ? TRUE : FALSE
			);

			foreach (CodeMetric metric in seqpnts)
			{
				if (metric.Excluded)
					continue;

				WriteLine("<seqpnt visitcount='{0}' line='{1}' column='{2}' endline='{3}' endcolumn='{4}' excluded='{5}' document='{6}' />",
					metric.VisitCount,
					metric.Line,
					metric.Column,
					metric.EndLine,
					metric.EndColumn,
					metric.Excluded ? TRUE : FALSE,
					XmlEncode(metric.SrcFile)
				);
			}

			WriteLine("</method>");
		}

		string XmlEncode(string text)
		{
			if (text == null) return String.Empty;

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
