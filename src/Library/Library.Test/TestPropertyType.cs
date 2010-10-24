#region Copyright 2009-2010 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
	public partial class TestPropertyType
	{
		interface ia { string PropertyA { get; set; } }
		interface ib : ia { ia PropertyB { get; set; } }

		// Test classes:
		class a : ia 
		{
			string privateField = "a"; 
			[System.ComponentModel.DisplayName("Prop A")]
			public string PropertyA { get { return privateField; } set { privateField = value; } } 
		}
		class b : a, ib 
		{ 
			public ia publicField = new a();
			[System.ComponentModel.DisplayName("Prop B")]
			ia ib.PropertyB { get { return publicField; } set { publicField = value; } }
			ia PrivateProperty { get { return publicField; } set { publicField = value; } } 
		}

		[Test]
		public void TestPropertyTypePublicProperty()
		{
			PropertyType pt;

			//public property
			pt = new PropertyType(typeof(ia), "PropertyA");
			Assert.AreEqual("PropertyA", pt.Name);
			Assert.AreEqual(typeof(string), pt.Type);
		}
		[Test]
		public void TestPropertyTypePrivateProperty()
		{
			PropertyType pt;

			//private property
			pt = new PropertyType(typeof(b), "PrivateProperty");
			Assert.AreEqual("PrivateProperty", pt.Name);
			Assert.AreEqual(typeof(ia), pt.Type);
		}
		[Test]
		public void TestPropertyTypePublicField()
		{
			PropertyType pt;

			pt = new PropertyType(typeof(b), "publicField");
			Assert.AreEqual("publicField", pt.Name);
			Assert.AreEqual(typeof(ia), pt.Type);
		}
		[Test]
		public void TestPropertyTypePrivateField()
		{
			PropertyType pt;

			//private field
			pt = new PropertyType(typeof(a), "privateField");
			Assert.AreEqual("privateField", pt.Name);
			Assert.AreEqual(typeof(string), pt.Type);
		}
		[Test][ExpectedException(typeof(MissingMemberException))]
		public void TestPropertyTypeExplicitIntefaceProperty()
		{
			PropertyType pt;

			//PropertyB is an explicit interface implementation which does not 
			//generate a property on this class, as such it is not possible to
			//reflect directly via the class, but only via the interface.  Yes, 
			//the get_x and set_x methods are there, but no property.
			pt = new PropertyType(typeof(b), "PropertyB");
			Assert.AreEqual("PropertyB", pt.Name);
			Assert.AreEqual(typeof(ia), pt.Type);
		}
		[Test]
		public void TestPropertyTypeTraversals()
		{
			PropertyType pt;
			//Any of the following formats can be used
			pt = PropertyType.TraverseProperties(typeof(b), "publicField.PropertyA.Length");
			Assert.AreEqual("Length", pt.Name);
			Assert.AreEqual(typeof(int), pt.Type);
			pt = PropertyType.TraverseProperties(typeof(b), "publicField", "PropertyA", "Length");
			Assert.AreEqual("Length", pt.Name);
			Assert.AreEqual(typeof(int), pt.Type);
			pt = PropertyType.TraverseProperties(typeof(b), "publicField.PropertyA", "Length");
			Assert.AreEqual("Length", pt.Name);
			Assert.AreEqual(typeof(int), pt.Type);
			pt = PropertyType.TraverseProperties(typeof(b), "publicField/PropertyA\\Length");
			Assert.AreEqual("Length", pt.Name);
			Assert.AreEqual(typeof(int), pt.Type);
			pt = PropertyType.TraverseProperties(typeof(b), "publicField\\PropertyA", "Length");
			Assert.AreEqual("Length", pt.Name);
			Assert.AreEqual(typeof(int), pt.Type);
			pt = PropertyType.TraverseProperties(typeof(b), "publicField/PropertyA", "Length");
			Assert.AreEqual("Length", pt.Name);
			Assert.AreEqual(typeof(int), pt.Type);
		}
		[Test]
		public void TestPropertyTypeGetSet()
		{
			a classa = new a();
			classa.PropertyA = "hello";
			Assert.AreEqual("hello", classa.PropertyA);

			PropertyType pt = new PropertyType(classa.GetType(), "PropertyA");
			Assert.AreEqual("hello", pt.GetValue(classa));
			pt.SetValue(classa, "world");
			Assert.AreEqual("world", pt.GetValue(classa));
			Assert.AreEqual("world", classa.PropertyA);

			classa.PropertyA = "hello";//set privateField back to "hello"
			Assert.AreEqual("hello", classa.PropertyA);

			pt = new PropertyType(classa.GetType(), "privateField");
			Assert.AreEqual("hello", pt.GetValue(classa));
			pt.SetValue(classa, "world");
			Assert.AreEqual("world", pt.GetValue(classa));
			Assert.AreEqual("world", classa.PropertyA);
		}
		[Test]
		public void TestPropertyTypeAttributes()
		{
			PropertyType pt = new PropertyType(typeof(a), "PropertyA");
			Assert.IsTrue(pt.IsDefined(typeof(System.ComponentModel.DisplayNameAttribute), false));
			Assert.AreEqual(1, pt.GetCustomAttributes(false).Length);
			Assert.AreEqual(1, pt.GetCustomAttributes(typeof(System.ComponentModel.DisplayNameAttribute), false).Length);
			Assert.AreEqual(0, pt.GetCustomAttributes(typeof(System.ComponentModel.DesignOnlyAttribute), false).Length);
		}
	}
}
