using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using CSharpTest.Net.Core;
using CSharpTest.Net.IO;

namespace CSharpTest.Net.Collections
{
    /// <summary>
    /// A resource for acquiring persisted transactions that can be reverted.  Used with
    /// a BPlusTree by providing this to the Options.TransactionFactory property and then
    /// constructing a transaction changes can be transacted across multiple BPlusTree 
    /// instances.  In addition to providing external transactions, the BPlusTree will
    /// also utilize this for persisting implicit transactions.  By using this with the
    /// BPlusTree the event of process crash or power failure causing corruption in the 
    /// storage layer can be remedied.
    /// </summary>
    public class BPlusTransactionFactory : IDisposable
    {
        readonly IDisposable _locked;
        readonly StreamCache _transactionStreams;
        readonly List<BPlusTransaction> _leftOvers;

        /// <summary>
        /// Constructs a transaction factory used either implicity by the BPlusTree or
        /// externally to transact several operations on one or more BPlusTree instances.
        /// </summary>
        /// <param name="transactionFolder">The folder where transaction.* files will be created</param>
        /// <param name="concurrentTransactions">The number of concurrent transactions allowed, 1-64</param>
        public BPlusTransactionFactory(string transactionFolder, int concurrentTransactions)
        {
            _leftOvers = new List<BPlusTransaction>();

            if (!Directory.Exists(transactionFolder))
                Directory.CreateDirectory(transactionFolder);

            //Ensure we are the only instance storing transactions in this directory.
            _locked = File.Open(Path.Combine(transactionFolder, "locked"), FileMode.Create, FileAccess.Write, FileShare.None);
            try
            {
                foreach (string file in Directory.GetFiles(transactionFolder, "transaction.???"))
                {
                    if (new FileInfo(file).Length == 0)
                    {
                        File.Delete(file);
                        continue;
                    }

                    //possible abandond transaction, time to rollback if needed.
                    Stream stream = File.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                    BPlusTransaction trans = null;

                    try
                    {
                        trans = new BPlusTransaction(stream, true);
                        if (trans.RollbackRequired)
                        {
                            GC.SuppressFinalize(trans);
                            _leftOvers.Add(trans);
                        }
                        else
                        {
                            trans.Dispose();
                            stream.Dispose();
                            File.Delete(file);
                        }
                    }
                    catch
                    {
                        if(trans != null)
                            trans.Dispose();

                        stream.Dispose();
                        System.Diagnostics.Trace.TraceError("Unable to recover transaction: {0}", file);

                        int index = 0;
                        string newName = file + ".corrupt{0:000}";
                        while (File.Exists(String.Format(newName, index)))
                            index++;

                        File.Move(file, String.Format(newName, index));
                    }
                }

                _transactionStreams = new StreamCache(new TransactionStreams(transactionFolder), Check.InRange(concurrentTransactions, 1, 64));
            }
            catch
            {
                _locked.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _locked.Dispose();
            _transactionStreams.Dispose();
        }

        /// <summary>
        /// Constructs a transaction instance that can be used with BPlusTree instances that are also
        /// using this transaction factory.
        /// </summary>
        public BPlusTransaction BeginTransaction()
        {
            return new BPlusTransaction(_transactionStreams.Open(), false);
        }

        internal void ConnectStorage(INodeStorage storageIn)
        {
            IPersistentNodeStorage storage = storageIn as IPersistentNodeStorage;
            if(storage == null) return;

            foreach (BPlusTransaction trans in _leftOvers)
            { }
        }

        class TransactionStreams : IFactory<Stream>
        {
            readonly string _directory;
            int _transactionId;

            public TransactionStreams(string transactionFolder)
            { _directory = transactionFolder; }

            public Stream Create()
            {
                while (true)
                {
                    int id = Interlocked.Increment(ref _transactionId);
                    string file = Path.Combine(_directory, String.Format("transaction.{0:000}", id));
                    if(!File.Exists(file))
                        return new BufferedStream(File.Open(file, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None), 16 * 1024);
                }
            }
        }
    }
}