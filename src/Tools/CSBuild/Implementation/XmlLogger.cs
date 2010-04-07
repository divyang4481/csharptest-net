#region Copyright 2008 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using Microsoft.Build.Framework;
using System.IO;
using System.Xml;

namespace CSharpTest.Net.CSBuild.Implementation
{
	class XmlLogger : ILogger
	{
		LoggerVerbosity _verbosity = LoggerVerbosity.Normal;
		string _logfile = Path.GetFullPath("msbuild.xml");
		XmlTextWriter _output = null;
		
		//log state:
		DateTime _start;
		int _errors = 0, _warnings = 0;
		string _current = null;

		public void Initialize(IEventSource eventSource)
		{
			_start = DateTime.Now;
			_current = null;
			_errors = _warnings = 0;

			_output = new XmlTextWriter(File.Open(_logfile, FileMode.Create, FileAccess.Write, FileShare.Read), System.Text.Encoding.ASCII);
			_output.Formatting = Formatting.Indented;
			_output.Indentation = 1;
			_output.IndentChar = '\t';
			_output.WriteStartElement("msbuild");
			_output.WriteAttributeString("startTime", _start.ToString("o"));

			eventSource.TargetStarted += new TargetStartedEventHandler(eventSource_TargetStarted);
			if (_verbosity == LoggerVerbosity.Diagnostic || _verbosity == LoggerVerbosity.Detailed)
				eventSource.MessageRaised += new BuildMessageEventHandler(eventSource_MessageRaised);
			eventSource.WarningRaised += new BuildWarningEventHandler(eventSource_WarningRaised);
			eventSource.ErrorRaised += new BuildErrorEventHandler(eventSource_ErrorRaised);
		}

		void CloseTarget()
		{
			if (_current != null && _output != null)
			{
				_current = null;
				_output.WriteEndElement();
				_output.Flush();
			}
		}

		void eventSource_TargetStarted(object sender, TargetStartedEventArgs e)
		{
			if (_output == null || String.IsNullOrEmpty(e.ProjectFile) || e.ProjectFile == _current) 
				return;

			CloseTarget();

			_current = e.ProjectFile;
			_output.WriteStartElement("project");

			string filename = e.ProjectFile;
			_output.WriteAttributeString("file", Path.GetFileName(filename));
			_output.WriteAttributeString("location", Path.GetDirectoryName(filename));
			
			if(!String.IsNullOrEmpty(e.TargetFile) )
				_output.WriteAttributeString("target", e.TargetFile);
		}

		void eventSource_MessageRaised(object sender, BuildMessageEventArgs e)
		{
			if (_output == null || e.Importance == MessageImportance.Low || String.IsNullOrEmpty(e.Message))
				return;

			_output.WriteStartElement("message");
			try
			{
				_output.WriteAttributeString("level", e.Importance.ToString().ToLower());
				_output.WriteAttributeString("text", e.Message.Trim());
			}
			finally
			{
				_output.WriteEndElement();
				_output.Flush();
			}
		}

		void eventSource_WarningRaised(object sender, BuildWarningEventArgs e)
		{
			if (_output == null || String.IsNullOrEmpty(e.Message))
				return;
			_warnings++;
			_output.WriteStartElement("message");
			try
			{
				_output.WriteAttributeString("level", "warning");
				_output.WriteAttributeString("code", e.Code);
				_output.WriteAttributeString("text", e.Message.Trim());
				if (!String.IsNullOrEmpty(e.Subcategory))
					_output.WriteAttributeString("category", e.Subcategory);
				_output.WriteAttributeString("line", e.LineNumber.ToString());
				_output.WriteAttributeString("col", e.ColumnNumber.ToString());
				_output.WriteAttributeString("file", e.File);
			}
			finally
			{
				_output.WriteEndElement();
			}
		}

		void eventSource_ErrorRaised(object sender, BuildErrorEventArgs e)
		{
			if (_output == null || String.IsNullOrEmpty(e.Message))
				return;
			_errors++;
			_output.WriteStartElement("message");
			try
			{
				_output.WriteAttributeString("level", "error");
				_output.WriteAttributeString("code", e.Code);
				_output.WriteAttributeString("text", e.Message.Trim());
				if (!String.IsNullOrEmpty(e.Subcategory))
					_output.WriteAttributeString("category", e.Subcategory);
				_output.WriteAttributeString("line", e.LineNumber.ToString());
				_output.WriteAttributeString("col", e.ColumnNumber.ToString());
				_output.WriteAttributeString("file", e.File);
			}
			finally
			{
				_output.WriteEndElement();
			}
		}

		public void Shutdown()
		{
			if (_output == null)
				return;

			DateTime end = DateTime.Now;

			try
			{
				CloseTarget();

				_output.WriteStartElement("completed");
				_output.WriteAttributeString("errors", _errors.ToString());
				_output.WriteAttributeString("warnings", _warnings.ToString());
				_output.WriteAttributeString("endTime", end.ToString("o"));
				_output.WriteAttributeString("duration", (end - _start).ToString());
				_output.WriteEndElement();
				_output.WriteEndElement();
			}
			finally
			{
				XmlTextWriter wtr = _output;
				_output = null;
				wtr.Flush();
				wtr.Close();
			}
		}

		public string Parameters
		{
			get { return String.Format("logfile={0}", _logfile); }
			set
			{
				if (_output != null)
					throw new InvalidOperationException("XmlLogger is already open.");
				if (!Check.NotNull(value).StartsWith("logfile="))
					throw new ArgumentException("Unrecognized argument: {0}", value);
				_logfile = Path.GetFullPath(value.Substring(8).Trim());
			}
		}

		public LoggerVerbosity Verbosity
		{
			get { return _verbosity; }
			set { _verbosity = value; }
		}
	}
}
