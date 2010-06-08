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
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using CSharpTest.Net.Interfaces;

namespace CSharpTest.Net.Collections
{
	/// <summary> A readonly list of T </summary>
    public class ReadOnlyList<T> : IReadOnlyCollection<T>, ICloneable<ReadOnlyList<T>>
	{
		readonly IList<T> _list;

		/// <summary> Creates a readonly list of T by copying the enumeration into a List&lt;T> </summary>
		public ReadOnlyList(IEnumerable<T> collection) : this(new List<T>(collection), false) { }
		/// <summary> Creates a readonly list of T by copying the collection </summary>
		public ReadOnlyList(IList<T> list) : this(list, true) { }
		/// <summary> Creates a readonly list of T creating a copy if desired </summary>
		public ReadOnlyList(IList<T> list, bool clone)
		{
			if(!clone)
				_list = list;
			else
			{
				T[] items = new T[list.Count];
				list.CopyTo(items, 0);
				_list = items;
			}
		}

		/// <summary>
		/// Returns the element at the given offset
		/// </summary>
		public T this[int index]
		{
			get { return _list[index]; }
		}

		/// <summary>
		/// Returns the zero-based index of the item or -1 if not found.
		/// </summary>
		public int IndexOf(T item)
		{
			return _list.IndexOf(item);
		}

		/// <summary>
		/// Returns true if the list contains the specified element.
		/// </summary>
		public bool Contains(T item)
		{
			return _list.Contains(item);
		}

		/// <summary>
		/// Returns the collection as an array
		/// </summary>
		public T[] ToArray()
		{
			T[] items = new T[_list.Count];
			_list.CopyTo(items, 0);
			return items;
		}

		/// <summary>
		/// Returns an enumeration of the elements in the collection
		/// </summary>
		public IEnumerator<T> GetEnumerator()
		{
			return _list.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)_list).GetEnumerator();
		}

		/// <summary>
		/// Copy the elements in the collection to the specified array
		/// </summary>
		public void CopyTo(Array array, int index)
		{
			if (array is T[])
				_list.CopyTo((T[])array, index);
			else
				foreach (T item in this)
					array.SetValue(item, index++);
		}

		/// <summary>
		/// Returns the count of items contained in the collection
		/// </summary>
		public int Count
		{
			get { return _list.Count; }
		}

		bool ICollection.IsSynchronized
		{
			get { return false; }
		}

		object ICollection.SyncRoot
		{
			get { return this; }
		}

        #region ICloneable<T> Members

        /// <summary> Returns a shallow clone of this object </summary>
        public ReadOnlyList<T> Clone()
        {
            return new ReadOnlyList<T>(_list, false);
        }

        object ICloneable.Clone()
        {
            return this.Clone();
        }

        #endregion
    }
}
