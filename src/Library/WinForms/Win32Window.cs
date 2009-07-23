#region Copyright 2008-2009 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Text;
using System.Windows.Forms;

namespace CSharpTest.Net.WinForms
{
	/// <summary>
	/// Provides a Windows.Forms implementation of the IWin32Window inteface for windows owned by
	/// a non-.Net window handle.
	/// </summary>
	public class Win32Window : IWin32Window
	{
		private readonly IntPtr _handle;

		private Win32Window(IntPtr handle) { _handle = handle; }

		/// <summary> Constructs an IWin32Window from a valid handle or returns null if handle == IntPtr.Zero </summary>
		public static IWin32Window FromHandle(IntPtr handle)
		{
			if (handle == IntPtr.Zero)
				return null;
			return new Win32Window(handle);
		}

		IntPtr IWin32Window.Handle { get { return _handle; } }
	}
}
