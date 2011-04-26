#region Copyright 2008-2011 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Xml.Serialization;
using System.ComponentModel;

#pragma warning disable 1591

namespace CSharpTest.Net.CSBuild.Configuration
{
    [Serializable]
    public abstract class BaseOutput
	{
		[XmlAttribute("level")]
		[DefaultValue(LoggerVerbosity.Normal)]
		public LoggerVerbosity Level;
	}

    [Serializable]
	public abstract class BaseFileOutput : BaseFileItem
	{
		[XmlAttribute("level")]
		public LoggerVerbosity Level;

		[XmlAttribute("file")]
		public string FileName;

		public override string AbsolutePath(IDictionary<string, string> namedValues)
		{
			if (this.Path == null)
			{
				this.RelativeTo = OutputRelative.None;
				return FileName;
			}
			return base.AbsolutePath(namedValues);
		}
	}

    [Serializable]
	public class LogFileOutput : BaseFileOutput
    {
    }
    [Serializable]
	public class XmlFileOutput : BaseFileOutput
    {
	}
}
