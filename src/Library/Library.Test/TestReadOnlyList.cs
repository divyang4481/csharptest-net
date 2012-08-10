#region Copyright 2010-2012 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using CSharpTest.Net.Collections;
using System.Collections;

#pragma warning disable 1591

namespace CSharpTest.Net.Library.Test
{
	[TestFixture]
	[Category("TestReadOnlyList")]
	public partial class TestReadOnlyList
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
			List<string> strings = new List<string>(new string[] { "a", "b", "c" });
			ReadOnlyList<String> read = new ReadOnlyList<string>(strings);
			strings.Add("d");
			Assert.AreEqual(3, read.Count);
			Assert.IsTrue(read.Contains("a"));
			Assert.AreEqual(0, read.IndexOf("a"));
			Assert.IsTrue(read.Contains("b"));
			Assert.AreEqual(1, read.IndexOf("b"));
			Assert.IsTrue(read.Contains("c"));
			Assert.AreEqual(2, read.IndexOf("c"));
			Assert.IsFalse(read.Contains("d"));
			Assert.AreEqual(-1, read.IndexOf("d"));
			Assert.AreEqual("a,b,c", String.Join(",", read.ToArray()));
			Assert.AreEqual("a,b,c", String.Join(",", new List<String>(read).ToArray()));
			
			string[] arcopy = new string[3];
			read.CopyTo(arcopy, 0);
			Assert.AreEqual("a,b,c", String.Join(",", arcopy));

			System.Collections.IEnumerator en = ((System.Collections.IEnumerable)read).GetEnumerator();
			Assert.IsTrue(en.MoveNext());
			Assert.AreEqual("a", en.Current);
			Assert.IsTrue(en.MoveNext());
			Assert.AreEqual("b", en.Current);
			Assert.IsTrue(en.MoveNext());
			Assert.AreEqual("c", en.Current);
			Assert.IsFalse(en.MoveNext());
		}

        [Test]
        public void TestModifyMaster()
        {
            List<string> strings = new List<string>(new string[] { "a", "b", "c" });
            ReadOnlyList<String> read = new ReadOnlyList<string>(strings, false);

            Assert.AreEqual(3, read.Count);
            strings.RemoveAt(2);
            Assert.AreEqual(2, read.Count);
            Assert.AreEqual("a", read[0]);
            Assert.AreEqual("b", read[1]);

            Assert.AreEqual("a,b", String.Join(",", read.Clone().ToArray()));
        }

        [Test]
        public void TestICollection()
        {
            ICollection read = new ReadOnlyList<int>((IEnumerable<int>)new int[] { 5, 10, 15 });
            Assert.AreEqual(3, read.Count);
            Assert.IsFalse(read.IsSynchronized);
            Assert.IsTrue(Object.ReferenceEquals(read, read.SyncRoot));

            long[] lary = new long[3];
            read.CopyTo(lary, 0);
            Assert.AreEqual(5L, lary[0]);
            Assert.AreEqual(10L, lary[1]);
            Assert.AreEqual(15L, lary[2]);

            ICollection copy = (ICollection)((ICloneable)read).Clone();
            Assert.AreEqual(3, copy.Count);
        }
	}
}
