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
using System.Diagnostics;
using System.Timers;
// ReSharper disable EmptyGeneralCatchClause

namespace CSharpTest.Net.Delegates
{
	/// <summary>
	/// Provides scheduling a single-fire event at some duration into the future.  The operation is
	/// performed once either when the duration expires, or when this object is disposed or collected.
	/// </summary>
	public class TimeoutAction : IDisposable
	{
		private Action<Exception> _onerror;
		private Action _perform;
		private readonly Timer _timer;

		/// <summary>
		/// Provides scheduling a single-fire event at some duration into the future.  The operation is
		/// performed once either when the duration expires, or when this object is disposed or collected.
		/// </summary>
		public TimeoutAction(TimeSpan wait, Action perform)
			: this(wait, perform, TraceException)
		{
		}

		/// <summary>
		/// Provides scheduling a single-fire event at some duration into the future.  The operation is
		/// performed once either when the duration expires, or when this object is disposed or collected.
		/// </summary>
		public TimeoutAction(TimeSpan wait, Action perform, Action<Exception> onerror)
		{
			_perform = perform;
			_onerror = onerror;
			_timer = new Timer();
			_timer.BeginInit();
			_timer.AutoReset = false;
			_timer.Elapsed += TimerElapsed;
			_timer.Interval = wait.TotalMilliseconds;
			_timer.EndInit();
			_timer.Start();
		}

		/// <summary> Fires if the event if needed and closes the object </summary>
		~TimeoutAction() { Dispose(false); }

		/// <summary> Fires if the event if needed and closes the object </summary>
		public void Dispose() { _timer.Stop(); Dispose(true); }

		/// <summary> Fires if the event if needed and closes the object </summary>
		protected virtual void Dispose(bool disposing)
		{
			lock (this)
			{
				try
				{
					_perform();
				}
				catch (Exception ex)
				{
					try { _onerror(ex); }
					catch { }
				}
				finally
				{
					_perform = Ignore;
					_onerror = TraceException;
					if (disposing)
					{
						GC.SuppressFinalize(this);
						_timer.Dispose();
					}
				}
			}
		}

		private static void Ignore() { }

		private void TimerElapsed(object sender, EventArgs e)
		{
			Dispose();
		}

		private static void TraceException(Exception ex)
		{
			Trace.WriteLine(ex, typeof(TimeoutAction).FullName);
		}

		/// <summary> Enqueues a task to be performed at some time in the future </summary>
		public static TimeoutAction Start(TimeSpan wait, Action perform)
		{
			return Start(wait, perform, TraceException);
		}

		/// <summary> Enqueues a task to be performed at some time in the future </summary>
		public static TimeoutAction Start(TimeSpan wait, Action perform, Action<Exception> onerror)
		{
			return new TimeoutAction(wait, perform, onerror);
		}

		/// <summary> Enqueues a task to be performed at some time in the future </summary>
		public static TimeoutAction Start<T1>(TimeSpan wait, Action<T1> perform, T1 arg1)
		{
			return Start(wait, perform, arg1, TraceException);
		}

		/// <summary> Enqueues a task to be performed at some time in the future </summary>
		public static TimeoutAction Start<T1>(TimeSpan wait, Action<T1> perform, T1 arg1, Action<Exception> onerror)
		{
			return Start(wait, delegate() { perform(arg1); }, onerror);
		}
	}
}
