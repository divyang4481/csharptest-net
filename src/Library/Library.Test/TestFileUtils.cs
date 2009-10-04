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
using System.IO;
using CSharpTest.Net.Utils;

#pragma warning disable 1591
namespace CSharpTest.Net.Library.Test
{
	[TestFixture]
	public partial class TestFileUtils
	{
		#region TestFixture SetUp/TearDown
		[TestFixtureSetUp]
		public virtual void Setup()
		{
		}

		[TestFixtureTearDown]
		public virtual void Teardown()
		{
		}
		#endregion

		[Test]
		public void TestFindFullPath()
		{
			string cmdexe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");
			cmdexe = Path.GetFullPath(cmdexe);
			Assert.IsTrue(File.Exists(cmdexe), "Not found: " + cmdexe);
			Assert.AreEqual(cmdexe.ToLower(), FileUtils.FindFullPath("cmd.exe").ToLower());
		}

		[Test]
		public void TestTrySearchPath()
		{
			string found, test;

			test = @"C:\This file hopefully doesn't exist on your hard drive!";
			Assert.IsFalse(File.Exists(test));
			Assert.IsFalse(FileUtils.TrySearchPath(test, out found));

			test = @"*<Illegal File Name?>*";
			Assert.IsFalse(FileUtils.TrySearchPath(test, out found));

			test = @"*"; //<= wild-cards not allowed.
			Assert.IsFalse(FileUtils.TrySearchPath(test, out found));

			test = @"????????.???"; //<= wild-cards not allowed.
			Assert.IsFalse(FileUtils.TrySearchPath(test, out found));
		}

		[Test][ExpectedException(typeof(FileNotFoundException))]
		public void TestFindFullPathNotFound()
		{
			FileUtils.FindFullPath("This file hopefully doesn't exist on your hard drive!");
			Assert.Fail();
		}
	}
}
