#region Copyright 2010-2012 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Threading;

namespace CSharpTest.Net.Threading
{
    /// <summary> Describes a set of WaitHandles that, when signaled, trigger a process to continue </summary>
    public interface IWaitAndContinue : IDisposable
    {
        /// <summary>
        /// Returns true when the task is complete, this value may change between calls; however, the
        /// HandleCount can not change except inside a call to ContinueProcessing.
        /// </summary>
        bool Completed { get; }
        /// <summary>
        /// Returns the number of handles that will be copied when CopyHandles is called, this value
        /// is invariant except inside a call to ContinueProcessing.  Must not return 0 unless Completed
        /// is also true.
        /// </summary>
        int HandleCount { get; }
        /// <summary>
        /// Copies the wait handles that will signal that this object is ready to continue processing
        /// </summary>
        void CopyHandles(WaitHandle[] array, int offset);
        /// <summary> 
        /// Called after one of the wait handles is signaled, providing the wait handle that was signaled.
        /// For a Mutex, this may also occur when AbandonedMutexException is raised. 
        /// </summary>
        void ContinueProcessing(WaitHandle handleSignaled);
    }
}
