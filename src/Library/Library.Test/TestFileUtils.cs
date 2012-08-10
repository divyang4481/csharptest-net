#region Copyright 2009-2012 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Security.AccessControl;
using System.Security.Principal;
using CSharpTest.Net.IO;

#pragma warning disable 1591
namespace CSharpTest.Net.Library.Test
{
	[TestFixture]
	public partial class TestFileUtils
	{
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

		[Test, System.Diagnostics.DebuggerNonUserCode ,ExpectedException(typeof(FileNotFoundException))]
		public void TestFindFullPathNotFound()
		{
			FileUtils.FindFullPath("This file hopefully doesn't exist on your hard drive!");
			Assert.Fail();
		}

		[Test]
		public void TestExpandEnvironment()
		{
			Assert.AreEqual(
				Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Test")),
				Path.GetFullPath(FileUtils.ExpandEnvironment(@"%PROGRAMFILES%\Test"))
				);
		}

		[Test]
		public void TestMakeRelativePath()
		{
			Assert.IsNull(FileUtils.MakeRelativePath(null, @"C:\Test\fileb.txt"));
			Assert.IsNull(FileUtils.MakeRelativePath(@"C:\Test\fileb.txt", null));

			Assert.AreEqual(
				@"filea.txt",
				FileUtils.MakeRelativePath(@"C:\Test\filea.txt", @"C:\Test\filea.txt")
				);
			Assert.AreEqual(
				@"fileb.txt",
				FileUtils.MakeRelativePath(@"C:\Test\filea.txt", @"C:\Test\fileb.txt")
				);
			Assert.AreEqual(
				@"..\fileb.txt",
				FileUtils.MakeRelativePath(@"C:\Test\filea.txt\", @"C:\Test\fileb.txt")
				);
			Assert.AreEqual(
				@"..\fileb.txt\",
				FileUtils.MakeRelativePath(@"C:\Test\filea.txt\", @"C:\Test\fileb.txt\")
				);
			Assert.AreEqual(
				@"fileb.txt\",
				FileUtils.MakeRelativePath(@"C:\Test\filea.txt", @"C:\Test\fileb.txt\")
				);
			Assert.AreEqual(
				@"sub\fileb.txt",
				FileUtils.MakeRelativePath(@"C:\Test\filea.txt", @"C:\Test\sub\fileb.txt")
				);
			Assert.AreEqual(
				@"..\fileb.txt",
				FileUtils.MakeRelativePath(@"C:\Test\sub\filea.txt", @"C:\Test\fileb.txt")
				);
			Assert.AreEqual(
				@"C:\Test\sub\fileb.txt",
				FileUtils.MakeRelativePath(@"E:\Test\sub\filea.txt", @"C:\Test\sub\fileb.txt")
				);
			Assert.AreEqual(
				@"..\test\fileb.txt",
				FileUtils.MakeRelativePath(@"sub\filea.txt", @"test\fileb.txt")
				);
			Assert.AreEqual(
				@"..\..\test\fileb.txt",
				FileUtils.MakeRelativePath(@"..\sub\filea.txt", @"test\fileb.txt")
				);
			Assert.AreEqual(
				@"fileb.txt",
				FileUtils.MakeRelativePath(@"sub\", @"sub\fileb.txt")
				);
			Assert.AreEqual(
				@"sub\fileb.txt",
				FileUtils.MakeRelativePath(@"sub", @"sub\fileb.txt")
				);
			Assert.AreEqual(
				@"sub\fileb.txt",
				FileUtils.MakeRelativePath(@".\sub", @"sub\fileb.txt")
				);
			Assert.AreEqual(
				@"..\sub\fileb.txt",
				FileUtils.MakeRelativePath(@"sub\test", @".\sub\.\..\sub\fileb.txt")
				);
		}

		[Test]
		public void TestGetAndReplacePermission()
		{
			string tempFile = Path.GetTempFileName();
			FileSystemRights rights;
			try
			{
				FileUtils.ReplacePermissions(tempFile, WellKnownSidType.WorldSid, FileSystemRights.Read);
				rights = FileUtils.GetPermissions(tempFile, WellKnownSidType.WorldSid);
				Assert.AreEqual(FileSystemRights.Read, FileSystemRights.Read & rights);

				FileUtils.ReplacePermissions(tempFile, WellKnownSidType.WorldSid, 0);
				rights = FileUtils.GetPermissions(tempFile, WellKnownSidType.WorldSid);
				Assert.AreEqual(0, (int)rights);

				FileUtils.GrantFullControlForFile(tempFile, WellKnownSidType.WorldSid);
				rights = FileUtils.GetPermissions(tempFile, WellKnownSidType.WorldSid);
				Assert.AreEqual(FileSystemRights.FullControl, rights);

				FileUtils.ReplacePermissions(tempFile, WellKnownSidType.WorldSid, FileSystemRights.Read);
				rights = FileUtils.GetPermissions(tempFile, WellKnownSidType.WorldSid);
				Assert.AreEqual(FileSystemRights.Read, FileSystemRights.Read & rights);

				FileUtils.GrantFullControlForFile(tempFile, WellKnownSidType.WorldSid);
				rights = FileUtils.GetPermissions(tempFile, WellKnownSidType.WorldSid);
				Assert.AreEqual(FileSystemRights.FullControl, rights);

				FileUtils.ReplacePermissions(tempFile, WellKnownSidType.WorldSid, 0);
				rights = FileUtils.GetPermissions(tempFile, WellKnownSidType.WorldSid);
				Assert.AreEqual(0, (int)rights);
			}
			finally
			{
				File.Delete(tempFile);
			}
		}

        bool TestExtension(string ext)
        {
            if (FileUtils.IsValidExtension(ext))
            {
                using(TempFile file = new TempFile())
                {
                    string newname = Path.ChangeExtension(file.TempPath, ext);
                    File.Create(newname).Dispose();
                    File.Delete(newname);
                }
                return true;
            }
            return false;
        }

        [Test]
        public void TestValidExtension()
        {
            Assert.IsFalse(TestExtension(null));
            Assert.IsFalse(TestExtension(""));
            Assert.IsFalse(TestExtension(".."));
            Assert.IsFalse(TestExtension("..."));
            Assert.IsFalse(TestExtension(". ."));
            Assert.IsFalse(TestExtension("a"));
            Assert.IsFalse(TestExtension(".a."));
            Assert.IsFalse(TestExtension("a.a"));
            Assert.IsFalse(TestExtension(".a/a"));
            Assert.IsFalse(TestExtension(".a|a"));

            Assert.IsTrue(TestExtension("."));
            Assert.IsTrue(TestExtension(". -"));
            Assert.IsTrue(TestExtension(".a"));
            Assert.IsTrue(TestExtension(".~!"));
            Assert.IsTrue(TestExtension(".a really really long file extension is actually ok"));
        }

        bool TestFileName(string name)
        {
            if (FileUtils.IsValidFileName(name))
            {
                string newname = Path.Combine(Path.GetTempPath(), name);

                Assert.AreEqual(Path.GetTempPath().Trim('\\'), new DirectoryInfo(newname).Parent.FullName);
                Assert.IsFalse(Directory.Exists(newname));

                Directory.CreateDirectory(newname);
                Directory.Delete(newname);

                File.Create(newname).Dispose();
                File.Delete(newname);
                return true;
            }
            return false;
        }

        [Test]
        public void TestValidFileName()
        {
            Assert.IsFalse(TestFileName(null));
            Assert.IsFalse(TestFileName(""));
            Assert.IsFalse(TestFileName("."));
            Assert.IsFalse(TestFileName(".."));
            Assert.IsFalse(TestFileName("..."));
            Assert.IsFalse(TestFileName(". . ."));
            Assert.IsFalse(TestFileName(" leading whitespace is bad"));
            Assert.IsFalse(TestFileName("trailing whitespace is bad\t"));
            Assert.IsFalse(TestFileName("a/a.txt"));
            Assert.IsFalse(TestFileName("a|a.txt"));

            Assert.IsTrue(TestFileName(".a"));
            Assert.IsTrue(TestFileName(". -"));
            Assert.IsTrue(TestFileName("..a"));
            Assert.IsTrue(TestFileName("a.."));
            Assert.IsTrue(TestFileName("No extension is fine"));
            Assert.IsTrue(TestFileName(". a leading dot is ok"));
            Assert.IsTrue(TestFileName("really really long file extension is actually ok."));
        }

        [Test]
        public void TestMakeValidFileName()
        {
            Assert.AreEqual("a...", FileUtils.MakeValidFileName("a..."));
            Assert.AreEqual("a. . .", FileUtils.MakeValidFileName("a. . . "));
            Assert.AreEqual("leading whitespace is bad", FileUtils.MakeValidFileName(" leading whitespace is bad"));
            Assert.AreEqual("trailing whitespace is bad", FileUtils.MakeValidFileName("trailing whitespace is bad\t"));
            Assert.AreEqual("a_a.txt", FileUtils.MakeValidFileName("a/a.txt", "_"));
            Assert.AreEqual("a_a.txt", FileUtils.MakeValidFileName("a|a.txt", "_"));
            Assert.AreEqual("a_a._txt", FileUtils.MakeValidFileName("a<|/\\*?>a.*txt", "_"));

            Assert.AreEqual(".a", FileUtils.MakeValidFileName(".a"));
            Assert.AreEqual(". -", FileUtils.MakeValidFileName(". -"));
            Assert.AreEqual("..a", FileUtils.MakeValidFileName("..a"));
            Assert.AreEqual("a..", FileUtils.MakeValidFileName("a.."));
            Assert.AreEqual("No extension is fine", FileUtils.MakeValidFileName("No extension is fine"));
            Assert.AreEqual(". a leading dot is ok", FileUtils.MakeValidFileName(". a leading dot is ok"));
            Assert.AreEqual("really really long file extension is actually ok.", FileUtils.MakeValidFileName("really really long file extension is actually ok."));
        }
	}
}
