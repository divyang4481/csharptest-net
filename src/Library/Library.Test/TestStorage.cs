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
using CSharpTest.Net.Serialization;
using CSharpTest.Net.Serialization.StorageClasses;

#pragma warning disable 1591
namespace CSharpTest.Net.Library.Test
{
	[TestFixture]
	[Category("TestStorage")]
	public partial class TestStorage
	{
		protected Storage Store;
		#region TestValues
		class TestValues1
		{
			public readonly bool _bool = true;
			public readonly byte _byte = byte.MaxValue;
			public readonly char _char = char.MaxValue;
			public readonly DateTime _DateTime = DateTime.MaxValue;
			public readonly decimal _decimal = decimal.MaxValue;
			public readonly double _double = double.MaxValue;
			public readonly float _float = float.MaxValue;
			public readonly Guid _Guid = Guid.NewGuid();
			public readonly int _int = int.MaxValue;
			public readonly long _long = long.MaxValue;
			public readonly sbyte _sbyte = sbyte.MaxValue;
			public readonly short _short = short.MaxValue;
			public readonly string _string = "test";
			public readonly TimeSpan _TimeSpan = TimeSpan.MaxValue;
			public readonly uint _uint = uint.MaxValue;
			public readonly ulong _ulong = ulong.MaxValue;
			public readonly Uri _Uri = new Uri("http://csharptest.net/Projects");
			public readonly ushort _ushort = ushort.MaxValue;
			public readonly Version _Version = new Version(1, 2, 3, 4);
			public readonly object _object = 1.234;
		}
		class TestValues2
		{
			public readonly bool _bool = false;
			public readonly byte _byte = byte.MinValue;
			public readonly char _char = char.MinValue;
			public readonly DateTime _DateTime = DateTime.MinValue;
			public readonly decimal _decimal = decimal.MinValue;
			public readonly double _double = double.MinValue;
			public readonly float _float = float.MinValue;
			public readonly Guid _Guid = Guid.NewGuid();
			public readonly int _int = int.MinValue;
			public readonly long _long = long.MinValue;
			public readonly sbyte _sbyte = sbyte.MinValue;
			public readonly short _short = short.MinValue;
			public readonly string _string = "different";
			public readonly TimeSpan _TimeSpan = TimeSpan.MinValue;
			public readonly uint _uint = uint.MinValue;
			public readonly ulong _ulong = ulong.MinValue;
			public readonly Uri _Uri = new Uri("http://csharptest.net/Blog");
			public readonly ushort _ushort = ushort.MinValue;
			public readonly Version _Version = new Version(4, 3, 2, 1);
			public readonly object _object = 4.321;
		}
		class TestValues
		{
			public bool _bool;//false;
			public byte _byte;//byte.MinValue;
			public char _char;//char.MinValue;
			public DateTime _DateTime;//DateTime.MinValue;
			public decimal _decimal;//decimal.MinValue;
			public double _double;//double.MinValue;
			public float _float;//float.MinValue;
			public Guid _Guid;//Guid.NewGuid();
			public int _int;//int.MinValue;
			public long _long;//long.MinValue;
			public sbyte _sbyte;//sbyte.MinValue;
			public short _short;//short.MinValue;
			public string _string;//"different";
			public TimeSpan _TimeSpan;//TimeSpan.MinValue;
			public uint _uint;//uint.MinValue;
			public ulong _ulong;//ulong.MinValue;
			public Uri _Uri;//new Uri("http://csharptest.net/Blog");
			public ushort _ushort;//ushort.MinValue;
			public Version _Version;//new Version(4, 3, 2, 1);
			public object _object;//4.321;
		}
		#endregion

		private TestValues1 ValuesA = new TestValues1();
		private TestValues2 ValuesB = new TestValues2();

		[SetUp]
		public virtual void Setup()
		{
			Store = new Storage(new DictionaryStorage(new Dictionary<string, string>()));
		}

		[Test]
		public void TestPath()
		{
			Assert.IsNull(Store.ContextPath);

			using (Store.SetContext("a"))
			{
				Assert.AreEqual("a", Store.ContextPath);
				using (Store.SetContext("b"))
					Assert.AreEqual("b", Store.ContextPath);
				Assert.AreEqual("a", Store.ContextPath);
			}

			Assert.IsNull(Store.ContextPath);

			using (Store.SetContext("a"))
			{
				Assert.AreEqual("a", Store.ContextPath);
				Store.SetContext("b");
				Assert.AreEqual("b", Store.ContextPath);
			}

			Assert.IsNull(Store.ContextPath);
		}

		[Test]
		public void TestDelete()
		{
			Version data = new Version(1, 2, 3, 4);
			Store.SetValue("a", data);
			Assert.AreEqual(data, Store.GetValue("a", (Version)null));

			Store.Delete("a");
			Assert.AreEqual(new Version(1, 0), Store.GetValue("a", new Version(1, 0)));

			Store.SetValue("a", data);
			Assert.AreEqual(data, Store.GetValue("a", (Version)null));
		
			Store.SetValue("a", (string)null);//set null will delete
			Assert.AreEqual(new Version(1, 0), Store.GetValue("a", new Version(1, 0)));
		}

		[Test]
		public void TestDefault()
		{
			Version data = new Version(1, 2, 3, 4);
			Version v2;
			Assert.IsFalse(Store.TryGetValue("v", out v2));
			Assert.AreEqual(data, Store.GetValue("v", data));
		}

		[Test]
		public void TestGetValueDefault()
		{
			Assert.AreEqual(ValuesA._bool, Store.GetValue("name", ValuesA._bool));
			Assert.AreEqual(ValuesA._byte, Store.GetValue("name", ValuesA._byte));
			Assert.AreEqual(ValuesA._char, Store.GetValue("name", ValuesA._char));
			Assert.AreEqual(ValuesA._DateTime, Store.GetValue("name", ValuesA._DateTime));
			Assert.AreEqual(ValuesA._decimal, Store.GetValue("name", ValuesA._decimal));
			Assert.AreEqual(ValuesA._double, Store.GetValue("name", ValuesA._double));
			Assert.AreEqual(ValuesA._float, Store.GetValue("name", ValuesA._float));
			Assert.AreEqual(ValuesA._Guid, Store.GetValue("name", ValuesA._Guid));
			Assert.AreEqual(ValuesA._int, Store.GetValue("name", ValuesA._int));
			Assert.AreEqual(ValuesA._long, Store.GetValue("name", ValuesA._long));
			Assert.AreEqual(ValuesA._sbyte, Store.GetValue("name", ValuesA._sbyte));
			Assert.AreEqual(ValuesA._short, Store.GetValue("name", ValuesA._short));
			Assert.AreEqual(ValuesA._string, Store.GetValue("name", ValuesA._string));
			Assert.AreEqual(ValuesA._TimeSpan, Store.GetValue("name", ValuesA._TimeSpan));
			Assert.AreEqual(ValuesA._uint, Store.GetValue("name", ValuesA._uint));
			Assert.AreEqual(ValuesA._ulong, Store.GetValue("name", ValuesA._ulong));
			Assert.AreEqual(ValuesA._Uri, Store.GetValue("name", ValuesA._Uri));
			Assert.AreEqual(ValuesA._ushort, Store.GetValue("name", ValuesA._ushort));
			Assert.AreEqual(ValuesA._Version, Store.GetValue("name", ValuesA._Version));
			Assert.AreEqual(ValuesA._object, Store.GetValue("name", ValuesA._object.GetType(), ValuesA._object));
		}

		[Test]
		public void TestTryGetValueFalse()
		{
			TestValues values = new TestValues();

			Assert.IsFalse(Store.TryGetValue("name", out values._bool));
			Assert.IsFalse(Store.TryGetValue("name", out values._byte));
			Assert.IsFalse(Store.TryGetValue("name", out values._char));
			Assert.IsFalse(Store.TryGetValue("name", out values._DateTime));
			Assert.IsFalse(Store.TryGetValue("name", out values._decimal));
			Assert.IsFalse(Store.TryGetValue("name", out values._double));
			Assert.IsFalse(Store.TryGetValue("name", out values._float));
			Assert.IsFalse(Store.TryGetValue("name", out values._Guid));
			Assert.IsFalse(Store.TryGetValue("name", out values._int));
			Assert.IsFalse(Store.TryGetValue("name", out values._long));
			Assert.IsFalse(Store.TryGetValue("name", out values._sbyte));
			Assert.IsFalse(Store.TryGetValue("name", out values._short));
			Assert.IsFalse(Store.TryGetValue("name", out values._string));
			Assert.IsFalse(Store.TryGetValue("name", out values._TimeSpan));
			Assert.IsFalse(Store.TryGetValue("name", out values._uint));
			Assert.IsFalse(Store.TryGetValue("name", out values._ulong));
			Assert.IsFalse(Store.TryGetValue("name", out values._Uri));
			Assert.IsFalse(Store.TryGetValue("name", out values._ushort));
			Assert.IsFalse(Store.TryGetValue("name", out values._Version));
			Assert.IsFalse(Store.TryGetValue("name", ValuesA._object.GetType(), out values._object));
		}

		[Test]
		public void TestSetValue()
		{
			//Part 1, write
			Store.SetValue("bool", ValuesA._bool);
			Store.SetValue("byte", ValuesA._byte);
			Store.SetValue("char", ValuesA._char);
			Store.SetValue("DateTime", ValuesA._DateTime);
			Store.SetValue("decimal", ValuesA._decimal);
			Store.SetValue("double", ValuesA._double);
			Store.SetValue("float", ValuesA._float);
			Store.SetValue("Guid", ValuesA._Guid);
			Store.SetValue("int", ValuesA._int);
			Store.SetValue("long", ValuesA._long);
			Store.SetValue("sbyte", ValuesA._sbyte);
			Store.SetValue("short", ValuesA._short);
			Store.SetValue("string", ValuesA._string);
			Store.SetValue("TimeSpan", ValuesA._TimeSpan);
			Store.SetValue("uint", ValuesA._uint);
			Store.SetValue("ulong", ValuesA._ulong);
			Store.SetValue("Uri", ValuesA._Uri);
			Store.SetValue("ushort", ValuesA._ushort);
			Store.SetValue("Version", ValuesA._Version);
			Store.SetValue("object", ValuesA._object.GetType(), ValuesA._object);

			//Part 2, read
			TestValues values = new TestValues();

			Assert.IsTrue(Store.TryGetValue("bool", out values._bool));
			Assert.IsTrue(Store.TryGetValue("byte", out values._byte));
			Assert.IsTrue(Store.TryGetValue("char", out values._char));
			Assert.IsTrue(Store.TryGetValue("DateTime", out values._DateTime));
			Assert.IsTrue(Store.TryGetValue("decimal", out values._decimal));
			Assert.IsTrue(Store.TryGetValue("double", out values._double));
			Assert.IsTrue(Store.TryGetValue("float", out values._float));
			Assert.IsTrue(Store.TryGetValue("Guid", out values._Guid));
			Assert.IsTrue(Store.TryGetValue("int", out values._int));
			Assert.IsTrue(Store.TryGetValue("long", out values._long));
			Assert.IsTrue(Store.TryGetValue("sbyte", out values._sbyte));
			Assert.IsTrue(Store.TryGetValue("short", out values._short));
			Assert.IsTrue(Store.TryGetValue("string", out values._string));
			Assert.IsTrue(Store.TryGetValue("TimeSpan", out values._TimeSpan));
			Assert.IsTrue(Store.TryGetValue("uint", out values._uint));
			Assert.IsTrue(Store.TryGetValue("ulong", out values._ulong));
			Assert.IsTrue(Store.TryGetValue("Uri", out values._Uri));
			Assert.IsTrue(Store.TryGetValue("ushort", out values._ushort));
			Assert.IsTrue(Store.TryGetValue("Version", out values._Version));
			Assert.IsTrue(Store.TryGetValue("object", ValuesA._object.GetType(), out values._object));

			//Part 3, assert
			Assert.AreEqual(ValuesA._bool, values._bool);
			Assert.AreEqual(ValuesA._byte, values._byte);
			Assert.AreEqual(ValuesA._char, values._char);
			Assert.AreEqual(ValuesA._DateTime, values._DateTime);
			Assert.AreEqual(ValuesA._decimal, values._decimal);
			Assert.AreEqual(ValuesA._double, values._double);
			Assert.AreEqual(ValuesA._float, values._float);
			Assert.AreEqual(ValuesA._Guid, values._Guid);
			Assert.AreEqual(ValuesA._int, values._int);
			Assert.AreEqual(ValuesA._long, values._long);
			Assert.AreEqual(ValuesA._sbyte, values._sbyte);
			Assert.AreEqual(ValuesA._short, values._short);
			Assert.AreEqual(ValuesA._string, values._string);
			Assert.AreEqual(ValuesA._TimeSpan, values._TimeSpan);
			Assert.AreEqual(ValuesA._uint, values._uint);
			Assert.AreEqual(ValuesA._ulong, values._ulong);
			Assert.AreEqual(ValuesA._Uri, values._Uri);
			Assert.AreEqual(ValuesA._ushort, values._ushort);
			Assert.AreEqual(ValuesA._Version, values._Version);
			Assert.AreEqual(ValuesA._object, values._object);

			//Part 4, non-default GetValue
			Assert.AreEqual(ValuesA._bool, Store.GetValue("bool", ValuesB._bool));
			Assert.AreEqual(ValuesA._byte, Store.GetValue("byte", ValuesB._byte));
			Assert.AreEqual(ValuesA._char, Store.GetValue("char", ValuesB._char));
			Assert.AreEqual(ValuesA._DateTime, Store.GetValue("DateTime", ValuesB._DateTime));
			Assert.AreEqual(ValuesA._decimal, Store.GetValue("decimal", ValuesB._decimal));
			Assert.AreEqual(ValuesA._double, Store.GetValue("double", ValuesB._double));
			Assert.AreEqual(ValuesA._float, Store.GetValue("float", ValuesB._float));
			Assert.AreEqual(ValuesA._Guid, Store.GetValue("Guid", ValuesB._Guid));
			Assert.AreEqual(ValuesA._int, Store.GetValue("int", ValuesB._int));
			Assert.AreEqual(ValuesA._long, Store.GetValue("long", ValuesB._long));
			Assert.AreEqual(ValuesA._sbyte, Store.GetValue("sbyte", ValuesB._sbyte));
			Assert.AreEqual(ValuesA._short, Store.GetValue("short", ValuesB._short));
			Assert.AreEqual(ValuesA._string, Store.GetValue("string", ValuesB._string));
			Assert.AreEqual(ValuesA._TimeSpan, Store.GetValue("TimeSpan", ValuesB._TimeSpan));
			Assert.AreEqual(ValuesA._uint, Store.GetValue("uint", ValuesB._uint));
			Assert.AreEqual(ValuesA._ulong, Store.GetValue("ulong", ValuesB._ulong));
			Assert.AreEqual(ValuesA._Uri, Store.GetValue("Uri", ValuesB._Uri));
			Assert.AreEqual(ValuesA._ushort, Store.GetValue("ushort", ValuesB._ushort));
			Assert.AreEqual(ValuesA._Version, Store.GetValue("Version", ValuesB._Version));
			Assert.AreEqual(ValuesA._object, Store.GetValue("object", ValuesB._object.GetType(), ValuesB._object));
		}

		[Test]
		public void TestTryBadValues()
		{
			TestValues values = new TestValues();
			
			//fails all but string
			Store.SetValue("name", String.Empty);
			Assert.IsTrue(Store.TryGetValue("name", out values._string));
			Assert.AreEqual(String.Empty, values._string);

			Assert.IsFalse(Store.TryGetValue("name", out values._bool));
			Assert.IsFalse(Store.TryGetValue("name", out values._byte));
			Assert.IsFalse(Store.TryGetValue("name", out values._char));
			Assert.IsFalse(Store.TryGetValue("name", out values._DateTime));
			Assert.IsFalse(Store.TryGetValue("name", out values._decimal));
			Assert.IsFalse(Store.TryGetValue("name", out values._double));
			Assert.IsFalse(Store.TryGetValue("name", out values._float));
			Assert.IsFalse(Store.TryGetValue("name", out values._Guid));
			Assert.IsFalse(Store.TryGetValue("name", out values._int));
			Assert.IsFalse(Store.TryGetValue("name", out values._long));
			Assert.IsFalse(Store.TryGetValue("name", out values._sbyte));
			Assert.IsFalse(Store.TryGetValue("name", out values._short));
			Assert.IsFalse(Store.TryGetValue("name", out values._TimeSpan));
			Assert.IsFalse(Store.TryGetValue("name", out values._uint));
			Assert.IsFalse(Store.TryGetValue("name", out values._ulong));
			Assert.IsFalse(Store.TryGetValue("name", out values._Uri));
			Assert.IsFalse(Store.TryGetValue("name", out values._ushort));
			Assert.IsFalse(Store.TryGetValue("name", out values._Version));
			Assert.IsFalse(Store.TryGetValue("name", ValuesA._object.GetType(), out values._object));
		}

	}
}