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

namespace CSharpTest.Net.Delegates
{
#if NET20

    //Note - 2.0 already defines Action<T>

    /// <summary> Encapsulates a method that takes no parameters and does not return a value. </summary>
    public delegate void Action();
    /// <summary> Encapsulates a method that has two parameters and does not return a value. </summary>
    public delegate void Action<T1, T2>(T1 arg1, T2 arg2);
    /// <summary> Encapsulates a method that has three parameters and does not return a value. </summary>
    public delegate void Action<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3);
    /// <summary> Encapsulates a method that has four parameters and does not return a value. </summary>
    public delegate void Action<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4);

#endif
}
