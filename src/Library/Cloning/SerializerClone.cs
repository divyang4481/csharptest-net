#region Copyright 2010-2014 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
    /// Used to duplicate objects either by the ISerializable interface or by field-level duplication.
    /// </summary>
    public class SerializerClone : MemberwiseClone
    {
        readonly IDictionary<Type, MemberInfo[]> _fieldsCache;
        readonly IList<IDeserializationCallback> _callbacks;
        static readonly StreamingContext _sCtx = new StreamingContext(StreamingContextStates.Persistence);

        /// <summary>
        /// Used to duplicate objects either by the ISerializable interface or by field-level duplication.
        /// </summary>
        public SerializerClone()
        {
            _fieldsCache = new Dictionary<Type, MemberInfo[]>();
            _callbacks = new List<IDeserializationCallback>();
        }

        /// <summary>
        /// Public entry point to begin duplication of the object graph.  If your using this instance multiple
        /// times you should call Clear() between the object graphs or the copies previously made will be used.
        /// </summary>
        public override T Clone<T>(T instance)
        {
            T result;
            try
            {
                result = base.Clone<T>(instance);

                foreach (IDeserializationCallback cb in _callbacks)
                    cb.OnDeserialization(this);
            }
            finally
            {
                if (_callbacks.Count > 0)
                    _callbacks.Clear();
            }
            return result;
        }

        /// <summary>
        /// If the object provided is [Serializable] a simulated serialization routine is used to duplicate 
        /// the object, if it's not serializable then the MemberwiseClone base class will perform the copy.
        /// </summary>
        protected override object CloneDefault(object instance)
        {
            Type type = instance.GetType();

            if (!type.IsSerializable)
                return base.CloneDefault(instance);

            object copy;
            if (instance is ISerializable)
                copy = CloneWithISerializable(instance);
            else
                copy = CloneSerializableFields(instance);

            if (copy is IDeserializationCallback)
                _callbacks.Add((IDeserializationCallback)copy);

            return copy;
        }

        private object CloneSerializableFields(object instance)
        {
            object copy;
            Type type = instance.GetType();
            MemberInfo[] fields = SerializedFields(type);
            object[] values = FormatterServices.GetObjectData(instance, fields);

            copy = FormatterServices.GetUninitializedObject(type);
            Graph.Add(instance, copy);

            for (int i = 0; i < values.Length; i++)
                values[i] = this.CloneObject(values[i]);

            FormatterServices.PopulateObjectMembers(copy, fields, values);
            return copy;
        }

        private object CloneWithISerializable(object instance)
        {
            object copy;
            Type type = instance.GetType();
            SerializationInfo sInfo = new SerializationInfo(type, new FormatterConverter());
            ((ISerializable)instance).GetObjectData(sInfo, _sCtx);

            Type tcopy = Type.GetType(String.Format("{0}, {1}", sInfo.FullTypeName, sInfo.AssemblyName), true, false);

            copy = FormatterServices.GetUninitializedObject(tcopy);
            Graph.Add(instance, copy);

            SerializationInfo sCopy = new SerializationInfo(tcopy, new FormatterConverter());
            foreach (SerializationEntry se in sInfo)
                sCopy.AddValue(se.Name, this.CloneObject(se.Value), se.ObjectType);

            if (type == tcopy || typeof(ISerializable).IsAssignableFrom(tcopy))
            {
                ConstructorInfo ci = tcopy.GetConstructor(
                    BindingFlags.CreateInstance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null,
                    new Type[] { typeof(SerializationInfo), typeof(StreamingContext) }, null);
                if (ci == null)
                    throw new SerializationException("Serialization constructor not found.");
                ci.Invoke(copy, new object[] { sCopy, _sCtx });
            }
            else
            {
                List<MemberInfo> fields = new List<MemberInfo>();
                List<object> values = new List<object>();

                foreach(FieldInfo fi in SerializedFields(tcopy))
                {
                    try
                    {
                        object value = sCopy.GetValue(fi.Name, fi.FieldType);
                        fields.Add(fi);
                        values.Add(value);
                    }
                    catch (SerializationException) { }
                }

                FormatterServices.PopulateObjectMembers(copy, fields.ToArray(), values.ToArray());
            }

            if (copy is IObjectReference)
            {
                copy = ((IObjectReference)copy).GetRealObject(_sCtx);
                Graph[instance] = copy;
            }
            return copy;
        }

        private MemberInfo[] SerializedFields(Type t)
        {
            MemberInfo[] result;
            if (_fieldsCache.TryGetValue(t, out result))
                return result;
            return _fieldsCache[t] = FormatterServices.GetSerializableMembers(t);
        }
    }
}
