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
using NUnit.Framework;
using System.Xml.Serialization;
using System.Configuration;
using System.Xml;
using System.IO;

#pragma warning disable 1591
namespace CSharpTest.Net.Utils.Test
{
	[TestFixture]
	public partial class TestXmlConfiguration
	{
		[Test]
		public void Test()
		{
			if (File.Exists(Section1.XsdFile))
				File.Delete(Section1.XsdFile);

			Section1 a = (XmlConfiguration<Section1>)ConfigurationManager.GetSection("Section1");
			Assert.AreEqual(123, a.id);
			Assert.AreEqual(432.1, a.value);
			Assert.AreEqual("hello", a.name);
			a = null;

			Section2 b = ((XmlConfiguration<Section2>)ConfigurationManager.GetSection("Section2")).Settings;
			Assert.AreEqual(123, b.id);
			Assert.AreEqual("432.1", b.value);
			Assert.AreEqual("hello", b.name);
			b = null;

			b = XmlConfiguration<Section2>.ReadConfig("Section2");
			Assert.AreEqual(123, b.id);
			Assert.AreEqual("432.1", b.value);
			Assert.AreEqual("hello", b.name);
			b = null;

			using (XmlReader rdr = new XmlTextReader(new StringReader("<Section2 id='123' value='432.1'><name>hello</name></Section2>")))
			{
				b = XmlConfiguration<Section2>.ReadXml(rdr);
				Assert.AreEqual(123, b.id);
				Assert.AreEqual("432.1", b.value);
				Assert.AreEqual("hello", b.name);
				b = null;
			}

			XmlConfiguration<Section2>.XmlSchema = System.Xml.Schema.XmlSchema.Read(
					this.GetType().Assembly.GetManifestResourceStream(this.GetType().Namespace + ".TestXmlSection2.xsd"),
					null
				);

			//now we should get an exception when reading section 2...
			try
			{
				ConfigurationManager.RefreshSection("Section2");
				b = (XmlConfiguration<Section2>)ConfigurationManager.GetSection("Section2");
			}
			catch (System.Configuration.ConfigurationErrorsException ce)
			{
				Assert.IsTrue(ce.Message.Contains("The 'value' attribute is invalid"));
				Assert.IsNotNull(ce.InnerException);
				Assert.AreEqual(typeof(XmlException), ce.InnerException.GetType());
			}
			Assert.IsNull(b);

			Section3 c = (XmlConfiguration<Section3>)ConfigurationManager.GetSection("Section3");
			Assert.AreEqual(123, c.id);
			Assert.AreEqual(new DateTime(2009, 12, 25, 0, 0, 0), c.value);
			Assert.AreEqual("hello", c.name);
			b = null;

			// the following invalid xsd will cause an exception
			File.WriteAllText(Section1.XsdFile, "<xs:schema  xmlns:xs='http://www.w3.org/2001/XMLSchema'><xs:element /></xs:schema>");
			ConfigurationManager.RefreshSection("Section1");
			string origDir = Environment.CurrentDirectory;
			Environment.CurrentDirectory = Path.GetTempPath();
			try
			{
				a = (XmlConfiguration<Section1>)ConfigurationManager.GetSection("Section1");
			}
			catch (ConfigurationErrorsException) { }
			finally 
			{
				Environment.CurrentDirectory = origDir;
				File.Delete(Section1.XsdFile); 
			}
			Assert.IsNull(a);
		}

		[XmlRoot]
		public class Section1
		{
			public static string XsdFile = Path.Combine(Path.GetTempPath(), typeof(Section1).FullName + ".xsd");
			[XmlAttribute]
			public int id;

			[XmlAttribute]
			public double value;

			[XmlElement]
			public string name;
		}

		[XmlRoot]
		public class Section2
		{
			[XmlAttribute]
			public int id;

			[XmlAttribute]
			public string value;

			[XmlElement]
			public string name;
		}

		[XmlRoot]
		public class Section3
		{
			[XmlAttribute]
			public int id;

			[XmlAttribute]
			public DateTime value;

			[XmlElement]
			public string name;
		}
	}
}
