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

#if NET20

namespace CSharpTest.Net.Delegates
{
    /// <summary> Encapsulates a method that takes no parameters and returns a value of the type specified by the TResult parameter. </summary>
    public delegate TResult Func<TResult>();
    /// <summary> Encapsulates a method that has one parameter and returns a value of the type specified by the TResult parameter. </summary>
    public delegate TResult Func<T1, TResult>(T1 arg1);
    /// <summary> Encapsulates a method that has two parameters and returns a value of the type specified by the TResult parameter. </summary>
    public delegate TResult Func<T1, T2, TResult>(T1 arg1, T2 arg2);
    /// <summary> Encapsulates a method that has three parameters and returns a value of the type specified by the TResult parameter. </summary>
    public delegate TResult Func<T1, T2, T3, TResult>(T1 arg1, T2 arg2, T3 arg3);
    /// <summary> Encapsulates a method that has four parameters and returns a value of the type specified by the TResult parameter. </summary>
    public delegate TResult Func<T1, T2, T3, T4, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
}

#endif