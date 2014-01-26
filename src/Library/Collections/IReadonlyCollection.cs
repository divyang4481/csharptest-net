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

namespace CSharpTest.Net.Collections
{
	/// <summary>
	/// Provides common interface members for the implementation of a Set
	/// </summary>
	public interface IReadOnlyCollection<T> : IEnumerable<T>, System.Collections.ICollection, System.Collections.IEnumerable
	{
		/// <summary> Access an item by it's ordinal offset in the list </summary>
		T this[int index] { get; }

		/// <summary> Returns the zero-based index of the item or -1 </summary>
		int IndexOf(T item);

		/// <summary> Returns true if the item is already in the collection </summary>
		bool Contains(T item);

		/// <summary> Returns this collection as an array </summary>
		T[] ToArray();
	}
}
