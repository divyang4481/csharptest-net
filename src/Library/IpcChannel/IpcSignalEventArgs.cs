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

namespace CSharpTest.Net.IpcChannel
{
    /// <summary>
    /// Represents an event raised by the IpcEventChannel to subscribers.
    /// </summary>
    public class IpcSignalEventArgs : EventArgs
    {
        private readonly IpcEventChannel _channel;
        private readonly string _name;
        private readonly string[] _arguments;

        /// <summary> Creates the event </summary>
        internal IpcSignalEventArgs(IpcEventChannel channel, string name, string[] args)
        {
            _channel = channel;
            _name = name;
            _arguments = (string[])args.Clone();
        }

        /// <summary> Gets the channel rasing the event </summary>
        public IpcEventChannel EventChannel { get { return _channel; } }
        /// <summary> Gets the name of the event </summary>
        public string EventName { get { return _name; } }
        /// <summary> Gets any arguments sent with the event </summary>
        public string[] Arguments { get { return (string[])_arguments.Clone(); } }
    }
}