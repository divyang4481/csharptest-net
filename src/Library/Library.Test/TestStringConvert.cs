#region Copyright 2009-2012 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
namespace CSharpTest.Net.Library.Test
{
	[TestFixture]
	[Category("TestStringConvert")]
	public partial class TestStringConvert
	{
		StringConverter Convert = new StringConverter();

		[Test]
		public void TestBasicConverts()
		{
			AssertConvert(1);
			AssertConvert<bool>(true, false);
			AssertConvert<byte>(2, 0, 1, byte.MinValue, byte.MaxValue);
			AssertConvert<sbyte>(-2, 0, 1, sbyte.MinValue, sbyte.MaxValue);
			AssertConvert<char>('Z', 'a', ' ', (char)1255, char.MaxValue, char.MinValue);
			AssertConvert<DateTime>(new DateTime(1999, 12, 31, 11, 59, 59, 999, DateTimeKind.Local), DateTime.MaxValue, DateTime.MinValue);
			AssertConvert<TimeSpan>(new TimeSpan(11, 22, 33, 44, 55), new TimeSpan(0), TimeSpan.MaxValue, TimeSpan.MinValue);
			AssertConvert<decimal>(9999999999999999999999999999m, 1m / 3m, decimal.One, decimal.Zero, decimal.MinusOne, decimal.MinValue, decimal.MaxValue);
			AssertConvert<double>(1.0000000000001, double.Epsilon, double.NaN, double.PositiveInfinity, double.NegativeInfinity, double.MinValue, double.MaxValue);
			AssertConvert<float>(1.0000001f, float.Epsilon, float.NaN, float.PositiveInfinity, float.NegativeInfinity, float.MinValue, float.MaxValue);
			AssertConvert<Guid>(new Guid("ca761232ed4211cebacd00aa0057b223"), Guid.Empty);
			AssertConvert<Uri>(new Uri("file://localhost/c$/windows"), new Uri("http://csharptest.net"));
			AssertConvert<short>(0, short.MinValue, short.MaxValue);
			AssertConvert<ushort>(0, ushort.MinValue, ushort.MaxValue);
			AssertConvert<int>(0, int.MinValue, int.MaxValue);
			AssertConvert<uint>(0, uint.MinValue, uint.MaxValue);
			AssertConvert<long>(0, long.MinValue, long.MaxValue);
			AssertConvert<ulong>(0, ulong.MinValue, ulong.MaxValue);
			AssertConvert<string>(String.Empty, "abc");
			AssertConvert<Version>(new Version(), new Version(1, 0), new Version(1,2), new Version(1, 2, 3), new Version(1,2,3,4), 
				new Version(ushort.MaxValue,ushort.MaxValue,ushort.MaxValue,ushort.MaxValue),
				new Version(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue));
		}

		private void AssertConvert<T>(params T[] valueset)
		{
			string sval;
			object oval;
			foreach (T value in valueset)
			{
				try
				{
					sval = Convert.ToString<T>(value);
					Assert.AreEqual(value, Convert.FromString<T>(sval), "Template convert " + typeof(T).Name);

					sval = Convert.ToString((object)value);
					Assert.IsTrue(Convert.TryParse(sval, typeof(T), out oval));
					Assert.AreEqual((object)value, oval, "Object convert " + typeof(T).Name);
				}
				catch (Exception e)
				{
					throw new ApplicationException(String.Format("failed to convert value '{0}' of type {1}", value, typeof(T)), e);
				}
			}
		}
		
		[Test]
		public void TestBadConverts()
		{
			AssertNoConvert<bool>(null, String.Empty, "a", "1.1", "1", "0", "yes", "false!");
			AssertNoConvert<byte>(null, String.Empty, "a", "1.1", "256", "-1");
			AssertNoConvert<sbyte>(null, String.Empty, "a", "1.1", "128", "-129");
			AssertNoConvert<char>(null, String.Empty);
			AssertNoConvert<DateTime>(null, String.Empty, "Jan 5, 2009", "11/23/2008", "a", "34895364");
			AssertNoConvert<TimeSpan>(null, String.Empty, "23905350");
			AssertNoConvert<decimal>(null, String.Empty, "a", "1z", "$1 ");
			AssertNoConvert<double>(null, String.Empty, "a", "1z", "1 ");
			AssertNoConvert<float>(null, String.Empty, "a", "1z", "1 ");
			AssertNoConvert<Guid>(null, String.Empty, "{ca761232ed4211ce-bacd-00aa0057b223}", "ca761232-ed42-11ce-bacd-00aa0057b223}", "{ca761232-ed42-11ce-bacd-00aa0057b223");
			AssertNoConvert<Uri>(null, String.Empty, ".");
			AssertNoConvert<short>(null, String.Empty, "a", "1.1", "32768", "-32769");
			AssertNoConvert<ushort>(null, String.Empty, "a", "1.1", "65536", "-1");
			AssertNoConvert<int>(null, String.Empty, "a", "1.1", "2147483648", "-2147483649");
			AssertNoConvert<uint>(null, String.Empty, "a", "1.1", "4347483648", "-1");
			AssertNoConvert<long>(null, String.Empty, "a", "1.1", "9223372036854775808", "-9223372036854775809");
			AssertNoConvert<ulong>(null, String.Empty, "a", "1.1", "18446744073709551616", "-1");
			AssertNoConvert<string>((string)null);
			AssertNoConvert<Version>(null, String.Empty, "a", "0", "-1", "1", "1.1.1.2999999999");
			AssertNoConvert<Version>("1.1.1.2999999999");
		}

		private void AssertNoConvert<T>(params string[] strings)
		{
			T tval;
			object oval;
			foreach (string sval in strings)
			{
				try
				{
					Assert.IsFalse(Convert.TryParse<T>(sval, out tval));
					Assert.IsFalse(Convert.TryParse(sval, typeof(T), out oval));
				}
				catch (Exception e)
				{
					throw new ApplicationException(String.Format("Value '{0}' is not of type {1}", sval, typeof(T)), e);
				}
			}
		}

		[Test]
		public void TestReplacedConverter()
		{
			DateTime date = new DateTime(2000, 1,2,3,4,5,6);
			string dt = date.ToString();

			StringConverter c = new StringConverter();

			Assert.AreNotEqual(dt, c.ToString(date));

			c.Add<DateTime>(DateTime.TryParse, delegate(DateTime value) { return value.ToString(); });

			Assert.AreEqual(dt, c.ToString(date));
		}
	}

	[TestFixture]
	[Category("TestStringConvert")]
	public partial class TestStringConvertNegative
	{
		StringConverter Convert = new StringConverter();

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void TestNullString()
		{
			Convert.FromString<int>(null);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void TestNullType()
		{
			object value;
			Convert.TryParse(String.Empty, null, out value);
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void TestFromStringBadConvert()
		{
			Convert.FromString<int>(String.Empty);
		}

		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void TestNoConverterForType()
		{
			Convert.ToString(new object());
		}

		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void TestNoConverterForType2()
		{
			Convert.ToString<object>(new object());
		}

		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void TestTryParseNoConverterForType()
		{
			object oval;
			Convert.TryParse<object>(String.Empty, out oval);
		}

		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void TestTryParseNoConverterForType2()
		{
			object oval;
			Convert.TryParse(String.Empty, typeof(object), out oval);
		}

		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void TestRemovedConverterForType()
		{
			StringConverter c = new StringConverter();

			Assert.AreEqual(5, c.FromString<int>("5"));
			c.Remove<int>();

			c.FromString<int>("5");
			Assert.Fail();
		}
	}
}
