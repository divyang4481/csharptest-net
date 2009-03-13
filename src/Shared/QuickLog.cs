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
using System.Diagnostics;
using System.IO;

/// <summary>
/// Quick and dirty logging for components that do not have dependencies
/// </summary>
internal static partial class Log
{
	#region static Log() -- Opens Log file for writting
	static Log()
	{
		Open();
	}

	static TextWriterTraceListener _traceWriter = null;

	public static void Open()
	{
		try
		{
			string fullName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), String.Format("{0}\\LogFile.txt", Process.GetCurrentProcess().ProcessName));
			Directory.CreateDirectory(Path.GetDirectoryName(fullName));

			string back = Path.ChangeExtension(fullName, ".bak");
			if (File.Exists(back))
				File.Delete(back);
			if (File.Exists(fullName))
				File.Move(fullName, back);

			FileStream fsStream = File.Open(fullName, FileMode.Append, FileAccess.Write, FileShare.Read | FileShare.Delete);
			StreamWriter sw = new StreamWriter(fsStream);
			sw.AutoFlush = true;
			Trace.Listeners.Add(_traceWriter = new TextWriterTraceListener(sw));
		}
		catch (Exception e)
		{ Trace.WriteLine(e.ToString(), "CSharpTest.Net.QuickLog.Open()"); }
	}

	public static void Close()
	{
		try
		{
			if (_traceWriter != null)
			{
				Trace.Listeners.Remove(_traceWriter);
				_traceWriter.Dispose();
				_traceWriter = null;
			}
		}
		catch (Exception e)
		{ Trace.WriteLine(e.ToString(), "CSharpTest.Net.QuickLog.Close()"); }
	}
	#endregion

	public static void Error(Exception e) { InternalWrite(TraceLevel.Error, "{0}", e); }
	public static void Warning(Exception e) { InternalWrite(TraceLevel.Warning, "{0}", e); }

	public static void Error(string format, params object[] args) { InternalWrite(TraceLevel.Error, format, args); }
	public static void Warning(string format, params object[] args) { InternalWrite(TraceLevel.Warning, format, args); }
	public static void Info(string format, params object[] args) { InternalWrite(TraceLevel.Info, format, args); }
	public static void Verbose(string format, params object[] args) { InternalWrite(TraceLevel.Verbose, format, args); }
	public static void Write(string format, params object[] args) { InternalWrite(TraceLevel.Off, format, args); }
	public static void Write(TraceLevel level, string format, params object[] args) { InternalWrite(level, format, args); }
	
	public static IDisposable Start(string format, params object[] args)
	{
		try 
		{
			if( args.Length > 0 ) format = String.Format( format, args );
			InternalWrite(TraceLevel.Verbose,  "Start {0}", format);
		}
		catch (Exception e) { Trace.WriteLine(e.ToString(), "CSharpTest.Net.QuickLog.Write()"); }
		return new TaskInfo( format );
	}

	public static IDisposable AppStart(string format, params object[] args)
	{
		try
		{
			if (args.Length > 0) format = String.Format(format, args);
			InternalWrite(TraceLevel.Verbose, "Start {0}", format);
		}
		catch (Exception e) { Trace.WriteLine(e.ToString(), "CSharpTest.Net.QuickLog.Write()"); }
		return new TaskInfo(format);
	}
	
	private class TaskInfo : MarshalByRefObject, IDisposable
	{
		private readonly DateTime _start;
		private readonly string _task;
		public TaskInfo(string task) { _task = task; _start = DateTime.Now; }
		void IDisposable.Dispose() { InternalWrite(TraceLevel.Verbose, "End {0} ({1} ms)", _task, (DateTime.Now - _start).TotalMilliseconds); }
	}

	private static void InternalWrite( TraceLevel level, string format, params object[] args )
	{
		try
		{
			if (args.Length > 0)
				format = String.Format(format, args);
			
			StackFrame frame = new StackFrame(2);
			System.Reflection.MethodBase method = frame.GetMethod();
			string full = String.Format("{0:D2}{1,8} - {2}   at {3}", 
				System.Threading.Thread.CurrentThread.ManagedThreadId, 
				level == TraceLevel.Off ? "None" : level.ToString(),
				format, method);

			Trace.WriteLine(full, method.ReflectedType.ToString());
			if (LogWrite != null)
				LogWrite(method, level, format);
		}
		catch(Exception e)
		{ Trace.WriteLine(e.ToString(), "CSharpTest.Net.QuickLog.Write()"); }
	}

	public delegate void LogEventHandler(System.Reflection.MethodBase method, TraceLevel level, string message);
	public static event LogEventHandler LogWrite;
	
	#region Remoting Able Version
	public interface ILog
	{
		void Error(Exception e);
		void Warning(Exception e);

		void Error(string format, params object[] args);
		void Warning(string format, params object[] args);
		void Info(string format, params object[] args);
		void Verbose(string format, params object[] args);
		void Write(string format, params object[] args);

		IDisposable Start(string format, params object[] args);
		IDisposable AppStart(string format, params object[] args);
	}

	public static ILog RemoteLog = new LogWrapper();

	private class LogWrapper : MarshalByRefObject, ILog
	{
		void ILog.Error(Exception e) { InternalWrite(TraceLevel.Error, "{0}", e); }
		void ILog.Warning(Exception e) { InternalWrite(TraceLevel.Warning, "{0}", e); }

		void ILog.Error(string format, params object[] args) { InternalWrite(TraceLevel.Error, format, args); }
		void ILog.Warning(string format, params object[] args) { InternalWrite(TraceLevel.Warning, format, args); }
		void ILog.Info(string format, params object[] args) { InternalWrite(TraceLevel.Info, format, args); }
		void ILog.Verbose(string format, params object[] args) { InternalWrite(TraceLevel.Verbose, format, args); }
		void ILog.Write(string format, params object[] args) { InternalWrite(TraceLevel.Off, format, args); }

		IDisposable ILog.Start(string format, params object[] args)
		{
			try
			{
				if (args.Length > 0) format = String.Format(format, args);
				InternalWrite(TraceLevel.Verbose, "Start {0}", format);
			}
			catch (Exception e) { Trace.WriteLine(e.ToString(), "CSharpTest.Net.QuickLog.Write()"); }
			return new TaskInfo(format);
		}

		IDisposable ILog.AppStart(string format, params object[] args)
		{
			try
			{
				if (args.Length > 0) format = String.Format(format, args);
				InternalWrite(TraceLevel.Verbose, "Start {0}", format);
			}
			catch (Exception e) { Trace.WriteLine(e.ToString(), "CSharpTest.Net.QuickLog.Write()"); }
			return new TaskInfo(format);
		}
	}
	#endregion
}
