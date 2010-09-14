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
        public void TestAsserts()
        {
            //pass all
            Check.Assert(true, delegate() { Assert.Fail(); return (Exception)null; });
            Check.Assert<Exception>(true);
            Check.Assert<Exception>(true, String.Empty);
            Check.Assert<Exception>(true, String.Empty, new Exception());
        }

        [Test]
        public void TestArraySizes()
        {
            Check.ArraySize(new byte[0], 0, 0);
            Check.ArraySize(new byte[1], 1, 25);
            Check.ArraySize(new byte[25], 1, 25);
        }

        [Test]
        public void TestInRange()
        {
            Check.InRange(0, 0, 0);
            Check.InRange(1, 1, 25);
            Check.InRange(25, 1, 25);
        }

		[Test]
		public void TestIsEqual()
		{
			Check.IsEqual(0, 0);
		}

		[Test]
		public void TestNotEqual()
		{
			Check.NotEqual(1, 0);
		}

		[Test]
		public void TestGuidNotEmpty()
		{
			Check.NotEmpty(Guid.NewGuid());
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

		/// <summary>
		/// Test classes for IsAssignable
		/// </summary>
		class a : ia { }
		struct at : ia { }
		class b : a, ib { }
		interface ia { }
		interface ib : ia { }

		[Test]
		public void TestIsAssignable()
		{
			Check.IsAssignable(typeof(ia), typeof(ib));
			Check.IsAssignable(typeof(ib), typeof(ib));
			Check.IsAssignable(typeof(object), typeof(int));
			Check.IsAssignable(typeof(ia), typeof(at));
			Check.IsAssignable(typeof(at), typeof(at));
			
			/*
			 * Note: these fail because they will not cast:
			Check.IsAssignable(typeof(int), typeof(short));
			Check.IsAssignable(typeof(int), typeof(System.DayOfWeek));
			 */
		}

		[Test]
		public void TestIsAssignableTemplated()
		{
			b b1 = new b();
			a a1 = new a();
			at at1 = new at();
			ia ianull = null;
			a anull = null;

			Assert.AreEqual(b1, Check.IsAssignable<a>(b1));
			Assert.AreEqual(null, Check.IsAssignable<a>(ianull));
			Assert.AreEqual(null, Check.IsAssignable<ia>(anull));
			Assert.AreEqual(at1, Check.IsAssignable<ia>(at1));
			Assert.AreEqual(at1, Check.IsAssignable<at>(at1));
			Assert.AreEqual(b1, Check.IsAssignable<ia>(b1));
			Assert.AreEqual(b1, Check.IsAssignable<ib>(b1));
			Assert.AreEqual(b1, Check.IsAssignable<b>(b1));
			Assert.AreEqual(null, Check.IsAssignable<string>(null));
			Assert.AreEqual(String.Empty, Check.IsAssignable<System.Collections.IEnumerable>(String.Empty));
		}

		[Test]
		public void TestIsAssignableByType()
		{
			b b1 = new b();
			a a1 = new a();
			at at1 = new at();
			ia ianull = null;
			a anull = null;

			Assert.AreEqual(b1, Check.IsAssignable(typeof(a), b1));
			Assert.AreEqual(null, Check.IsAssignable(typeof(a), ianull));
			Assert.AreEqual(null, Check.IsAssignable(typeof(ia), anull));
			Assert.AreEqual(at1, Check.IsAssignable(typeof(ia), at1));
			Assert.AreEqual(at1, Check.IsAssignable(typeof(at), at1));
			Assert.AreEqual(b1, Check.IsAssignable(typeof(ia), b1));
			Assert.AreEqual(b1, Check.IsAssignable(typeof(ib), b1));
			Assert.AreEqual(b1, Check.IsAssignable(typeof(b), b1));
			Assert.AreEqual(null, Check.IsAssignable(typeof(string), (object)null));
			Assert.AreEqual(String.Empty, Check.IsAssignable(typeof(System.Collections.IEnumerable), String.Empty));
		}

		//Negative Tests:

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void TestIsAssignableTypesCantBeNull()
		{
			Check.IsAssignable<at>(null);
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void TestIsAssignableTypesCantBeNull2()
		{
			Check.IsAssignable<int>(null);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void TestIsAssignableNullType()
		{
			Check.IsAssignable((Type)null, typeof(int));
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void TestIsAssignableNullType2()
		{
			Check.IsAssignable(typeof(int), (Type)null);
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void TestIsAssignableBadCast1()
		{
			Check.IsAssignable<ib>(new a());
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void TestIsAssignableBadCast2()
		{
			Check.IsAssignable<b>(new a());
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void TestIsAssignableBadCast3()
		{
			Check.IsAssignable<int>(new a());
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void TestIsAssignableBadCast4()
		{
			Check.IsAssignable<a>(5);
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void TestIsAssignableBadCast5()
		{
			Check.IsAssignable(typeof(ib), typeof(ia));
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void TestIsEqualError()
		{
			Check.IsEqual(0, 1);
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void TestNotEqualError()
		{
			Check.NotEqual(0, 0);
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
        
        [Test, ExpectedException(typeof(InsufficientMemoryException))]
        public void TestAssertFail()
        {
            Check.Assert<InsufficientMemoryException>(false);
        }

        [Test, ExpectedException(typeof(Exception), ExpectedMessage = "This is the message")]
        public void TestAssertFailWithMessage()
        {
            Check.Assert<Exception>(false, "This is the message");
        }

        [Test, ExpectedException(typeof(ApplicationException))]
        public void TestAssertFailWithInner()
        {
            try { Check.Assert<ApplicationException>(false, "Application Message", new Exception("INNER")); }
            catch (ApplicationException ae)
            {
                Assert.AreEqual("Application Message", ae.Message);
                Assert.IsNotNull(ae.InnerException);
                Assert.AreEqual(typeof(Exception), ae.InnerException.GetType());
                Assert.AreEqual("INNER", ae.InnerException.Message);
                throw;
            }
        }

        [Test, ExpectedException(typeof(ApplicationException), ExpectedMessage = "Custom Message")]
        public void TestAssertFailWithCustom()
        {
            Check.Assert(false, delegate() { return new ApplicationException("Custom Message"); });
        }

        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestArrayNull()
        {
            Check.ArraySize<byte[]>(null, 0, 0);
        }

        [Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestArrayBelowMin()
        {
            Check.ArraySize(new byte[0], 1, 1);
        }

        [Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestArrayAboveMax()
        {
            Check.ArraySize(new byte[2], 1, 1);
        }

        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestInRangeNull()
        {
            Check.InRange<string>(null, "a", "b");
        }

        [Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestInRangeBelowMin()
        {
            Check.InRange(0, 1, 1);
        }

        [Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestInRangeAboveMax()
        {
            Check.InRange(2, 1, 1);
		}

		[Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void TestGuidIsEmpty()
		{
			Check.NotEmpty(Guid.Empty);
		}
	}
}
