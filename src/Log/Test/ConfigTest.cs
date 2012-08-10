#region Copyright 2008-2012 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Globalization;
using System.IO;
using System.IO.Compression;

#pragma warning disable 1591
namespace CSharpTest.Net.Logging.Test
{
	[TestFixture]
	[Category("ConfigTest")]
	public partial class ConfigTest : TraceListeningTest
	{
		#region TestFixture SetUp/TearDown
		TextWriter _out, _error;

		[SetUp]
		public void RestoreLogState()
		{
			Log.Config.Options = LogOptions.Default;
			Log.Config.Output = LogOutputs.LogFile | LogOutputs.TraceWrite;
			Log.Config.Level = LogLevels.Verbose;
			Log.Config.SetOutputLevel(LogOutputs.All, LogLevels.Verbose);
			Log.Config.SetOutputFormat(LogOutputs.TraceWrite, "{Message}");
		}

		[TestFixtureSetUp]
		public override void Setup()
		{
			base.Setup();

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

			RestoreLogState();
			Log.Config.SetOutputFormat(LogOutputs.All, "[{ManagedThreadId}] {Level} - {FullMessage}");
			base.Teardown();
		}
		#endregion

		[Test]
		public void TestOutput()
		{
			LogOutputs defaultOutputs = Log.Config.Output;
			Assert.AreEqual(defaultOutputs, Log.Config.Output);
			Log.Config.Output = LogOutputs.TraceWrite;

			Log.Write("Test Trace");
			Assert.AreEqual(GetType().FullName + ": Test Trace", _lastTrace);

			Assert.AreEqual(LogOutputs.TraceWrite, Log.Config.Output);

			Log.Config.Output = LogOutputs.None;
			_lastTrace = null;
			Log.Write("Test Trace");
			Assert.IsNull(_lastTrace);

			Log.Config.Output = LogOutputs.TraceWrite | defaultOutputs;
			Assert.AreEqual(LogOutputs.TraceWrite | defaultOutputs, Log.Config.Output);
		}

		[Test]
		public void TestFormatter()
		{
			DateTime now = DateTime.Now;
			string cultured = String.Format("{0}", now);
			string invariant = String.Format(CultureInfo.InvariantCulture, "{0}", now);

			Log.Write("{0}", now);
			Assert.IsTrue(_lastTrace.StartsWith(GetType().FullName + ": "));
			string trimed = _lastTrace.Substring(GetType().FullName.Length + 2);

			Assert.AreEqual(invariant, trimed);

			Log.Config.FormatProvider = CultureInfo.CurrentCulture;
			Log.Write("{0}", now);
			Assert.IsTrue(_lastTrace.StartsWith(GetType().FullName + ": "));
			trimed = _lastTrace.Substring(GetType().FullName.Length + 2);

			Assert.AreEqual(cultured, trimed);
	
			Log.Config.FormatProvider = CultureInfo.InvariantCulture;
			Log.Write("{0}", now);
			Assert.IsTrue(_lastTrace.StartsWith(GetType().FullName + ": "));
			trimed = _lastTrace.Substring(GetType().FullName.Length + 2);

			Assert.AreEqual(invariant, trimed);
		}

		[Test]
		public void TestSetLogFile()
		{
			Log.Config.Output |= LogOutputs.LogFile;
			string curFile = Log.Config.LogFile;
			Assert.IsTrue(curFile.EndsWith("{0}.txt"));
			string temp = Path.GetTempFileName();
			try
			{
				File.Delete(temp);

				Assert.IsFalse(File.Exists(temp));
				Log.Config.LogFile = temp;
				Assert.AreEqual(temp, Log.Config.LogFile);

				string message = String.Format("Hello World: {0}", Guid.NewGuid());
				Log.Write(message);
				Assert.IsTrue(File.Exists(temp));

				string filetext;
				using (StreamReader sr = new StreamReader(File.Open(temp, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
					filetext = sr.ReadToEnd();
				Assert.IsTrue(filetext.Contains(message));
			}
			finally
			{
				Log.Config.LogFile = curFile;
				File.Delete(temp);
			}
		}

		[Test]
		public void TestLogMaxValues()
		{
			TestLogFileWriter(false, true);
			TestLogFileWriter(true, true);
			TestLogFileWriter(true, false);
		}
		private void TestLogFileWriter( bool compressed, bool rolling )
		{
			if( compressed )
				Log.Config.Options |= LogOptions.GZipLogFileOnRoll;
			else
				Log.Config.Options &= ~LogOptions.GZipLogFileOnRoll;

			int origHist = 0, origSize = 0;
			string OrigFile = Log.Config.LogFile;
			string tempFile = Path.GetTempFileName();
			try
			{
				Assert.GreaterOrEqual(origHist = Log.Config.LogFileMaxHistory, 5);
				Assert.GreaterOrEqual(origSize = Log.Config.LogFileMaxSize, 1024 * 1024);
				string newFile = Path.ChangeExtension(tempFile, rolling ? ".{0}.txt" : ".xml");

				Log.Config.LogFileMaxHistory = -3;
				Assert.AreNotEqual(-3, Log.Config.LogFileMaxHistory);
				Log.Config.LogFileMaxSize = -10;
				Assert.AreNotEqual(-10, Log.Config.LogFileMaxSize);

				Log.Config.LogFileMaxHistory = 3;
				int minBytesPerFile = Log.Config.LogFileMaxSize;

				Assert.AreEqual(3, Log.Config.LogFileMaxHistory);
				Assert.GreaterOrEqual(minBytesPerFile, 8192);

				Log.Config.LogFile = newFile;

				Random rnd = new Random();
				byte[] randomdata = new byte[1024];
				rnd.NextBytes(randomdata);
				string randomtext = Convert.ToBase64String(randomdata);

				for (int size = 0; size < 5 * minBytesPerFile; size += randomtext.Length)
					Log.Write(randomtext);

				//Current file uncompressed?
				string current = String.Format(newFile, 0);
				Assert.IsTrue(File.Exists(current));
				Assert.LessOrEqual((int)new FileInfo(current).Length, minBytesPerFile + 2048);

				byte[] testdata = new byte[minBytesPerFile * 2];
				if (rolling)
				{
					for (int i = 1; i < 3; i++)
					{
						string sfile = String.Format(newFile, i) + (compressed ? ".gz" : "");
						//Other files are compressed?
						Assert.IsTrue(File.Exists(sfile));

						using (Stream io = File.OpenRead(sfile))
						{
							Stream read = io;
							if (compressed)
								read = new GZipStream(io, CompressionMode.Decompress);

							Assert.LessOrEqual(read.Read(testdata, 0, testdata.Length), minBytesPerFile + 2048);
						}
						File.Delete(sfile);
					}
					Assert.IsFalse(File.Exists(String.Format(newFile, 3)));
				}
				else
				{
					System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
					using(Stream io = File.Open( newFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite ))
						doc.Load(io);
				}
			}
			finally
			{
				Log.Config.LogFileMaxHistory = origHist;
				Log.Config.LogFileMaxSize = origSize;
				Log.Config.LogFile = OrigFile;
				Log.Config.Options |= LogOptions.GZipLogFileOnRoll;
				foreach (string file in Directory.GetFiles(Path.GetDirectoryName(tempFile), Path.ChangeExtension(Path.GetFileName(tempFile), ".*")))
					File.Delete(file);
				File.Delete(tempFile);
			}
		}

		[Test]
		public void TestOutputFormat()
		{
			LogOutputs output = Log.Config.Output;
			try
			{
				string format = "NOTHING";
				Log.Config.SetOutputFormat(LogOutputs.AspNetTrace, format);
				Log.Config.SetOutputFormat(LogOutputs.TraceWrite, format);
				Log.Config.SetOutputFormat(LogOutputs.Console, format);
				Log.Config.SetOutputFormat(LogOutputs.EventLog, format);
				Log.Config.SetOutputFormat(LogOutputs.LogFile, format);
				Log.Config.SetOutputFormat(LogOutputs.None, format);

				Log.Config.Output = LogOutputs.All & ~LogOutputs.EventLog;

				TextWriter sw = new StringWriter(), orig = Console.Out;
				Console.SetOut(sw);
				string tempFile = Path.GetTempFileName();
				string origLog = Log.Config.LogFile;
				Log.Config.LogFile = tempFile;
				try
				{
					Log.Write("Hello");
				}
				finally
				{
					Console.SetOut(orig);
					Log.Config.LogFile = origLog;
				}

				Assert.AreEqual(format, _lastTrace.Substring(GetType().FullName.Length + 2));
				Assert.AreEqual(format, sw.ToString().Trim());
				Assert.AreEqual(format, File.ReadAllText(tempFile).Trim());
			}
			finally
			{
				Log.Config.Output = output;
				Log.Config.SetOutputFormat(LogOutputs.All, "[{ManagedThreadId}] {Level} - {FullMessage}");
			}
		}

		[Test]
		public void TestOutputLevel()
		{
			LogOutputs output = Log.Config.Output;
			try
			{
				string messageText = "Warning will robinson, DANGER!";

				Log.Config.SetOutputFormat(LogOutputs.All, "{Message}");
				Log.Config.SetOutputLevel(LogOutputs.All, LogLevels.Info);

				LogLevels newLevel = LogLevels.Warning;
				Assert.AreEqual(LogLevels.Info, Log.Config.SetOutputLevel(LogOutputs.AspNetTrace, newLevel));
				Assert.AreEqual(LogLevels.Info, Log.Config.SetOutputLevel(LogOutputs.TraceWrite, newLevel));
				Assert.AreEqual(LogLevels.Info, Log.Config.SetOutputLevel(LogOutputs.Console, newLevel));
				Assert.AreEqual(LogLevels.Info, Log.Config.SetOutputLevel(LogOutputs.EventLog, newLevel));
				Assert.AreEqual(LogLevels.Info, Log.Config.SetOutputLevel(LogOutputs.LogFile, newLevel));
				Assert.AreEqual(LogLevels.None, Log.Config.SetOutputLevel(LogOutputs.None, newLevel));

				Log.Config.Output = LogOutputs.All & ~LogOutputs.EventLog;

				TextWriter sw = new StringWriter(), orig = Console.Out;
				Console.SetOut(sw);
				string tempFile = Path.GetTempFileName();
				string origLog = Log.Config.LogFile;
				Log.Config.LogFile = tempFile;
				try
				{
					Log.Warning(messageText);
					Log.Info("Hello");
					Log.Verbose("Hello");
				}
				finally
				{
					Console.SetOut(orig);
					Log.Config.LogFile = origLog;
				}

				Assert.AreEqual(messageText, _lastTrace.Substring(GetType().FullName.Length + 2));
				Assert.AreEqual(messageText, sw.ToString().Trim());
				Assert.AreEqual(messageText, File.ReadAllText(tempFile).Trim());
			}
			finally
			{
				Log.Config.Output = output;
				Log.Config.SetOutputLevel(LogOutputs.All, LogLevels.Verbose);
				Log.Config.SetOutputFormat(LogOutputs.All, "[{ManagedThreadId}] {Level} - {FullMessage}");
			}
		}

		[Test]
		public void TestEventSourceInfo()
		{
			string origLog = Log.Config.EventLogName;
			string origSrc = Log.Config.EventLogSource;

			Log.Config.EventLogName = "Dua!";
			Assert.AreEqual("Dua!", Log.Config.EventLogName);
			Log.Config.EventLogSource = "Yea.";
			Assert.AreEqual("Yea.", Log.Config.EventLogSource);
			try
			{
				Log.Config.EventLogName = "Application";
				Log.Config.EventLogSource = "MsiInstaller";

				Log.Config.Output = LogOutputs.EventLog;
				Log.Config.SetOutputLevel(LogOutputs.EventLog, LogLevels.Info);
				Log.Write("Logging.Test has hijacked your MsiInstaller source for testing use.  Ignore this message.");
			}
			finally
			{
				RestoreLogState();
				Log.Config.EventLogName = "Application";
				Log.Config.EventLogSource = "Logging.Test";
			}
		}
	}
}
