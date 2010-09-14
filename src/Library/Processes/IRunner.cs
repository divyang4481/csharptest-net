using System;

namespace CSharpTest.Net.Processes
{
    /// <summary>
    /// The common interface between spawning processes, and spawning scripts.
    /// </summary>
    public interface IRunner
    {
        /// <summary> Notifies caller of writes to the std::err or std::out </summary>
        event ProcessOutputEventHandler OutputReceived;

        /// <summary> Notifies caller when the process exits </summary>
        event ProcessExitedEventHandler ProcessExited;

		/// <summary> Allows writes to the std::in for the process </summary>
		System.IO.TextWriter StandardInput { get; }

        /// <summary> Waits for the process to exit and returns the exit code </summary>
        int ExitCode { get; }

        /// <summary> Returns true if this instance is running a process </summary>
        bool IsRunning { get; }

        /// <summary> Kills the process if it is still running </summary>
        void Kill();

        /// <summary> Closes std::in and waits for the process to exit </summary>
        void WaitForExit();

        /// <summary> Closes std::in and waits for the process to exit, returns false if the process did not exit in the time given </summary>
        bool WaitForExit(TimeSpan timeout);

        /// <summary> Runs the process and returns the exit code. </summary>
        int Run();

        /// <summary> Runs the process and returns the exit code. </summary>
		int Run(params string[] args);

        /// <summary> Starts the process and returns. </summary>
        void Start();

        /// <summary> Starts the process and returns. </summary>
		void Start(params string[] args);
    }
}