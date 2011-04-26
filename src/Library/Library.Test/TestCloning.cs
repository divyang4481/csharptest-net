#region Copyright 2010-2011 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Runtime.Serialization;
using System.Threading;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Diagnostics;
using CSharpTest.Net.Cloning;

#pragma warning disable 1591

namespace CSharpTest.Net.Library.Test
{
    [TestFixture]
    [Category("TestCloning")]
    public partial class TestCloning
    {
        private void AssertDifferences(TestObject objA, TestObject objB)
        {
            Assert.AreEqual(objA.stringData, objB.stringData);
            Assert.AreEqual(objA.intData, objB.intData);
            Assert.AreEqual(objA.dateData, objB.dateData);
            Assert.AreEqual(objA.doubleData.Length, objB.doubleData.Length);

            Assert.IsFalse(Object.ReferenceEquals(objA.methodData, objB.methodData));
            Assert.IsFalse(objA.hasRun);
            objA.methodData();
            Assert.IsTrue(objA.hasRun);
            Assert.IsFalse(objB.hasRun);
            objB.methodData();
            Assert.IsTrue(objB.hasRun);

            Assert.IsFalse(Object.ReferenceEquals(objA.customData, objB.customData));
            Assert.IsFalse(Object.ReferenceEquals(objA.simpleData, objB.simpleData));
            Assert.AreEqual(objA.simpleData.Created, objB.simpleData.Created);
        }

        [Test]
        public void TestCloneMembers()
        {
            TestObject[] copies = new TestObject[3];
            TestObject objB, objA = TestObject.Create();

            using (ObjectCloner cloner = new MemberwiseClone())
            {
                objB = cloner.Clone(objA);
                for (int i = 0; i < copies.Length; i++)
                    copies[i] = cloner.Clone(objB);
            }

            AssertDifferences(objA, objB);
            //not using serialization yields new instance of singleton
            Assert.IsFalse(Object.ReferenceEquals(objA.singletonData, objB.singletonData));
            //not using serializaiton skips all custom routines
            Assert.IsFalse(objA.customData.CustomData);
            Assert.IsFalse(objA.customData.Deserialized);
            Assert.IsFalse(objB.customData.CustomData);
            Assert.IsFalse(objB.customData.Deserialized);
        }

        [Test]
        public void TestCloneSerializer()
        {
            TestObject[] copies = new TestObject[3];
            TestObject objB, objA = TestObject.Create();

            using (ObjectCloner cloner = new SerializerClone())
            {
                objB = cloner.Clone(objA);
                for (int i = 0; i < copies.Length; i++)
                    copies[i] = cloner.Clone(objB);
            }

            AssertDifferences(objA, objB);
            //not using serialization yields new instance of singleton
            Assert.IsTrue(Object.ReferenceEquals(objA.singletonData, objB.singletonData));
            //not using serializaiton skips all custom routines
            Assert.IsFalse(objA.customData.CustomData);
            Assert.IsFalse(objA.customData.Deserialized);
            Assert.IsTrue(objB.customData.CustomData);
            Assert.IsTrue(objB.customData.Deserialized);
        }

        //[Test]
        public void TestClonePerf()
        {
            const int Reps = 1000;
            TestObject test = TestObject.Create();
            object result;

            Stopwatch timer = new Stopwatch();
            timer.Start();

            for (int i = 0; i < Reps; i++)
            {
                BinaryFormatter bf = new BinaryFormatter();
                using (MemoryStream m = new MemoryStream())
                {
                    bf.Serialize(m, test);
                    m.Position = 0;
                    result = bf.Deserialize(m);
                }
            }

            timer.Stop();
            Console.WriteLine("Elapsed: {0,10} (100%)", timer.ElapsedTicks);
            long percentof = timer.ElapsedTicks;

            timer = new Stopwatch();
            timer.Start();

            for (int i = 0; i < Reps; i++)
            {
                result = new SerializerClone().Clone(test);
            }

            timer.Stop();
            Console.WriteLine("Elapsed: {0,10} ({1}%)", timer.ElapsedTicks, (timer.ElapsedTicks * 100) / percentof);

            timer = new Stopwatch();
            timer.Start();

            for (int i = 0; i < Reps; i++)
            {
                result = new MemberwiseClone().Clone(test);
            }

            timer.Stop();
            Console.WriteLine("Elapsed: {0,10} ({1}%)", timer.ElapsedTicks, (timer.ElapsedTicks * 100) / percentof);
        }

        [Serializable]
        sealed class Singleton : ISerializable
        {
            public readonly DateTime CreatedOn = DateTime.Now;
            private static readonly Singleton theOneObject = new Singleton();
            private Singleton()
            { }
            public static Singleton GetSingleton()
            {
                return theOneObject;
            }
            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.SetType(typeof(SingletonSerializationHelper));
                info.AddValue("_createdOn", CreatedOn);
            }
        }

        [Serializable]
        sealed class SingletonSerializationHelper : IObjectReference
        {
            DateTime _createdOn = DateTime.MinValue;
            int _missingField = 1;

            public Object GetRealObject(StreamingContext context)
            {
                Assert.AreEqual(0, _missingField);
                Singleton instance = Singleton.GetSingleton();
                Assert.AreEqual(instance.CreatedOn, _createdOn);
                return instance;
            }
        }

        [Serializable]
        class TestCustomSer : ISerializable, IDeserializationCallback
        {
            public TestCustomSer() { }
            private TestCustomSer(SerializationInfo info, StreamingContext context)
            {
                Deserialized = false;
                CustomData = info.GetBoolean("_customData");
            }

            public bool Deserialized = false;
            public bool CustomData = false;

            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("_customData", true);
            }

            void IDeserializationCallback.OnDeserialization(object sender)
            {
                Deserialized = true;
            }
        }

        class TestSimple { public readonly DateTime Created = DateTime.Now; }

        [Serializable]
        class TestObject
        {
            public string stringData = "copy string";
            public int intData = int.MaxValue;
            public DateTime dateData = DateTime.Now;
            public double[,] doubleData = new double[,] { { 1.1, 2.1 }, { 1.2, 2.2 }, { 1.3, 2.3 } };
            public ThreadStart methodData;
            public Singleton singletonData = Singleton.GetSingleton();
            public TestCustomSer customData = new TestCustomSer();
            public TestSimple simpleData = new TestSimple();
            [NonSerialized]
            public bool hasRun = false;

            private TestObject()
            {
                methodData = new ThreadStart(Run);
            }

            public static TestObject Create() { return new TestObject(); }

            public void Run() { hasRun = true; }
        }
    }

    [TestFixture]
    public class TestCloningNegative
    {
        [Test]
        [ExpectedException(typeof(SerializationException))]
        public void TestBadSerializer()
        {
            using (ObjectCloner c = new SerializerClone())
                c.Clone(new BadSerializer());
        }

        [Serializable]
        class BadSerializer : ISerializable
        {
            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
            }
        }
    }
}
