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
using CSharpTest.Net.Utils;

#pragma warning disable 1591
namespace CSharpTest.Net.Shared.Test
{
	[TestFixture]
	[Category("TestArgumentList")]
	public partial class TestArgumentList
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
			ArgumentList args = new ArgumentList("-test=value", "/Test", "\"/other:value\"");
			Assert.AreEqual(2, args.Count);

			Assert.AreEqual(1, args[0].Count);
			Assert.AreEqual("test", args[0].Name);
			Assert.AreEqual("value", args[1].Value);

			Assert.AreEqual(1, args[1].Count);
			Assert.AreEqual("other", args[1].Name);
			Assert.AreEqual("value", args[1].Value);

			ArgumentList.DefaultComparison = StringComparer.Ordinal;
			Assert.AreEqual(StringComparer.Ordinal, ArgumentList.DefaultComparison);
			
			ArgumentList.NameDelimeters = new char[] { '=' };
			Assert.AreEqual('=', ArgumentList.NameDelimeters[0]);

			ArgumentList.PrefixChars = new char[] { '/' };
			Assert.AreEqual('/'	, ArgumentList.PrefixChars[0]);

			args = new ArgumentList("-test=value", "/Test", "\"/other:value\"");
			Assert.AreEqual(2, args.Count);
			Assert.AreEqual(0, args[0].Count);
			Assert.AreEqual("Test", args[0].Name);
			Assert.AreEqual(null, args[1].Value);

			Assert.AreEqual(1, args.Unnamed.Count);
			foreach(string sval in args.Unnamed)
				Assert.AreEqual("-test=value", sval);

			Assert.AreEqual(0, args[1].Count);
			Assert.AreEqual("other:value", args[1].Name);
			Assert.AreEqual(null, args[1].Value);

			args.Unnamed = new string[0];
			Assert.AreEqual(0, args.Unnamed.Count);

			args.Add("other", "value");
			Assert.AreEqual(null, (string)args["Test"]);
			Assert.AreEqual("value", (string)args["other"]);
			Assert.AreEqual("value", (string)args.SafeGet("other"));
			Assert.IsNotNull(args.SafeGet("other-not-existing"));
			Assert.AreEqual(null, (string)args.SafeGet("other-not-existing"));

			ArgumentList.Item item;

			args = new ArgumentList();
			Assert.AreEqual(0, args.Count);
			Assert.IsFalse(args.TryGetValue(String.Empty, out item));
			args.Add(String.Empty, null);
			Assert.IsTrue(args.TryGetValue(String.Empty, out item));

			string test = item;
			Assert.IsNull(test);

			string[] testarry = item;
			Assert.IsNotNull(testarry);
			Assert.AreEqual(0, testarry.Length);

			item.Value = "roger";
			Assert.AreEqual("roger", item.Value);
			Assert.AreEqual(1, item.Values.Length);
			Assert.AreEqual("roger", item.Values[0]);

			Assert.Contains("roger", item.ToArray());
			Assert.AreEqual(1, item.ToArray().Length);

			item.AddRange(new string[] { "wuz", "here" });
			Assert.AreEqual(3, item.Values.Length);
			Assert.AreEqual("roger wuz here", String.Join(" ", item));

			item.Values = new string[] { "roger", "was", "here" };
			Assert.AreEqual("roger was here", String.Join(" ", item));

			KeyValuePair<string, string[]> testkv = item;
			Assert.AreEqual(String.Empty, testkv.Key);
			Assert.AreEqual(3, testkv.Value.Length);
			Assert.AreEqual("roger was here", String.Join(" ", testkv.Value));
		}

	}

	[TestFixture]
	[Category("TestArgumentList")]
	public partial class TestArgumentListNegative
	{
		[Test]
		[ExpectedException(ExceptionType = typeof(ArgumentNullException))]
		public void TestCTor()
		{
			new ArgumentList((string[])null);
		}
		[Test]
		[ExpectedException(ExceptionType = typeof(ArgumentNullException))]
		public void TestDefaultComparison()
		{
			ArgumentList.DefaultComparison = null;
		}

		[Test]
		[ExpectedException(ExceptionType = typeof(ArgumentNullException))]
		public void TestNameDelimeters()
		{
			ArgumentList.NameDelimeters = null;
		}
		[Test]
		[ExpectedException(ExceptionType = typeof(ArgumentNullException))]
		public void TestPrefixChars()
		{
			ArgumentList.PrefixChars = null;
		}

		[Test]
		[ExpectedException(ExceptionType = typeof(ArgumentOutOfRangeException))]
		public void TestNameDelimeters2()
		{
			ArgumentList.NameDelimeters = new char[0];
		}
		[Test]
		[ExpectedException(ExceptionType = typeof(ArgumentOutOfRangeException))]
		public void TestPrefixChars2()
		{
			ArgumentList.PrefixChars = new char[0];
		}

		[Test]
		[ExpectedException(ExceptionType = typeof(ArgumentNullException))]
		public void TestAddRange()
		{
			new ArgumentList().AddRange(null);
		}
		[Test]
		[ExpectedException(ExceptionType = typeof(ArgumentNullException))]
		public void TestAddRange2()
		{
			new ArgumentList().AddRange(new string[] { "1", null, "2" });
		}
		[Test]
		[ExpectedException(ExceptionType = typeof(ArgumentNullException))]
		public void TestAdd()
		{
			new ArgumentList().Add(null, null);
		}
		[Test]
		[ExpectedException(ExceptionType = typeof(ArgumentNullException))]
		public void TestTryGetValue()
		{
			ArgumentList.Item item;
			new ArgumentList().TryGetValue(null, out item);
		}
		[Test]
		[ExpectedException(ExceptionType = typeof(ArgumentNullException))]
		public void TestValueAssignment()
		{
			ArgumentList.Item item = null;
			KeyValuePair<string, string[]> kv = item;
		}
		[Test]
		[ExpectedException(ExceptionType = typeof(ArgumentNullException))]
		public void TestItemNameNull()
		{
			ArgumentList.Item item = new ArgumentList.Item(null, null);
		}
	}
}
