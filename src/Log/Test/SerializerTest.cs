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
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

#pragma warning disable 1591
namespace CSharpTest.Net.Logging.Test
{
	[TestFixture]
	[Category("SerializerTest")]
	public partial class SerializerTest
	{
		#region TestFixture SetUp/TearDown
		[SetUp]
		public void RestoreLogState()
		{
			Log.Config.Options = LogOptions.Default | LogOptions.LogAddFileInfo | LogOptions.LogAddAssemblyInfo;
			Log.Config.Output = LogOutputs.LogFile | LogOutputs.TraceWrite;
			Log.Config.Level = LogLevels.Verbose;
			Log.Config.SetOutputLevel(LogOutputs.All, LogLevels.Verbose);
			Log.Config.SetOutputFormat(LogOutputs.All, "{FullMessage}");
			Log.Config.SetOutputFormat(LogOutputs.TraceWrite, "{Message}");
		}

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
			string message = "This is a test of serialized event data.";
			Exception myError = new PlatformNotSupportedException("Error.Message", new InsufficientMemoryException());
			LogEventArgs arg1 = null;

			LogEventHandler eh = new LogEventHandler(delegate(object s, LogEventArgs e) { arg1 = e; });
			string[] stack = new string[] { "step 1", "step 2" };
			using (Log.Start(stack[0]))
			using (Log.Start(stack[1]))
			{
				Log.LogWrite += eh;
				Log.Error(myError, message);
				Log.LogWrite -= eh;
			}
			Assert.IsNotNull(arg1);
			Assert.AreEqual(1, arg1.Count);
			Assert.AreEqual(1, arg1.ToArray().Length);

			EventData data = arg1.ToArray()[0];
			Assert.IsNotNull(data);
			BasicLogTest.AssertMessage(GetType(), stack, data, LogLevels.Error, message, myError.GetType());
			Assert.AreEqual(String.Join("::", stack), data.ToString("{LogStack}"));

			BinaryFormatter ser = new BinaryFormatter();
			MemoryStream ms = new MemoryStream();
			
			ser.Serialize(ms, arg1);
			Assert.Greater((int)ms.Position, 0);

			ms.Position = 0;
			object restored = ser.Deserialize(ms);
			Assert.IsNotNull(restored);
			Assert.AreEqual(typeof(LogEventArgs), restored.GetType());
			LogEventArgs arg2 = restored as LogEventArgs;

			Assert.IsNotNull(arg2);
			Assert.AreEqual(1, arg2.Count);
			Assert.AreEqual(1, arg2.ToArray().Length);

			data = arg2.ToArray()[0];
			Assert.IsNotNull(data);

			Assert.IsNotNull(data.Exception);
			Assert.AreNotEqual(myError.GetType(), data.Exception.GetType());
			Assert.AreEqual(typeof(Log).Assembly, data.Exception.GetType().Assembly);
			Assert.AreEqual(myError.Message, data.Exception.Message);
			Assert.AreEqual(myError.StackTrace, data.Exception.StackTrace);
			Assert.AreEqual(myError.Source, data.Exception.Source);
			Assert.AreEqual(myError.ToString(), data.Exception.ToString());

			BasicLogTest.AssertMessage(GetType(), stack, data, LogLevels.Error, message, data.Exception.GetType());
		}
	}
}
