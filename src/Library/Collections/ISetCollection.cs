#region Copyright 2009 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
	public interface ISetCollection<TITEM, TSET> : IReadonlyCollection<TITEM>
		where TSET : ISetCollection<TITEM, TSET>
	{
		/// <summary> Returns a new collection adding the item provided </summary>
		TSET Add(TITEM item);
		
		/// <summary> Returns a new collection with the item provided removed </summary>
		TSET Remove(TITEM item);

		/// <summary> Returns the set of items that are in both this set and the provided set </summary>
		/// <example>{ 1, 2, 3 }.IntersectWith({ 2, 3, 4 }) == { 2, 3 }</example>
		TSET IntersectWith(TSET other);
		
		/// <summary> Returns the set of items that are in either this set or the provided set </summary>
		/// <example>{ 1, 2, 3 }.UnionWith({ 2, 3, 4 }) == { 1, 2, 3, 4 }</example>
		TSET UnionWith(TSET other);

		/// <summary> Returns the items in the provided set that are not in this set </summary>
		/// <example>{ 1, 2, 3 }.ComplementOf({ 2, 3, 4 }) == { 4 }</example>
		TSET ComplementOf(TSET other);

		/// <summary> Returns the items in this set that are not in the provided set </summary>
		/// <example>{ 1, 2, 3 }.RemoveAll({ 2, 3, 4 }) == { 1 }</example>
		TSET RemoveAll(TSET other);

		/// <summary> Returns the items in this set that are not in the provided set </summary>
		/// <example>{ 1, 2, 3 }.ExclusiveOrWith({ 2, 3, 4 }) == { 1, 4 }</example>
		TSET ExclusiveOrWith(TSET other);

		/// <summary> Returns true if all items in this set are also in the provided set </summary>
		/// <example>{ 1, 2 }.IsEqualTo({ 1, 2 }) == true &amp;&amp; {}.IsEqualTo({}) == true</example>
		bool IsEqualTo(TSET other);

		/// <summary> Returns true if all items in this set are also in the provided set </summary>
		/// <example>{ 1, 2, 4 }.IsSubsetOf({ 1, 2, 3, 4 }) == true &amp;&amp; {}.IsSubsetOf({ 1 }) == true</example>
		bool IsSubsetOf(TSET other);
		
		/// <summary> Returns true if all items in the provided set are also in this set </summary>
		/// <example>{ 1, 2, 3, 4 }.IsSupersetOf({ 1, 2, 4 }) == true &amp;&amp; { 1 }.IsSupersetOf({}) == true</example>
		bool IsSupersetOf(TSET other);
	}
}