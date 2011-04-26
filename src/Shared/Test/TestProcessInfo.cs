#region Copyright 2009-2011 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using CSharpTest.Net.Utils;

namespace CSharpTest.Net.Utils.Test
{
	/// <summary> Test for ProcessInfo class</summary>
	[TestFixture]
	[Category("TestProcessInfo")]
	public partial class TestProcessInfo
	{
		/// <summary> Straight prop-equal test will only work when running nunit-console. </summary>
		[Test]
		public void Test()
		{
			ProcessInfo info = new ProcessInfo();

			Assert.AreNotEqual(0, info.ProcessId);
			Assert.AreEqual("nunit-console.exe", info.ProcessName);
			Assert.IsTrue(info.ProcessFile.EndsWith(@"\nunit-console.exe", StringComparison.OrdinalIgnoreCase));
			Assert.AreEqual("domain-CSharpTest.Net.Shared.Test.dll", info.AppDomainName);
			Assert.IsTrue(StringComparer.OrdinalIgnoreCase.Equals("nunit.core", info.EntryAssembly.GetName().Name));
			Assert.AreEqual(new Version(2,4,0,2), info.ProductVersion);
			Assert.AreEqual("NUnit", info.ProductName);
			Assert.AreEqual("NUnit.org", info.CompanyName);
			//Assert.AreEqual("", info.IsDebugging);
			Assert.AreEqual(@"Software\NUnit.org\NUnit", info.RegistrySoftwarePath);
			
			Assert.IsTrue(info.ApplicationData.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), StringComparison.OrdinalIgnoreCase));
			Assert.IsTrue(info.ApplicationData.EndsWith(@"\NUnit.org\NUnit", StringComparison.OrdinalIgnoreCase));
			
			Assert.IsTrue(info.LocalApplicationData.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), StringComparison.OrdinalIgnoreCase));
			Assert.IsTrue(info.LocalApplicationData.EndsWith(@"\NUnit.org\NUnit", StringComparison.OrdinalIgnoreCase));
			
			Assert.IsTrue(info.DefaultLogFile.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(info.DefaultLogFile.EndsWith(@"\NUnit.org\NUnit\domain-CSharpTest.Net.Shared.Test.dll.txt", StringComparison.OrdinalIgnoreCase));
		}
	}
}
