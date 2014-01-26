#region Copyright 2008-2014 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Globalization;

#pragma warning disable 1591
namespace CSharpTest.Net.Logging.Test
{
	[TestFixture]
	[Category("NegativeTests")]
	public partial class NegativeTests : TraceListeningTest
	{
		#region TestFixture SetUp/TearDown
		TextWriter _out, _error;

		[TestFixtureSetUp]
		public override void Setup()
		{
			base.Setup();
			Log.Config.Output = LogOutputs.TraceWrite;
			Log.Config.Options = LogOptions.LogAddAssemblyInfo | LogOptions.LogAddFileInfo | LogOptions.Default;
			Log.Config.Level = LogLevels.Verbose;
			Log.Config.SetOutputFormat(LogOutputs.TraceWrite, "NegativeTests: {FULLMESSAGE}");

			_out = Console.Out;
			_error = Console.Error;
			Console.SetOut(new StringWriter());
			Console.SetError(new StringWriter());
		}

		[TestFixtureTearDown]
		public override void Teardown()
		{
			Console.SetOut(_out);
			Console.SetError(_error);
			base.Teardown();
		}
		#endregion

		[System.Diagnostics.DebuggerNonUserCode]
		public class IBlowUp : Exception
		{
			public override string Message { get { throw new NotImplementedException(); } }
			public override string StackTrace { get { throw new NotImplementedException(); } }
			public override string Source { get { throw new NotImplementedException(); } }
			public override string ToString() { throw new NotImplementedException(); } 
		}
		public readonly IBlowUp i_blow_up = new IBlowUp();
		string[] badFormats = new string[] { null, "{50}", "{-1}", "{ {Message:-%s-}", "{0}" };

		[Test]
		public void TestNoneOfThisBreaksMe()
		{
			IDisposable disp = Log.AppStart(null);
			disp.Dispose();
			disp.Dispose();
			foreach( string format in badFormats )
				Log.AppStart(format, i_blow_up).Dispose();

			disp = Log.Start(null);
			disp.Dispose();
			disp.Dispose();
			foreach (string format in badFormats)
				Log.AppStart(format, i_blow_up).Dispose();

			foreach (string format in badFormats)
				Log.Write(format, i_blow_up);

			foreach (string format in badFormats)
				Log.Critical(format, i_blow_up);
			foreach (string format in badFormats)
				Log.Critical(i_blow_up, format, i_blow_up);
			Log.Critical(i_blow_up);

			foreach (string format in badFormats)
				Log.Error(format, i_blow_up);
			foreach (string format in badFormats)
				Log.Error(i_blow_up, format, i_blow_up);
			Log.Error(i_blow_up);

			foreach (string format in badFormats)
				Log.Warning(format, i_blow_up);
			foreach (string format in badFormats)
				Log.Warning(i_blow_up, format, i_blow_up);
			Log.Warning(i_blow_up);

			foreach (string format in badFormats)
				Log.Info(format, i_blow_up);
			foreach (string format in badFormats)
				Log.Info(i_blow_up, format, i_blow_up);
			Log.Info(i_blow_up);

			foreach (string format in badFormats)
				Log.Verbose(format, i_blow_up);
			foreach (string format in badFormats)
				Log.Verbose(i_blow_up, format, i_blow_up);
			Log.Verbose(i_blow_up);
		}

		[Test]
		public void TestOtherBadStuff()
		{
			string origFile = Log.Config.LogFile;
			try
			{
				Log.Config.Output |= LogOutputs.LogFile;
				Log.Config.SetOutputFormat(LogOutputs.TraceWrite, "{Message}");

				Log.Config.LogFile = @"\\\\\ { <mal formed!? file> ///{0}.txt";
				Log.Write("Hi1");
				Log.Write("Hi2");
				Log.Write("Hi3");
				Assert.AreEqual(this.GetType().FullName + ": Hi3", _lastTrace);
				Log.Config.LogFile = @"C: mal {formatted} file {0}.txt";
				Log.Write("Hi4");
				Log.Write("Hi5");
				Log.Write("Hi6");
				Assert.AreEqual(this.GetType().FullName + ": Hi6", _lastTrace);
				string path = Path.Combine(Path.GetTempPath(), @"my-path-doesnt-exist");
				if (Directory.Exists(path)) Directory.Delete(path, true);
				Log.Config.LogFile = Path.Combine(path, "log.txt");
				Log.Write("Hi!");
				Assert.IsTrue(Directory.Exists(path));
				Assert.IsTrue(File.Exists(Path.Combine(path, "log.txt")));
				Log.Config.LogFile = origFile;
				Directory.Delete(path, true);

				string dir = Environment.CurrentDirectory;
				Environment.CurrentDirectory = Path.GetDirectoryName(origFile);
				Log.Config.LogFile = Path.GetFileName(origFile);
				Assert.AreEqual(origFile, Log.Config.LogFile);
				Environment.CurrentDirectory = dir;
				Assert.AreEqual(origFile, Log.Config.LogFile);
			}
			finally
			{
				Log.Config.LogFile = origFile;
			}
		}

		[Test]
		public void TestBadMessageData()
		{
			Log.LogWrite += BreakIn_LogWrite;
			try
			{

				string message = "Bad {500} Data: {0}";
				Exception myError = i_blow_up;
				LogEventArgs arg1 = null;

				LogEventHandler eh = new LogEventHandler(delegate(object s, LogEventArgs e) { arg1 = e; });
				Log.LogWrite += eh;
				Log.LogWrite += BreakIn_LogWrite;
				Log.Error(myError, message, i_blow_up);
				Log.LogWrite -= eh;
				Log.LogWrite -= BreakIn_LogWrite;

				Assert.IsNotNull(arg1);
				Assert.AreEqual(1, arg1.Count);
				Assert.AreEqual(1, arg1.ToArray().Length);

				EventData data = arg1.ToArray()[0];
				Assert.IsNotNull(data);
				BasicLogTest.AssertMessage(GetType(), null, data, LogLevels.Error, null, myError.GetType());

				Assert.IsTrue(data.ToString().Contains(message));
				Assert.IsTrue(data.ToString("{Message:%s!} {}").Contains(message));
				Assert.IsTrue(data.ToString("{Exception}").Contains(myError.GetType().FullName));

				data.Write((System.Xml.XmlTextWriter)null);
				System.Xml.XmlTextWriter wtr = new System.Xml.XmlTextWriter(new MemoryStream(new byte[10]), System.Text.Encoding.UTF32);
				data.Write(wtr);

				data.Write(new StreamWriter(new MemoryStream(new byte[10])));
				foreach (string format in badFormats)
					data.Write(new StreamWriter(new MemoryStream(new byte[10])), format);

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
				Assert.IsTrue(data.Exception.Message.Contains(myError.GetType().FullName));
				Assert.IsNotNull(data.Exception.ToString());
				Assert.AreNotEqual(String.Empty, data.Exception.ToString());
				Assert.IsTrue(data.Exception.ToString().Contains(myError.GetType().FullName));

				BasicLogTest.AssertMessage(GetType(), null, data, LogLevels.Error, null, data.Exception.GetType());

				System.Runtime.Serialization.SerializationInfo info = new System.Runtime.Serialization.SerializationInfo(data.Exception.GetType(), new myconverter());
				System.Runtime.Serialization.StreamingContext ctx = new System.Runtime.Serialization.StreamingContext();

				Exception err = (Exception)
					data.Exception.GetType().InvokeMember(null, System.Reflection.BindingFlags.CreateInstance,
					null, null, new object[] { info, ctx });

				Assert.IsNotNull(err.Message);
				Assert.IsNotEmpty(err.Message);
				Assert.IsNotNull(err.ToString());
				Assert.IsNotEmpty(err.ToString());
			}
			finally
			{
				Log.LogWrite -= BreakIn_LogWrite;
				Log.LogWrite -= BreakIn_LogWrite;
				Log.LogWrite -= BreakIn_LogWrite;
			}
		}

		[System.Diagnostics.DebuggerNonUserCode]
		void BreakIn_LogWrite(object sender, LogEventArgs args)
		{
			throw new NotImplementedException();
		}

			#region IFormatterConverter Members
		[System.Diagnostics.DebuggerNonUserCode][NUnit.Framework.Ignore]
		class myconverter : System.Runtime.Serialization.IFormatterConverter
		{
			public object Convert(object value, TypeCode typeCode)
			{
				throw new NotImplementedException();
			}

			public object Convert(object value, Type type)
			{
				throw new NotImplementedException();
			}

			public bool ToBoolean(object value)
			{
				throw new NotImplementedException();
			}

			public byte ToByte(object value)
			{
				throw new NotImplementedException();
			}

			public char ToChar(object value)
			{
				throw new NotImplementedException();
			}

			public DateTime ToDateTime(object value)
			{
				throw new NotImplementedException();
			}

			public decimal ToDecimal(object value)
			{
				throw new NotImplementedException();
			}

			public double ToDouble(object value)
			{
				throw new NotImplementedException();
			}

			public short ToInt16(object value)
			{
				throw new NotImplementedException();
			}

			public int ToInt32(object value)
			{
				throw new NotImplementedException();
			}

			public long ToInt64(object value)
			{
				throw new NotImplementedException();
			}

			public sbyte ToSByte(object value)
			{
				throw new NotImplementedException();
			}

			public float ToSingle(object value)
			{
				throw new NotImplementedException();
			}

			public string ToString(object value)
			{
				throw new NotImplementedException();
			}

			public ushort ToUInt16(object value)
			{
				throw new NotImplementedException();
			}

			public uint ToUInt32(object value)
			{
				throw new NotImplementedException();
			}

			public ulong ToUInt64(object value)
			{
				throw new NotImplementedException();
			}

		}
			#endregion

	}
}
