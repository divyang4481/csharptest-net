#region Copyright 2010 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Reflection;
using System.Runtime.Serialization;

namespace CSharpTest.Net.Cloning
{
    /// <summary>
    /// Provides a deep-copy, field-level duplication of any object
    /// </summary>
    public class MemberwiseClone : ObjectCloner
    {
        readonly IDictionary<Type, MemberInfo[]> _fieldsCache;

        /// <summary>
        /// Provides a deep-copy, field-level duplication of any object
        /// </summary>
        public MemberwiseClone()
        {
            _fieldsCache = new Dictionary<Type, MemberInfo[]>();
        }

        /// <summary>
        /// Routine to clone an objects fields and their contents
        /// </summary>
        protected override object CloneDefault(object instance)
        {
            Type type = instance.GetType();
            object copy = FormatterServices.GetUninitializedObject(type);
            Graph.Add(instance, copy);

            MemberInfo[] fields = ClonableFields(type);
            object[] values = FormatterServices.GetObjectData(instance, fields);

            for (int i = 0; i < values.Length; i++)
                values[i] = this.CloneObject(values[i]);

            FormatterServices.PopulateObjectMembers(copy, fields, values);
            return copy;
        }

        private MemberInfo[] ClonableFields(Type type)
        {
            MemberInfo[] result;
            if (_fieldsCache.TryGetValue(type, out result))
                return result;

            List<MemberInfo> fields = new List<MemberInfo>();
            Type t = type;
            while (t != null && t != typeof(Object))
            {
                fields.AddRange(
                    t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));
                t = t.BaseType;
            }

            return _fieldsCache[type] = fields.ToArray();
        }
    }
}
