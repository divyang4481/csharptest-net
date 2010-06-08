#region Copyright 2010 by Roger Knapp, Licensed under the Apache License, Version 2.0
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

namespace CSharpTest.Net.Html
{
	/// <summary>
	/// Represents a single attribute on an xml element
	/// </summary>
	public struct XmlLightAttribute
	{
		/// <summary> The full name of the attribute </summary>
		public string Name;
		/// <summary> The original encoded text value of the attribute </summary>
		public string Value;
	}

	/// <summary>
	/// Provides a means by which the XmlLightParser can inform you of the document
	/// elements encountered.
	/// </summary>
	public interface IXmlLightReader
	{
		/// <summary> Begins the processing of an xml input </summary>
		void StartDocument();

		/// <summary> Begins the processing of an xml tag </summary>
		void StartTag(string fullName, bool selfClosed, string unparsedTag, IEnumerable<XmlLightAttribute> attributes);

		/// <summary> Ends the processing of an xml tag </summary>
		void EndTag(string fullName);

		/// <summary> Encountered text or whitespace in the document </summary>
		void AddText(string content);

		/// <summary> Encountered comment in the document </summary>
		void AddComment(string comment);

		/// <summary> Encountered cdata section in the document </summary>
		void AddCData(string cdata);

		/// <summary> Encountered control information &lt;! ... &gt; in the document </summary>
		void AddControl(string cdata);

		/// <summary> Encountered processing instruction &lt;? ... ?&gt; in the document </summary>
		void AddInstruction(string instruction);

		/// <summary> Ends the processing of an xml input </summary>
		void EndDocument();
	}
}
