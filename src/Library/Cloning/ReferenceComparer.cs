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
using System.Collections.Generic;

namespace CSharpTest.Net.Cloning
{
    /// <summary>
    /// Dictionary comparer for comparing objects by reference reguardless of having
    /// the GetHashCode / Equals override implemented on an object.
    /// </summary>
    public sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        /// <summary>
        /// Returns true if the two objects are the same instance
        /// </summary>
        bool IEqualityComparer<object>.Equals(object x, object y)
        {
            return Object.ReferenceEquals(x, y);
        }

        /// <summary>
        /// Returns a hash code the instance of the object
        /// </summary>
        int IEqualityComparer<object>.GetHashCode(object obj)
        {
            return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
