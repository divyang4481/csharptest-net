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
    /// Provides a means of subscribing to a named event on an IpcEventChannel, access via:
    /// IpcEventChannel["Name"].OnEvent += new IpcSignalEventArgs(MyHandler);
    /// </summary>
    public abstract class IpcEvent
    {
        private readonly string _localName;
        /// <summary>Not intended for external use.</summary>
        internal protected IpcEvent(string localName)
        { 
            _localName = localName; 
        }

        /// <summary> Retrieves the name of this event </summary>
        public string LocalName { get { return _localName; } }
        /// <summary> Allows you to subscribe or unsubscribe to the event </summary>
        public event EventHandler<IpcSignalEventArgs> OnEvent;

        /// <summary>Not intended for external use.</summary>
        protected void RaiseEvent(IpcEventChannel channel, params string[] args)
        {
            EventHandler<IpcSignalEventArgs> exec = OnEvent;
            if (exec != null)
                exec(channel, new IpcSignalEventArgs(channel, LocalName, args));
        }

        /// <summary> Used to compare IpcEvent instances by name </summary>
        public static readonly System.Collections.IComparer Comparer = new IpcSignalComparer();
        private class IpcSignalComparer : System.Collections.IComparer
        {
            public int Compare(object x, object y)
            {
                string a = x as string;
                string b = y as string;
                if (a == null && x is IpcEvent) a = ((IpcEvent)x).LocalName;
                if (b == null && y is IpcEvent) b = ((IpcEvent)y).LocalName;
                return StringComparer.Ordinal.Compare(a, b);
            }
        }
    }
}