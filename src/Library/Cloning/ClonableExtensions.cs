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
#if !NET20
    
    /// <summary>
    /// Extension method for DeepClone&lt;T>() on every object.
    /// </summary>
    public static class ClonableExtensions
    {
        /// <summary>
        /// Provides a deep-clone of objects using either serialization routines if available
        /// or memberwize cloning when serialization is not supported.
        /// See also: new SerializerClone().Clone&lt;T>(instance)
        /// </summary>
        public static T DeepClone<T>(this T instance)
        {
            return new SerializerClone().Clone<T>(instance);
        }
    }
#endif
}
