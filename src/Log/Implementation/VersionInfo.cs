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
using System.Security.Cryptography;
using System.Reflection;

namespace CSharpTest.Net.Logging.Implementation
{
	/// <summary>
	/// This is a small utility class for computing a hash of the fields to prevent different versions from
	/// having issues durring de-serialization.
	/// </summary>
	class VersionInfo
	{
		public static readonly int FieldCount;
		public static readonly string CheckSum;
		public static readonly Type[] FieldTypes;

		static VersionInfo()
		{
			// Now we are certian things look good, we need to build some static information about
			// the EventData type for use by the serialization routines as well as other systems.
			try
			{
				Type EventDataType = typeof(EventData);
				string[] fields = Enum.GetNames(typeof(LogFields));
				FieldCount = 1 + fields.Length;
				string all = String.Join(",", fields);
				byte[] hash = MD5.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(all));
				CheckSum = Convert.ToBase64String(hash);

				FieldTypes = new Type[FieldCount];
				for (int i = 0; i < fields.Length; i++)
				{
					object[] attrs = null;
					MemberInfo[] member = typeof(LogFields).GetMember(fields[i]);
					if (member != null && member.Length == 1)
						attrs = member[0].GetCustomAttributes(typeof(LogFieldAttribute), false);
					if (attrs != null && attrs.Length == 1 && attrs[0] is LogFieldAttribute)
						FieldTypes[i + 1] = ((LogFieldAttribute)attrs[0]).SerializeType;
				}
			}
			catch (Exception e)
			{
				System.Diagnostics.Trace.Fail(e.ToString());
			}
		}

		public static void Assert()
		{
			// This is basically a run-time unit-test of sorts... It asserts that the fields/properties
			// on the EventData class match exactly with the enumeration LogFields members.  This is 
			// critical to correct behavior and since I'm prone to mistakes this keeps me honest.
			try
			{
				int[] fieldIds = (int[])Enum.GetValues(typeof(LogFields));
				int fieldCount = fieldIds.Length + 1;

				int[] dupTest = new int[fieldCount];
				dupTest[0] = 1;//reserved index for this
				foreach (int i in fieldIds)
					System.Diagnostics.Trace.Assert(1 == ++dupTest[i], String.Format("The LogField index {0} is used more than once.", i));

				foreach (PropertyInfo pi in typeof(EventData).GetProperties())
					Enum.Parse(typeof(LogFields), pi.Name);

				foreach (FieldInfo fi in typeof(EventData).GetFields())
					Enum.Parse(typeof(LogFields), fi.Name);

				for (int i = 1; i < fieldCount; i++)
				{
					string name = Enum.GetName(typeof(LogFields), i);
					System.Diagnostics.Trace.Assert(1 == typeof(EventData).GetMember(name).Length, String.Format("The field {0} is not defined.", name));
				}
			}
			catch (Exception e)
			{
				System.Diagnostics.Trace.Fail(e.ToString());
			}
		}
	}
}
