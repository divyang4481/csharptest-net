#region Copyright 2012 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Security.Principal;
using NUnit.Framework;
using CSharpTest.Net.IO;
using System.IO;
using System.Security.AccessControl;

namespace CSharpTest.Net.Library.Test
{
    [TestFixture]
    public class TestFindFile
    {
        private TempDirectory TempDir;
        private string TestFolder;

        #region TestFixture SetUp/TearDown
        [TestFixtureSetUp]
        public virtual void Setup()
        {
            TempDir = new TempDirectory();
            TestFolder = TempDir.TempPath;
            File.WriteAllText(Path.Combine(TestFolder, "a.1"), "a");
            File.WriteAllText(Path.Combine(TestFolder, "b.2"), "b");
            File.WriteAllText(Path.Combine(TestFolder, "c.3"), "c");
            Directory.CreateDirectory(Path.Combine(TestFolder, "child1"));
            File.WriteAllText(Path.Combine(TestFolder, @"child1\a.1"), "a");
            File.WriteAllText(Path.Combine(TestFolder, @"child1\b.2"), "b");
            File.WriteAllText(Path.Combine(TestFolder, @"child1\c.3"), "c");
            Directory.CreateDirectory(Path.Combine(TestFolder, @"child1\child2"));
        }

        [TestFixtureTearDown]
        public virtual void Teardown()
        {
            TempDir.Dispose();
        }
        #endregion


        [Test]
        public void TestFilesAndFolders()
        {
            int files = 0, folders = 0, total = 0;
            string dir = TestFolder;

            FindFile.FilesIn(dir, e => { Assert.IsTrue(File.Exists(e.FullPath)); files++; } );
            FindFile.FoldersIn(dir, e => { Assert.IsTrue(Directory.Exists(e.FullPath)); folders++; });
            FindFile.FilesAndFoldersIn(dir, e => { Assert.IsTrue(File.Exists(e.FullPath) || Directory.Exists(e.FullPath)); total++; });
            Assert.AreEqual(3, files);
            Assert.AreEqual(1, folders);
            Assert.AreEqual(4, total);
        }
        [Test]
        public void TestAllFilesAndFolders()
        {
            int files = 0, folders = 0, total = 0;
            string dir = TestFolder;

            FindFile.AllFilesIn(dir, e => { Assert.IsTrue(File.Exists(e.FullPath)); files++; });
            FindFile.AllFoldersIn(dir, e => { Assert.IsTrue(Directory.Exists(e.FullPath)); folders++; });
            FindFile.AllFilesAndFoldersIn(dir, e => { Assert.IsTrue(File.Exists(e.FullPath) || Directory.Exists(e.FullPath)); total++; });
            Assert.AreEqual(6, files);
            Assert.AreEqual(2, folders);
            Assert.AreEqual(8, total);
        }
        [Test]
        public void TestCancel()
        {
            int total = 0;
            FindFile.AllFilesAndFoldersIn(TestFolder, 
                e =>
                    {
                        Assert.IsTrue(File.Exists(e.FullPath));
                        total++; 
                        e.CancelEnumeration = true;
                    });
            Assert.AreEqual(1, total);
        }
        [Test]
        public void TestFileFoundArgs()
        {
            int ix = 0;
            FileInfo[] expected = new DirectoryInfo(TestFolder).GetFiles();
            FindFile.FilesIn(TestFolder,
                e =>
                    {
                        Assert.AreEqual(expected[ix].Attributes, e.Attributes);
                        Assert.AreEqual(expected[ix].FullName, e.FullPath);
                        Assert.AreEqual(expected[ix].Extension, e.Extension);
                        Assert.AreEqual(expected[ix].Name, e.Name);
                        Assert.AreEqual(expected[ix].Length, e.Length);
                        Assert.AreEqual(expected[ix].CreationTimeUtc, e.CreationTimeUtc);
                        Assert.AreEqual(expected[ix].LastAccessTimeUtc, e.LastAccessTimeUtc);
                        Assert.AreEqual(expected[ix].LastWriteTimeUtc, e.LastWriteTimeUtc);

                        Assert.AreEqual(FindFile.UncPrefix + expected[ix].FullName, e.FullPathUnc);
                        Assert.AreEqual(Path.GetDirectoryName(expected[ix].FullName).TrimEnd('\\'), e.ParentPath.TrimEnd('\\'));
                        Assert.AreEqual(FindFile.UncPrefix + Path.GetDirectoryName(expected[ix].FullName).TrimEnd('\\'), e.ParentPathUnc.TrimEnd('\\'));

                        Assert.AreEqual(false, e.CancelEnumeration);
                        Assert.AreEqual(false, e.IsCompressed);
                        Assert.AreEqual(false, e.IsDirectory);
                        Assert.AreEqual(false, e.IsEncrypted);
                        Assert.AreEqual(false, e.IsHidden);
                        Assert.AreEqual(false, e.IsOffline);
                        Assert.AreEqual(false, e.IsReadOnly);
                        Assert.AreEqual(false, e.IsReparsePoint);
                        Assert.AreEqual(false, e.IsSystem);
                        ix++;
                    });
            Assert.AreEqual(expected.Length, ix);
        }
        [Test]
        public void TestFolderFoundArgs()
        {
            int ix = 0;
            DirectoryInfo[] expected = new DirectoryInfo(TestFolder).GetDirectories();
            FindFile.FoldersIn(TestFolder,
                e =>
                {
                    Assert.AreEqual(expected[ix].Attributes, e.Attributes);
                    Assert.AreEqual(expected[ix].FullName, e.FullPath);
                    Assert.AreEqual(expected[ix].Extension, e.Extension);
                    Assert.AreEqual(expected[ix].Name, e.Name);
                    Assert.AreEqual(0, e.Length);
                    Assert.AreEqual(expected[ix].CreationTimeUtc, e.CreationTimeUtc);
                    Assert.AreEqual(expected[ix].LastAccessTimeUtc, e.LastAccessTimeUtc);
                    Assert.AreEqual(expected[ix].LastWriteTimeUtc, e.LastWriteTimeUtc);

                    Assert.AreEqual(FindFile.UncPrefix + expected[ix].FullName, e.FullPathUnc);
                    Assert.AreEqual(Path.GetDirectoryName(expected[ix].FullName).TrimEnd('\\'), e.ParentPath.TrimEnd('\\'));
                    Assert.AreEqual(FindFile.UncPrefix + Path.GetDirectoryName(expected[ix].FullName).TrimEnd('\\'), e.ParentPathUnc.TrimEnd('\\'));

                    Assert.AreEqual(false, e.CancelEnumeration);
                    Assert.AreEqual(false, e.IsCompressed);
                    Assert.AreEqual(true, e.IsDirectory);
                    Assert.AreEqual(false, e.IsEncrypted);
                    Assert.AreEqual(false, e.IsHidden);
                    Assert.AreEqual(false, e.IsOffline);
                    Assert.AreEqual(false, e.IsReadOnly);
                    Assert.AreEqual(false, e.IsReparsePoint);
                    Assert.AreEqual(false, e.IsSystem);
                    ix++;
                });
            Assert.AreEqual(expected.Length, ix);
        }
        [Test]
        public void TestFindFileInfo()
        {
            int ix = 0;
            FileInfo[] expected = new DirectoryInfo(TestFolder).GetFiles("*");
            FindFile.FilesIn(TestFolder,
                args =>
                    {
                        FindFile.Info e = args.GetInfo();
                        Assert.AreEqual(expected[ix].Attributes, e.Attributes);
                        Assert.AreEqual(expected[ix].FullName, e.FullPath);
                        Assert.AreEqual(expected[ix].Extension, e.Extension);
                        Assert.AreEqual(expected[ix].Name, e.Name);
                        Assert.AreEqual(expected[ix].Length, e.Length);
                        Assert.AreEqual(expected[ix].CreationTimeUtc, e.CreationTimeUtc);
                        Assert.AreEqual(expected[ix].LastAccessTimeUtc, e.LastAccessTimeUtc);
                        Assert.AreEqual(expected[ix].LastWriteTimeUtc, e.LastWriteTimeUtc);

                        Assert.AreEqual(FindFile.UncPrefix + expected[ix].FullName, e.FullPathUnc);
                        Assert.AreEqual(Path.GetDirectoryName(expected[ix].FullName).TrimEnd('\\'), e.ParentPath.TrimEnd('\\'));
                        Assert.AreEqual(FindFile.UncPrefix + Path.GetDirectoryName(expected[ix].FullName).TrimEnd('\\'), e.ParentPathUnc.TrimEnd('\\'));
                        ix++;
                });
            Assert.AreEqual(expected.Length, ix);
        }
        [Test]
        public void TestFindFileProperties()
        {
            FindFile ff = new FindFile();
            Assert.AreEqual("", ff.BaseDirectory);
            Assert.AreEqual("*", ff.FilePattern);
            Assert.AreEqual(true, ff.Recursive);
            Assert.AreEqual(true, ff.IncludeFiles);
            Assert.AreEqual(true, ff.IncludeFolders);
            Assert.AreEqual(false, ff.RaiseOnAccessDenied);
            Assert.AreEqual(4096, ff.MaxPath);

            Assert.AreEqual(TestFolder, ff.BaseDirectory = TestFolder);
            Assert.AreEqual("a.*", ff.FilePattern = "a.*");
            Assert.AreEqual(false, ff.Recursive = false);
            Assert.AreEqual(false, ff.IncludeFiles = false);
            Assert.AreEqual(false, ff.IncludeFolders = false);
            Assert.AreEqual(true, ff.RaiseOnAccessDenied = true);
            Assert.AreEqual(1024, ff.MaxPath = 1024);

            ff.FileFound += (o, e) => Assert.Fail("Should not find anything.");
            ff.Find();
        }
        [Test]
        public void TestFindFileByPattern()
        {
            List<string> found = new List<string>();
            FindFile ff = new FindFile(TestFolder, "a.*", true, true, true);
            ff.FileFound += (o, e) => found.Add(e.FullPath);
            ff.Find();

            Assert.AreEqual(2, found.Count);
        }
        [Test]
        public void TestFileNotFound()
        {
            FindFile ff = new FindFile(TestFolder, "foo", false, false);
            ff.FileFound += (o, e) => Assert.Fail("Should not find a file");
            ff.Find();
        }
        [Test]
        public void TestPathNotFound()
        {
            FindFile ff = new FindFile(Path.Combine(TestFolder, "foo"), "*", false);
            ff.FileFound += (o, e) => Assert.Fail("Should not find a file");
            ff.Find();
        }
        [Test]
        public void TestDriveNotFound()
        {
            FindFile ff = new FindFile("ZZZ:\\WTF", "*");
            ff.FileFound += (o, e) => Assert.Fail("Should not find a file");
            ff.Find();
        }
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void TestBadPath()
        {
            FindFile ff = new FindFile("C:\\?#%)(#!&_~&)@%^&%~_#()!$\"*@(#_)~*");
            ff.FileFound += (o, e) => Assert.Fail("Should not find a file");
            ff.Find();
        }
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void TestBadFilePattern()
        {
            FindFile ff = new FindFile();
            ff.FileFound += (o, e) => Assert.Fail("Should not find a file");
            ff.Find("\":^|&");
        }
        [Test, ExpectedException(typeof(System.ComponentModel.Win32Exception))]// : The network path was not found
        public void TestServerNotFound()
        {
            FindFile ff = new FindFile(@"\\127.0.0.2\C$", "*", false, false);
            ff.FileFound += (o, e) => Assert.Fail("Should not find a file");
            ff.Find();
        }
        [Test, ExpectedException(typeof(System.ComponentModel.Win32Exception))]// : The specified path is invalid
        public void TestPathError()
        {
            FindFile ff = new FindFile(@"\\\", "*", false, false);
            ff.FileFound += (o, e) => Assert.Fail("Should not find a file");
            ff.Find();
        }
        [Test]
        public void TestAccessDenied()
        {
            string child2 = Path.Combine(TestFolder, @"child1\child2");
            
            DirectorySecurity acl = Directory.GetAccessControl(child2);
            byte[] original = acl.GetSecurityDescriptorBinaryForm();
            acl.AddAccessRule(
                new FileSystemAccessRule(
                    new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                    FileSystemRights.ListDirectory,
                    AccessControlType.Deny)
                );
            Directory.SetAccessControl(child2, acl);
            try
            {
                //By default it ignores AccessDenied
                FindFile ff = new FindFile(child2, "*", false, false);
                ff.FileFound += (o, e) => Assert.Fail("Should not find a file");
                ff.Find();

                //Now raise the AccessDenied
                ff.RaiseOnAccessDenied = true;
                try
                {
                    ff.Find();
                    Assert.Fail("Should throw Access Denied.");
                }
                catch(System.ComponentModel.Win32Exception we)
                { Assert.AreEqual(5, we.NativeErrorCode); }
            }
            finally
            {
                acl.SetSecurityDescriptorBinaryForm(original, AccessControlSections.All);
                Directory.SetAccessControl(child2, acl);
            }
        }
    }
}
