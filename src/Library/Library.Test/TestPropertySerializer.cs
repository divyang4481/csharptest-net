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
using CSharpTest.Net.Serialization;
using CSharpTest.Net.Serialization.StorageClasses;
using System.Reflection;

#pragma warning disable 1591
#pragma warning disable 649 // is never assigned to

namespace CSharpTest.Net.Library.Test
{
	[TestFixture]
	public partial class TestPropertySerializer
	{
		protected INameValueStore Dictionary;
		protected Storage Store;
		#region TestValues
		static readonly string[] ValueNames = new string[] 
		{
			"_bool",
			"_byte",
			"_char",
			"_DateTime",
			"_decimal",
			"_double",
			"_float",
			"_Guid",
			"_int",
			"_long",
			"_sbyte",
			"_short",
			"_string",
			"_TimeSpan",
			"_uint",
			"_ulong",
			"_Uri",
			"_ushort",
			"_Version",
			"_object"
		};
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
			Store = new Storage(Dictionary = new DictionaryStorage(new Dictionary<string, string>()));
		}

		[Test]
		public void TestSerialize()
		{
			PropertySerializer ser = new PropertySerializer(typeof(TestValues1), ValueNames);
			ser.ContinueOnError = false;
			Assert.AreEqual(false, ser.ContinueOnError);

			ser.Serialize(ValuesA, Dictionary);

			TestValues test = new TestValues();
			ser.Deserialize(test, Dictionary);

			Assert.AreEqual(ValuesA._bool, test._bool);
			Assert.AreEqual(ValuesA._byte, test._byte);
			Assert.AreEqual(ValuesA._char, test._char);
			Assert.AreEqual(ValuesA._DateTime, test._DateTime);
			Assert.AreEqual(ValuesA._decimal, test._decimal);
			Assert.AreEqual(ValuesA._double, test._double);
			Assert.AreEqual(ValuesA._float, test._float);
			Assert.AreEqual(ValuesA._Guid, test._Guid);
			Assert.AreEqual(ValuesA._int, test._int);
			Assert.AreEqual(ValuesA._long, test._long);
			Assert.AreEqual(ValuesA._sbyte, test._sbyte);
			Assert.AreEqual(ValuesA._short, test._short);
			Assert.AreEqual(ValuesA._string, test._string);
			Assert.AreEqual(ValuesA._TimeSpan, test._TimeSpan);
			Assert.AreEqual(ValuesA._uint, test._uint);
			Assert.AreEqual(ValuesA._ulong, test._ulong);
			Assert.AreEqual(ValuesA._Uri, test._Uri);
			Assert.AreEqual(ValuesA._ushort, test._ushort);
			Assert.AreEqual(ValuesA._Version, test._Version);

			//ROK - note, it can not deserialize this since it does not know the type:
			Assert.AreEqual(null, test._object);
		}

		public class HaveReadOnly
		{
			public string Value;

			public string ReadOnly { get { return Value; } }
			public string WriteOnly { set { Value = value; } }
		}

		[Test][ExpectedException(typeof(ArgumentException))]
		public void TestDeserializeReadOnly()
		{
			HaveReadOnly o = new HaveReadOnly();
			o.Value = "a";

			PropertySerializer<HaveReadOnly> ser = new PropertySerializer<HaveReadOnly>("Value", "ReadOnly");
			ser.ContinueOnError = false;
			Assert.AreEqual(false, ser.ContinueOnError);

			ser.Serialize(o, Dictionary);

			HaveReadOnly test = new HaveReadOnly();
			ser.Deserialize(test, Dictionary);//should go boom

			Assert.Fail();
		}

		[Test]
		public void TestDeserializeReadOnlyWithContinue()
		{
			HaveReadOnly o = new HaveReadOnly();
			o.Value = "a";

			PropertySerializer<HaveReadOnly> ser = new PropertySerializer<HaveReadOnly>("Value", "ReadOnly");
			ser.ContinueOnError = true;
			Assert.AreEqual(true, ser.ContinueOnError);

			ser.Serialize(o, Dictionary);

			HaveReadOnly test = new HaveReadOnly();
			ser.Deserialize(test, Dictionary);
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void TestSerializeReadOnly()
		{
			HaveReadOnly o = new HaveReadOnly();
			o.Value = "a";

			PropertySerializer<HaveReadOnly> ser = new PropertySerializer<HaveReadOnly>("Value", "WriteOnly");
			ser.ContinueOnError = false;
			Assert.AreEqual(false, ser.ContinueOnError);

			ser.Serialize(o, Dictionary);
			Assert.Fail();
		}

		[Test]
		public void TestSerializeReadOnlyWithContinue()
		{
			HaveReadOnly o = new HaveReadOnly();
			o.Value = "a";

			PropertySerializer<HaveReadOnly> ser = new PropertySerializer<HaveReadOnly>("Value", "WriteOnly");
			ser.ContinueOnError = true;
			Assert.AreEqual(true, ser.ContinueOnError);

			ser.Serialize(o, Dictionary);
		}

		[Test]
		public void TestObsolete()
		{
			PropertySerializer<HaveReadOnly> ser = new PropertySerializer<HaveReadOnly>();
			try
			{
				ser.GetType().InvokeMember("Serialize", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.Instance, null,
					ser, new object[] { new object(), Dictionary });
			}
			catch (TargetInvocationException e)
			{ Assert.AreEqual(typeof(NotSupportedException), e.InnerException.GetType()); }
			try
			{
				ser.GetType().InvokeMember("Deserialize", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.Instance, null,
					ser, new object[] { new object(), Dictionary });
			}
			catch (TargetInvocationException e)
			{ Assert.AreEqual(typeof(NotSupportedException), e.InnerException.GetType()); }
		}

		[Test]
		public void TestObjectSerializer()
		{
			HaveReadOnly data = new HaveReadOnly();
			data.Value = "a";

			ObjectSerializer ser = new ObjectSerializer(data, "Value");
			ser.Serialize(Dictionary);

			HaveReadOnly test = new HaveReadOnly();
			ser = new ObjectSerializer(test, "Value");
			ser.Deserialize(Dictionary);

			Assert.AreEqual("a", test.Value);
		}
	}
}
