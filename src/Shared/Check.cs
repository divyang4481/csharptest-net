#region Copyright 2008 by Roger Knapp, Licensed under the Apache License, Version 2.0
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

/// <summary>
/// provides a set of runtime validations for inputs
/// </summary>
[System.Diagnostics.DebuggerNonUserCode]
[System.Diagnostics.DebuggerStepThrough]
internal static partial class Check
{
	public static T NotNull<T>(T value)
	{
		if (value == null) throw new ArgumentNullException();
		return value;
	}

	public static string NotEmpty(string value)
	{
		if (value == null) throw new ArgumentNullException();
		if (value.Length == 0) throw new ArgumentOutOfRangeException();
		return value;
	}

	public static T NotEmpty<T>(T value) where T : System.Collections.IEnumerable
	{
		if (value == null) throw new ArgumentNullException();
		if (!value.GetEnumerator().MoveNext()) throw new ArgumentOutOfRangeException();
		return value;
	}

	public static void IsEqual<T>(T a, T b) where T : IComparable<T>
	{
		if (0 != a.CompareTo(b))
			throw new ArgumentException();
	}
}
