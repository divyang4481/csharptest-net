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
using System.IO;

namespace CSharpTest.Net.Logging.Implementation
{
	[System.Diagnostics.DebuggerNonUserCode()]
	//[System.Diagnostics.DebuggerStepThrough()]
	class Configuration
	{
		internal static readonly Int32 ProcessId;
		internal static readonly string ProcessName;
		internal static readonly string AppDomainName;
		internal static readonly string EntryAssembly;
		internal static readonly string DefaultLogFile;
		internal static readonly bool IsDebugging;

		internal static readonly string FORMAT_DEFAULT;
		internal static readonly string FORMAT_LOCATION;
		internal static readonly string FORMAT_METHOD;
		internal static readonly string FORMAT_FILELINE;
		internal static readonly string FORMAT_FULLMESSAGE;

		internal static readonly IFormatProvider FormatProvider;
		internal static readonly System.Runtime.Serialization.Formatters.Binary.BinaryFormatter Binary;

		/// <summary> This is the max size a file will get (roughly) before being renamed </summary>
		internal static Int32 FILE_SIZE_THREASHOLD;
		/// <summary> This is the max number of files to keep, don't set too high until you revisit RollingRenameFile()</summary>
		internal static Int32 FILE_MAX_HISTORY_SIZE;

		internal static LogLevels LogLevel;
		internal static LogOutputs LogOutput;
		internal static LogOptions LogOption;
		internal static IFormatProvider InnerFormatter;

		internal static string CurrentLogFile;
		internal static string EventLogName;
		internal static string EventLogSource;

		internal static string FORMAT_ASPNET;
		internal static string FORMAT_TRACE;
		internal static string FORMAT_CONSOLE;
		internal static string FORMAT_LOGFILE;
		internal static string FORMAT_EVENTLOG;

		internal static LogLevels LEVEL_ASPNET;
		internal static LogLevels LEVEL_TRACE;
		internal static LogLevels LEVEL_CONSOLE;
		internal static LogLevels LEVEL_LOGFILE;
		internal static LogLevels LEVEL_EVENTLOG;

		/// <summary> Initializes all readonly configuration values </summary>
		static Configuration()
		{
			// Serializer:
			Binary = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
			Binary.AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
			Binary.FilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;
			Binary.TypeFormat = System.Runtime.Serialization.Formatters.FormatterTypeStyle.TypesWhenNeeded;

			// DEFUALT FORMATTING
			FORMAT_DEFAULT		= Utils.PrepareFormatString("[{ManagedThreadId:D2}] {Level,8} - {Message}");// <- this can NOT contain expressions
			FORMAT_METHOD		= Utils.PrepareFormatString("{MethodType:%s.}{MethodName}{MethodArgs:(%s)}");
			FORMAT_LOCATION		= Utils.PrepareFormatString("{LogCurrent:   executing %s}{Method:   at %s@}{IlOffset:?}{FileLocation}");
			FORMAT_FILELINE		= Utils.PrepareFormatString("{FileName: in %s:line }{FileLine:?}");
			FORMAT_FULLMESSAGE	= Utils.PrepareFormatString("{Message}{Location}{Exception}");
			FormatProvider = new EventDataFormatter();

			// INIT STATIC DATA
			IsDebugging = System.Diagnostics.Debugger.IsAttached;

			System.Diagnostics.Process thisProcess = System.Diagnostics.Process.GetCurrentProcess();
			ProcessId = thisProcess.Id;
			try { ProcessName = thisProcess.MainModule.ModuleName; }
			catch { ProcessName = String.Format( "[{0}]", ProcessId ); }

			AppDomainName = AppDomain.CurrentDomain.FriendlyName;
			System.Reflection.Assembly asm = System.Reflection.Assembly.GetEntryAssembly();
			if( asm != null )
				EntryAssembly = asm.GetName().Name;
			else if( ProcessName.EndsWith( ".exe", StringComparison.OrdinalIgnoreCase ) )
				EntryAssembly = ProcessName.Substring( 0, ProcessName.Length - 4 );
			else
				EntryAssembly = ProcessName;

			DefaultLogFile = Path.Combine( Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ), EntryAssembly ), "LogFile{0}.txt" );
		}

		/// <summary> Sets appropriate (i hope) defaults to all configuration options </summary>
		public static void Configure()
		{
			// DEFAULT OUTPUT FORMATS:
			FORMAT_ASPNET	= Utils.PrepareFormatString("{Level,8} - {Message}");
			FORMAT_TRACE	= Utils.PrepareFormatString("[{ManagedThreadId:D2}] {Level,8} - {Message}{Location}{Exception}");
			FORMAT_CONSOLE  = Utils.PrepareFormatString("[{ManagedThreadId:D2}] {Level,8} - {Message}{Location}{Exception}");
			FORMAT_EVENTLOG = Utils.PrepareFormatString("[{ManagedThreadId:D2}] {Level,8} - {Message}{Location}{Exception}");
			FORMAT_LOGFILE	= Utils.PrepareFormatString("{EventTime:o} [{ProcessId:D4},{ManagedThreadId:D2}] {Level,8} - {Message}{Location}{Exception}");

			//These filters will apply after the Log.Level filter:
			LEVEL_ASPNET = LogLevels.Verbose;
			LEVEL_TRACE = LogLevels.Verbose;
			LEVEL_CONSOLE = LogLevels.Verbose;
			LEVEL_LOGFILE = LogLevels.Verbose;
			LEVEL_EVENTLOG = LogLevels.Warning;
			
			// INIT DEFAULT LOG LEVELS
			LogOutput = LogOutputs.LogFile;
			LogLevel = LogLevels.Verbose;
			LogOption = LogOptions.Default;
			InnerFormatter = System.Globalization.CultureInfo.InvariantCulture;
			CurrentLogFile = DefaultLogFile;

			FILE_SIZE_THREASHOLD = 10 * 1024 * 1024; // 10mb max log file
			FILE_MAX_HISTORY_SIZE = 10; // Don't keep more than 10

			EventLogName = "Application";
			EventLogSource = ProcessName;

			try 
			{ 
				if( System.Web.HttpContext.Current != null && System.Web.HttpContext.Current.Trace.IsEnabled )
					LogOutput |= LogOutputs.AspNetTrace; 
			} catch { }

			if( IsDebugging )
				LogOutput |= LogOutputs.TraceWrite;

			// if debugging or running with asp.net's trace mode, add the file info (we know we aren't in a production environment)
			if( (LogOutput & (LogOutputs.AspNetTrace | LogOutputs.TraceWrite)) != LogOutputs.None )
				LogOption |= LogOptions.LogAddFileInfo;
		}
	}
}
