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
using System.IO;
using System.Collections.Generic;
using CSharpTest.Net.Collections;
using CSharpTest.Net.Serialization;
using CSharpTest.Net.IO;
using CSharpTest.Net.Synchronization;
using System.Runtime.InteropServices;
using NUnit.Framework;
using CSharpTest.Net.Threading;
using System.Threading;
using CSharpTest.Net.Reflection;

namespace CSharpTest.Net.BPlusTree.Test
{
    [TestFixture]
    public class TestBackupAndRecovery
    {
        BPlusTree<Guid, TestInfo>.OptionsV2 GetOptions(TempFile temp)
        {
            BPlusTree<Guid, TestInfo>.OptionsV2 options = new BPlusTree<Guid, TestInfo>.OptionsV2(
                PrimitiveSerializer.Guid, new TestInfoSerializer());
            options.CalcBTreeOrder(Marshal.SizeOf(typeof(Guid)), Marshal.SizeOf(typeof(TestInfo)));
            options.CreateFile = CreatePolicy.IfNeeded;
            options.FileName = temp.TempPath;

            // The following three options allow for automatic commit/recovery:
            options.CallLevelLock = new ReaderWriterLocking();
            options.TransactionLogFileName = Path.ChangeExtension(options.FileName, ".tlog");
            return options;
        }

        static void Insert(BPlusTree<Guid, TestInfo> tree, IDictionary<Guid, TestInfo> testdata, int threads, int count, TimeSpan wait)
        {
            using (var work = new WorkQueue<IEnumerable<KeyValuePair<Guid, TestInfo>>>(tree.AddRange, threads))
            {
                foreach (var set in TestInfo.CreateSets(threads, count, testdata))
                    work.Enqueue(set);
                work.Complete(false, wait == TimeSpan.MaxValue ? Timeout.Infinite : (int)Math.Min(int.MaxValue, wait.TotalMilliseconds));
            }
        }

        [Test]
        public void TestWriteToTemporaryCopy()
        {
            Dictionary<Guid, TestInfo> first, data = new Dictionary<Guid, TestInfo>();
            using(TempFile temp = new TempFile())
            {
                temp.Delete();
                var options = GetOptions(temp);
                options.TransactionLogFileName = Path.ChangeExtension(options.FileName, ".tlog");

                using (var tree = new BPlusTree<Guid, TestInfo>(options))
                {
                    Insert(tree, data, 1, 100, TimeSpan.MaxValue);
                    TestInfo.AssertEquals(data, tree);
                    Assert.IsFalse(temp.Exists);
                }

                // All data commits to output file
                Assert.IsTrue(temp.Exists);
                TestInfo.AssertEquals(data, BPlusTree<Guid, TestInfo>.EnumerateFile(options));

                first = new Dictionary<Guid, TestInfo>(data);
                
                using (var tree = new BPlusTree<Guid, TestInfo>(options))
                {
                    Insert(tree, data, 1, 100, TimeSpan.MaxValue);

                    //We are writing to a backup, the original file still contains 100 items:
                    TestInfo.AssertEquals(first, BPlusTree<Guid, TestInfo>.EnumerateFile(options));

                    //Commit the changes and the original file will now contain our changes:
                    tree.CommitChanges();
                    TestInfo.AssertEquals(data, BPlusTree<Guid, TestInfo>.EnumerateFile(options));

                    //Add a few more records...
                    Insert(tree, data, 1, 100, TimeSpan.MaxValue);
                }
                //Dispose of the tree will commit changes...
                TestInfo.AssertEquals(data, BPlusTree<Guid, TestInfo>.EnumerateFile(options));
            }
        }

        [Test]
        public void TestRecoveryOnNewWithAsyncLog()
        {
            using (TempFile temp = new TempFile())
            {
                var options = GetOptions(temp);
                options.TransactionLog = new TransactionLog<Guid, TestInfo>(
                    new TransactionLogOptions<Guid, TestInfo>(
                        options.TransactionLogFileName,
                        options.KeySerializer,
                        options.ValueSerializer
                        ) {FileOptions = FileOptions.Asynchronous}
                    );
                TestRecoveryOnNew(options, 100, 0);
            }
        }

        [Test]
        public void TestRecoveryOnExistingWithAsyncLog()
        {
            using (TempFile temp = new TempFile())
            {
                var options = GetOptions(temp);
                options.TransactionLog = new TransactionLog<Guid, TestInfo>(
                    new TransactionLogOptions<Guid, TestInfo>(
                        options.TransactionLogFileName,
                        options.KeySerializer,
                        options.ValueSerializer
                        ) { FileOptions = FileOptions.Asynchronous }
                    );
                TestRecoveryOnExisting(options, 100, 0);
            }
        }

        [Test]
        public void TestRecoveryOnNew()
        {
            using (TempFile temp = new TempFile())
            {
                var options = GetOptions(temp);
                TestRecoveryOnNew(options, 10, 0);
            }
        }

        [Test]
        public void TestRecoveryOnExisting()
        {
            using (TempFile temp = new TempFile())
            {
                var options = GetOptions(temp);
                TestRecoveryOnExisting(options, 10, 0);
            }
        }


        [Test]
        public void TestRecoveryOnNewLargeOrder()
        {
            using (TempFile temp = new TempFile())
            {
                var options = GetOptions(temp);
                options.MaximumValueNodes = 255;
                options.MinimumValueNodes = 100;
                options.TransactionLog = new TransactionLog<Guid, TestInfo>(
                    new TransactionLogOptions<Guid, TestInfo>(
                        options.TransactionLogFileName,
                        options.KeySerializer,
                        options.ValueSerializer
                        ) { FileOptions = FileOptions.None } /* no-write through */
                    );
                TestRecoveryOnNew(options, 100, 10000);
            }
        }

        [Test]
        public void TestRecoveryOnExistingLargeOrder()
        {
            using (TempFile temp = new TempFile())
            {
                var options = GetOptions(temp);
                options.MaximumValueNodes = 255;
                options.MinimumValueNodes = 100;
                options.TransactionLog = new TransactionLog<Guid, TestInfo>(
                    new TransactionLogOptions<Guid, TestInfo>(
                        options.TransactionLogFileName,
                        options.KeySerializer,
                        options.ValueSerializer
                        ) { FileOptions = FileOptions.None } /* no-write through */
                    );
                TestRecoveryOnExisting(options, 100, ushort.MaxValue);
            }
        }

        void TestRecoveryOnNew(BPlusTree<Guid, TestInfo>.OptionsV2 options, int count, int added)
        {
            BPlusTree<Guid, TestInfo> tree = null;
            var temp = TempFile.Attach(options.FileName);
            Dictionary<Guid, TestInfo> data = new Dictionary<Guid, TestInfo>();
            try
            {
                Assert.IsNotNull(options.TransactionLog);
                temp.Delete();
                tree = new BPlusTree<Guid, TestInfo>(options);
                using (var log = options.TransactionLog)
                {
                    using ((IDisposable)new PropertyValue(tree, "_storage").Value)
                        Insert(tree, data, Environment.ProcessorCount, count, TimeSpan.MaxValue);
                    //Add extra data...
                    AppendToLog(log, TestInfo.Create(added, data));
                }
                tree = null;
                //No file... yet...
                Assert.IsFalse(File.Exists(options.FileName));
                //Now recover...
                using (var recovered = new BPlusTree<Guid, TestInfo>(options))
                {
                    TestInfo.AssertEquals(data, recovered);
                }

                Assert.IsTrue(File.Exists(options.FileName));
            }
            finally
            {
                temp.Dispose();
                if (tree != null)
                    tree.Dispose();
            }
        }

        void TestRecoveryOnExisting(BPlusTree<Guid, TestInfo>.OptionsV2 options, int count, int added)
        {
            BPlusTree<Guid, TestInfo> tree = null;
            var temp = TempFile.Attach(options.FileName);
            Dictionary<Guid, TestInfo> dataFirst, data = new Dictionary<Guid, TestInfo>();
            try
            {
                temp.Delete();
                Assert.IsNotNull(options.TransactionLog);

                using (tree = new BPlusTree<Guid, TestInfo>(options))
                {
                    Insert(tree, data, 1, 100, TimeSpan.MaxValue);
                    TestInfo.AssertEquals(data, tree);
                    Assert.IsFalse(temp.Exists);
                }
                tree = null;
                Assert.IsTrue(File.Exists(options.TransactionLogFileName));

                // All data commits to output file
                Assert.IsTrue(temp.Exists);
                TestInfo.AssertEquals(data, BPlusTree<Guid, TestInfo>.EnumerateFile(options));

                dataFirst = new Dictionary<Guid, TestInfo>(data);
                DateTime modified = temp.Info.LastWriteTimeUtc;

                tree = new BPlusTree<Guid, TestInfo>(options);
                using (var log = options.TransactionLog)
                {
                    using ((IDisposable) new PropertyValue(tree, "_storage").Value)
                        Insert(tree, data, Environment.ProcessorCount, count, TimeSpan.MaxValue);
                    //Add extra data...
                    AppendToLog(log, TestInfo.Create(added, data));
                }
                tree = null;

                //Still only contains original data
                Assert.AreEqual(modified, temp.Info.LastWriteTimeUtc);
                TestInfo.AssertEquals(dataFirst, BPlusTree<Guid, TestInfo>.EnumerateFile(options));

                //Now recover...
                using (var recovered = new BPlusTree<Guid, TestInfo>(options))
                {
                    TestInfo.AssertEquals(data, recovered);
                }
            }
            finally
            {
                temp.Dispose();
                if (tree != null)
                    tree.Dispose();
            }
        }

        private static void AppendToLog(ITransactionLog<Guid, TestInfo> log, IEnumerable<KeyValuePair<Guid, TestInfo>> keyValuePairs)
        {
            using(var items = keyValuePairs.GetEnumerator())
            {
                bool more = items.MoveNext();
                while (more)
                {
                    var tx = log.BeginTransaction();
                    int count = 1000;
                    do
                    {
                        log.AddValue(ref tx, items.Current.Key, items.Current.Value);
                        more = items.MoveNext();
                    } while (more && --count > 0);

                    log.CommitTransaction(ref tx);
                }
            }
        }
    }
}
