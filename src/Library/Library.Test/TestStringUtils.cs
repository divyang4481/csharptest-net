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
using CSharpTest.Net.Utils;

#pragma warning disable 1591
namespace CSharpTest.Net.Library.Test
{
	[TestFixture]
	[Category("TestStringUtils")]
	public partial class TestStringUtils
	{
		[Test]
		public void TestAlphaNumericOnly()
		{
			Assert.AreEqual("", StringUtils.AlphaNumericOnly("~!@#$%^&*()_+|}{	"));
			
			Assert.AreEqual("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890", 
				StringUtils.AlphaNumericOnly("abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ.1234567890"));
			
			Assert.AreEqual("ca761232ed4211cebacd00aa0057b223", 
				StringUtils.AlphaNumericOnly("{ca761232-ed42-11ce-bacd-00aa0057b223}"));
		}

		[Test]
		public void TestSafeFileName()
		{
			Assert.AreEqual(null, StringUtils.SafeFileName(null));
			Assert.AreEqual("--------------", StringUtils.SafeFileName("\b\t\r\n/\\:*?\"'<>|"));

			Assert.AreEqual("Greetings this is !@#)%~^@+)(",
				StringUtils.SafeFileName("Greetings this is !@#)%~^@+)("));

			Assert.AreEqual("-ca761232-ed42-11ce-bacd-00aa0057b223-",
				StringUtils.SafeFileName("<ca761232|ed42|11ce|bacd|00aa0057b223>"));
		}

		[Test]
		public void TestSafeFilePath()
		{
			Assert.AreEqual(String.Empty, StringUtils.SafeFilePath("\\"));

			Assert.AreEqual("----\\--------", StringUtils.SafeFilePath("\b\t\r\n/\\:*?\"'<>|"));

			Assert.AreEqual("Greetings this\\is !@#)%~^@+)(",
				StringUtils.SafeFilePath("Greetings this/is !@#)%~^@+)("));

			Assert.AreEqual("ca761232\\ed42-11ce-bacd\\00aa0057b223",
				StringUtils.SafeFilePath("/ca761232/ed42|11ce|bacd\\00aa0057b223\\"));
		}

		[Test]
		public void TestStringConverterApi()
		{
			Assert.AreEqual("5.1", StringUtils.ToString<double>(5.1));
			Assert.AreEqual("5.1", StringUtils.ToString((object)new Version(5, 1)));

			int ival;
			object oval;

			Assert.IsTrue(StringUtils.TryParse("1", out ival));
			Assert.AreEqual(1, ival);

			Assert.IsTrue(StringUtils.TryParse("1", typeof(int), out oval));
			Assert.AreEqual(1, oval);
		}
	}
}
