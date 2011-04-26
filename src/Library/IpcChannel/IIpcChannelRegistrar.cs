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
using System.Collections.Generic;

namespace CSharpTest.Net.IpcChannel
{
    /// <summary>
    /// Interface to provide a means of channel member registration and cross-process serialization
    /// of arguments for specific events.  Implementations must be thread-safe even across process
    /// boundaries.
    /// </summary>
    public interface IIpcChannelRegistrar
    {
        /// <summary> Registers a member (instanceId) for the provided channel name </summary>
        void RegisterInstance(string channelName, string instanceId, string instanceName);
        /// <summary> Unregisters a member (instanceId) from the provided channel name </summary>
        void UnregisterInstance(string channelName, string instanceId);

        /// <summary> Enumerates the registered instanceIds for the provided channel name </summary>
        IEnumerable<string> GetRegisteredInstances(string channelName);
        /// <summary> Enumerates the registered instanceIds who's name is instanceName for the provided channel name </summary>
        IEnumerable<string> GetRegisteredInstances(string channelName, string instanceName);
        /// <summary> Enumerates the registered instanceIds who's name is instanceName for the provided channel name </summary>
        IEnumerable<string> GetRegisteredInstances(string channelName, IEnumerable<string> instanceNames);

        /// <summary> Serializes the arguments for the event being sent to the specified instance </summary>
        bool WriteParameters(string channelName, string instanceId, string eventName, string[] arguments);
        /// <summary> Retreives the arguments for the event being sent to the specified instance </summary>
        string[] ReadParameters(string channelName, string instanceId, string eventName);
    }
}