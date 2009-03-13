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
using NUnit.Framework;
using System.IO;
using CSharpTest.Net.Utils;

#pragma warning disable 1591
namespace CSharpTest.Net.Shared.Test
{
	[TestFixture]
	[Category("TestFileList")]
	public partial class TestFileList
	{
		readonly string BaseDirectory;

		public TestFileList() { BaseDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()); }

		#region TestFixture SetUp/TearDown
		[TestFixtureSetUp]
		public virtual void Setup()
		{
			byte[] bytes = new byte[0];

			Assert.IsFalse(Directory.Exists(BaseDirectory));
			Directory.CreateDirectory(BaseDirectory);

			File.WriteAllBytes(Path.Combine(BaseDirectory, "file1.txt"), bytes);

			string child = Path.Combine(BaseDirectory, "child1");
			Directory.CreateDirectory(child);
			new DirectoryInfo(child).Attributes |= FileAttributes.Hidden;
			File.WriteAllBytes(Path.Combine(child, "file1.txt"), bytes);
			File.WriteAllBytes(Path.Combine(child, "file2.dat"), bytes);
			File.WriteAllBytes(Path.Combine(child, "file3.xml"), bytes);
			File.WriteAllBytes(Path.Combine(child, "file4.ini"), bytes);

			child = Path.Combine(BaseDirectory, "child2");
			Directory.CreateDirectory(child);
			File.WriteAllBytes(Path.Combine(child, "file1.txt"), bytes);
			File.WriteAllBytes(Path.Combine(child, "file2.dat"), bytes);
			foreach (FileInfo f in new DirectoryInfo(child).GetFiles())
				f.Attributes |= FileAttributes.ReadOnly;
		}

		[TestFixtureTearDown]
		public virtual void Teardown()
		{
			try
			{
				foreach (FileInfo fi in new FileList(BaseDirectory))
					fi.Attributes &= ~FileAttributes.ReadOnly;
				Directory.Delete(BaseDirectory, true);
			}
			catch (Exception e)
			{
				Console.Error.WriteLine(e.ToString());
			}
		}
		#endregion

		[Test]
		public void Test()
		{
			FileList files = new FileList(BaseDirectory);
			Assert.AreEqual(3, files.Count);

			files = new FileList(Path.Combine(BaseDirectory, "file?.*"));
			Assert.AreEqual(3, files.Count);

			files = new FileList();
			files.Add(BaseDirectory);
			Assert.AreEqual(3, files.Count);

			files = new FileList(0, BaseDirectory);
			Assert.AreEqual(0, (int)files.ProhibitedAttributes);
			Assert.AreEqual(7, files.Count);

			files = new FileList();
			files.IgnoreFolderAttributes = true;
			Assert.IsTrue(files.IgnoreFolderAttributes);
			files.Add(BaseDirectory);
			Assert.AreEqual(7, files.Count);

			files = new FileList();
			files.RecurseFolders = false;
			Assert.IsFalse(files.RecurseFolders);
			files.Add(BaseDirectory);
			Assert.AreEqual(1, files.Count);

			files = new FileList();
			files.IgnoreFolderAttributes = true;
			files.FileFound += new EventHandler<FileList.FileFoundEventArgs>(
				delegate(object sender, FileList.FileFoundEventArgs e) { if(e.File.Extension != ".ini") e.Ignore = true; } );
			files.Add(BaseDirectory);
			Assert.AreEqual(new FileList(0, Path.Combine(BaseDirectory, "*.ini")).Count, files.Count);
			Assert.AreEqual(".ini", files[0].Extension);

			files = new FileList();
			files.ProhibitedAttributes = FileAttributes.Hidden;
			files.Add(BaseDirectory);
			Assert.AreEqual(3, files.Count);

			files = new FileList();
			files.ProhibitedAttributes = FileAttributes.ReadOnly;
			files.Add(BaseDirectory);
			Assert.AreEqual(5, files.Count);
			files.Add(Path.Combine(BaseDirectory, "file1.txt"));
			Assert.AreEqual(5, files.Count);
			Assert.AreEqual(5, files.ToArray().Length);

			files = new FileList(Path.Combine(BaseDirectory, "*.none"));
			Assert.AreEqual(0, files.Count);//nothing matching wildcard - does not throw FileNotFound
		}
	}

	[TestFixture]
	[Category("TestFileList")]
	public partial class TestFileListNegative
	{
		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void TestNullNew()
		{
			new FileList(null);
		}
		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void TestAddNull()
		{
			new FileList(null);
		}
		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void TestAddNull2()
		{
			new FileList().Add((string[])null);
		}
		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void TestAddNull3()
		{
			new FileList().Add((string)null);
		}
		[Test]
		[ExpectedException(typeof(FileNotFoundException))]
		public void TestAddDirNotFound()
		{
			new FileList().Add(Path.Combine(Path.Combine(@"C:\I don't exist", Guid.NewGuid().ToString()), Guid.NewGuid().ToString()));
		}
		[Test]
		[ExpectedException(typeof(FileNotFoundException))]
		public void TestAddFileNotFound()
		{
			//A file name that does not contain wildcards and does not exist will throw FileNotFound
			new FileList().Add(Path.Combine(@"C:\", Guid.NewGuid().ToString()));
		}
	}
}
