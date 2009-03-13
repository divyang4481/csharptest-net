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
using NUnit.Framework;

#pragma warning disable 1591
namespace CSharpTest.Net.Shared.Test
{
	[TestFixture]
	[Category("TestCheck")]
	public partial class TestCheck
	{
		#region TestFixture SetUp/TearDown
		[TestFixtureSetUp]
		public virtual void Setup()
		{
		}

		[TestFixtureTearDown]
		public virtual void Teardown()
		{
		}
		#endregion

		[Test]
		public void Test()
		{
			Assert.AreEqual(this, Check.NotNull(this));

			List<TestCheck> items = new List<TestCheck>();
			items.Add(this);

			Assert.AreEqual(items, Check.NotEmpty(items));
			Assert.AreEqual("a", Check.NotEmpty("a"));
		}

		[Test]
		public void TestIsEqual()
		{
			Check.IsEqual(0, 0);
		}

		[Test]
		public void TestInstanceNotNull()
		{
			Check.NotNull(new object());
		}

		[Test]
		public void TestStringNotEmpty()
		{
			Check.NotEmpty("a");
		}

		[Test]
		public void TestStringNotEmpty2()
		{
			Check.NotEmpty("bcde");
		}

		[Test]
		public void TestCollNotEmpty()
		{
			Check.NotEmpty(new List<Object>(new object[1]));
		}

		[Test]
		public void TestCollNotEmpty2()
		{
			Check.NotEmpty(new object[2]);
		}

		//Negative Tests:

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void TestIsEqualError()
		{
			Check.IsEqual(0, 1);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void TestInstanceNotNullError()
		{
			Check.NotNull((TestCheck)null);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void TestStringNotEmptyError()
		{
			Check.NotEmpty((string)null);
		}

		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void TestStringNotEmpty2Error()
		{
			Check.NotEmpty("");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void TestCollNotEmptyError()
		{
			Check.NotEmpty((List<TestCheck>)null);
		}

		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void TestCollNotEmpty2Error()
		{
			Check.NotEmpty(new List<TestCheck>());
		}
	}
}
