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
using System.Collections.Generic;

namespace CSharpTest.Net.Cloning
{
    /// <summary>
    /// A class that performs duplication of an entire object graph
    /// </summary>
    public abstract class ObjectCloner : IDisposable
    {
        readonly IDictionary<object, object> _graph;

        /// <summary>
        /// Creates the cloner
        /// </summary>
        protected ObjectCloner()
        {
            _graph = new Dictionary<object, object>(new ReferenceEqualityComparer());
        }

        /// <summary>
        /// Disposes of the instance and it's references to objects that have been duplicated
        /// </summary>
        public void Dispose() { Clear(); }
        /// <summary>
        /// Removes all instances from the object graph
        /// </summary>
        public virtual void Clear() { _graph.Clear(); }
        /// <summary>
        /// Add or Remove instances from the object graph, by adding this.Graph[o] = o; the instance 'o' will 
        /// not be duplicated.
        /// </summary>
        public IDictionary<object, object> Graph { [System.Diagnostics.DebuggerStepThrough] get { return _graph; } }

        /// <summary>
        /// Public entry point to begin duplication of the object graph.
        /// </summary>
        public virtual T Clone<T>(T instance)
        {
            try
            {
                return CloneObject<T>(instance);
            }
            finally { Clear(); }
        }

        /// <summary>
        /// Internal duplicate an object graph
        /// </summary>
        protected T CloneObject<T>(T instance)
        {
            object copy;

            if (Object.ReferenceEquals(instance, null) || instance is string || instance is DateTime ||
                instance is DateTimeOffset || instance is TimeSpan || instance is Boolean || instance is Byte ||
                instance is SByte || instance is Int16 || instance is UInt16 || instance is Int32 ||
                instance is UInt32 || instance is Int64 || instance is UInt64 || instance is IntPtr ||
                instance is Char || instance is Double || instance is Single || instance is MarshalByRefObject)
                return instance;

            if (_graph.TryGetValue(instance, out copy))
                return (T)copy;

            Type type = instance.GetType();

            if (instance is Array)
                return (T)(object)CloneArray((Array)(object)instance);
            else if (instance is Delegate)
                return (T)(object)CloneDelegate((Delegate)(object)instance);
            else
                return (T)CloneDefault(instance);
        }

        /// <summary>
        /// Provides the default behavior for duplicating an object and recording the
        /// duplication into the graph.
        /// </summary>
        protected abstract object CloneDefault(object inst);

        private Array CloneArray(Array instance)
        {
            Array copy = (Array)((ICloneable)instance).Clone();
            _graph.Add(instance, copy);

            int[] indexes = new int[copy.Rank];
            CloneArray(copy, indexes, 0);
            return copy;
        }

        private void CloneArray(Array copy, int[] indexes, int rank)
        {
            int stop = copy.GetUpperBound(rank);
            for (indexes[rank] = copy.GetLowerBound(rank); indexes[rank] <= stop; indexes[rank]++)
            {
                if ((rank + 1) < copy.Rank)
                    CloneArray(copy, indexes, rank + 1);
                else
                    copy.SetValue(this.CloneObject(copy.GetValue(indexes)), indexes);
            }
        }

        private Delegate CloneDelegate(Delegate method)
        {
            Delegate result = null;
            foreach (Delegate del in method.GetInvocationList())
            {
                Delegate delCopy = Delegate.CreateDelegate(del.GetType(), CloneObject(del.Target), del.Method, true);
                result = Delegate.Combine(result, delCopy);
            }

            _graph[method] = result;
            return result;
        }
    }
}
