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
using NUnit.Framework;
using CSharpTest.Net.WinForms;

#pragma warning disable 1591

namespace CSharpTest.Net.Library.Test
{
	[TestFixture]
	public partial class TestWin32Window
	{
		[Test]
		public void TestNullFromIntPtrZero()
		{
			Assert.IsNull(Win32Window.FromHandle(IntPtr.Zero));
		}

		[Test]
		public void TestFromNonIntPtrZero()
		{
			IntPtr ptr = new IntPtr(0x01000);//any number but zero

			Assert.IsNotNull(Win32Window.FromHandle(ptr));
			Assert.AreEqual(ptr, Win32Window.FromHandle(ptr).Handle);
		}
	}
}
