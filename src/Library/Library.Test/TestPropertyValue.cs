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
using NUnit.Framework;
using CSharpTest.Net.Reflection;

#pragma warning disable 1591

namespace CSharpTest.Net.Library.Test
{
	[TestFixture]
	public partial class TestPropertyValue
	{
		//Test classes
		interface ia { string PropertyA { get; set; } }
		interface ib : ia { ia PropertyB { get; set; } }

		// Test classes:
		class a : ia
		{
			string privateField;
			public string PropertyA { get { return privateField; } set { privateField = value; } }
		}
		class b : a, ib
		{
			public ia publicField;
			ia ib.PropertyB { get { return publicField; } set { publicField = value; } }
			ia PrivateProperty { get { return publicField; } set { publicField = value; } }
		}
		
		[Test]
		public void TestPublicProperty()
		{
			a obj = new a();
			obj.PropertyA = "a";
			PropertyValue pt = new PropertyValue(obj, "PropertyA");

			Assert.AreEqual("PropertyA", pt.Name);
			Assert.AreEqual(typeof(string), pt.Type);
			Assert.AreEqual("a", pt.Value);
			pt.Value = "b";
			Assert.AreEqual("b", pt.Value);
		}
		[Test]
		public void TestPrivateProperty()
		{
			a obja = new a();
			b obj = new b();
			obj.PropertyA = "a";
			obj.publicField = obja;
			PropertyValue pt = new PropertyValue(obj, "PrivateProperty");

			Assert.AreEqual("PrivateProperty", pt.Name);
			Assert.AreEqual(typeof(ia), pt.Type);
			Assert.AreEqual(obja, pt.Value);
			pt.Value = new a();
			Assert.AreNotEqual(obja, pt.Value);
		}
		[Test]
		public void TestPublicField()
		{
			a obja = new a();
			b obj = new b();
			obj.PropertyA = "a";
			obj.publicField = obja;
			PropertyValue pt = new PropertyValue(obj, "publicField");

			Assert.AreEqual("publicField", pt.Name);
			Assert.AreEqual(typeof(ia), pt.Type);
			Assert.AreEqual(obja, pt.Value);
			pt.Value = new a();
			Assert.AreNotEqual(obja, pt.Value);
		}
		[Test]
		public void TestPrivateField()
		{
			a obj = new a();
			obj.PropertyA = "a";
			PropertyValue pt = new PropertyValue(obj, "privateField");

			Assert.AreEqual("privateField", pt.Name);
			Assert.AreEqual(typeof(string), pt.Type);
			Assert.AreEqual("a", pt.Value);
			pt.Value = "b";
			Assert.AreEqual("b", pt.Value);
		}
		[Test]
		public void TestPrivateField2()
		{
			a obj = new a();
			obj.PropertyA = "a";
			PropertyValue<string> pt = new PropertyValue<string>(obj, "privateField");
			Assert.AreEqual("a", pt.Value);
			pt.Value = "b";
			Assert.AreEqual("b", pt.Value);
		}

		[Test]
		public void TestPropertyTypeTraversals()
		{
			b obj = new b();
			a obja = new a();
			obj.publicField = obja;
			obja.PropertyA = "test";

			PropertyValue pt;
			//Any of the following formats can be used
			pt = PropertyValue.TraverseProperties(obj, "publicField.PropertyA.Length");
			Assert.AreEqual("Length", pt.Name);
			Assert.AreEqual(typeof(int), pt.Type);
			pt = PropertyValue.TraverseProperties(obj, "publicField", "PropertyA", "Length");
			Assert.AreEqual("Length", pt.Name);
			Assert.AreEqual(typeof(int), pt.Type);
			pt = PropertyValue.TraverseProperties(obj, "publicField.PropertyA", "Length");
			Assert.AreEqual("Length", pt.Name);
			Assert.AreEqual(typeof(int), pt.Type);
			pt = PropertyValue.TraverseProperties(obj, "publicField/PropertyA\\Length");
			Assert.AreEqual("Length", pt.Name);
			Assert.AreEqual(typeof(int), pt.Type);
			pt = PropertyValue.TraverseProperties(obj, "publicField\\PropertyA", "Length");
			Assert.AreEqual("Length", pt.Name);
			Assert.AreEqual(typeof(int), pt.Type);
			
			pt = PropertyValue.TraverseProperties(obj, "publicField/PropertyA");
			Assert.AreEqual("PropertyA", pt.Name);
			Assert.AreEqual(typeof(string), pt.Type);

			Assert.AreEqual("test", pt.Value);
			pt.Value = "b";
			Assert.AreEqual("b", pt.Value);
		}
	}
}
