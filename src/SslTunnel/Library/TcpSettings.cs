#region Copyright 2009-2014 by Roger Knapp, Licensed under the Apache License, Version 2.0
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

namespace CSharpTest.Net.SslTunnel
{
    /// <summary>
    /// Some basic global timeout setting that can be modified.
    /// </summary>
	public static class TcpSettings
	{
        /// <summary>
        /// Timeout when recieving data from foreign systems
        /// </summary>
		public static int ReadTimeout = 30000;
        /// <summary>
        /// Timeout when writting data to foreign systems
        /// </summary>
		public static int WriteTimeout = 30000;
        /// <summary>
        /// Timeout used to close an inactive connection
        /// </summary>
		public static int ActivityTimeout = 60000;
	}
}
