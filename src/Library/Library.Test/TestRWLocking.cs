#region Copyright 2010 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using CSharpTest.Net.Synchronization;
using NUnit.Framework;
using Thread = System.Threading.Thread;

namespace CSharpTest.Net.Library.Test
{
	[TestFixture]
	public class TestExclusiveLocking : TestReadWriteLocking
	{
		protected override ILockStrategy CreateLock() { return new ExclusiveLocking(); }
		protected override bool CanHaveMultipleReaders(ILockStrategy l) { return false; }
	}

	[TestFixture]
	public class TestIgnoreLocking : TestReadWriteLocking
	{
		protected override ILockStrategy CreateLock() { return new IgnoreLocking(); }
		protected override bool IgnoringLocking(ILockStrategy l) { return true; }
	}

	[TestFixture]
	public class TestReadWriteLocking
	{
		protected virtual bool IgnoringLocking(ILockStrategy l) { return false; }
		protected virtual bool CanHaveMultipleReaders(ILockStrategy l) { return true; }
		protected virtual ILockStrategy CreateLock() { return new ReadWriteLocking(); }

		private bool CanOtherThreadRead(ILockStrategy strategy)
		{
			bool locked = false;
			Thread t = new Thread(
				delegate()
					{
						IReadLock l;
						if(strategy.TryRead(TimeSpan.FromMilliseconds(0), out l))
						{ locked = true; l.Dispose(); }
					});

			t.Start();
			t.Join();

			return locked;
		}

		private bool CanOtherThreadWrite(ILockStrategy strategy)
		{
			bool locked = false;
			Thread t = new Thread(
				delegate()
				{
					IWriteLock l;
					if (strategy.TryWrite(TimeSpan.FromMilliseconds(0), out l))
					{ locked = true; l.Dispose(); }
				});

			t.Start();
			t.Join();

			return locked;
		}

		private class ThreadedReadLock : IDisposable
		{
			private System.Threading.ManualResetEvent _mreStart = new System.Threading.ManualResetEvent(false);
			private System.Threading.ManualResetEvent _mreStop = new System.Threading.ManualResetEvent(false);
			protected ILockStrategy _lock;
			public ThreadedReadLock(ILockStrategy l) 
			{
				_lock = l;
				new Thread(DoLock).Start();
				_mreStart.WaitOne();
			}
			public void Dispose() { _mreStop.Set(); }
			protected virtual void DoLock()
			{
				using (_lock.Read())
				{
					_mreStart.Set();
					_mreStop.WaitOne();
				}
			}
		}
		private class ThreadedWriteLock : ThreadedReadLock
		{
			public ThreadedWriteLock(ILockStrategy l) : base (l) { }
			protected override void DoLock()
			{
				using(_lock.Write())
					base.DoLock();
			}
		}


		[Test]
		public void TestRead()
		{
			ILockStrategy l = CreateLock();
			using(l.Read())
			{
				Assert.AreEqual(CanHaveMultipleReaders(l), CanOtherThreadRead(l));
				Assert.AreEqual(IgnoringLocking(l), CanOtherThreadWrite(l));
			}
			Assert.IsTrue(CanOtherThreadWrite(l));
		}

		[Test]
		public void TestWrite()
		{
			ILockStrategy l = CreateLock();
			using (l.Write())
			{
				Assert.AreEqual(IgnoringLocking(l), CanOtherThreadRead(l));
				Assert.AreEqual(IgnoringLocking(l), CanOtherThreadWrite(l));
			}
			Assert.IsTrue(CanOtherThreadWrite(l));
		}

		[Test]
		public void TestReadThenWrite()
		{
			ILockStrategy l = CreateLock();
			using (IReadLock r = l.Read())
			{
				Assert.AreEqual(CanHaveMultipleReaders(l), CanOtherThreadRead(l));
				Assert.AreEqual(IgnoringLocking(l), CanOtherThreadWrite(l));

				using (r.Write())
				{
					Assert.AreEqual(IgnoringLocking(l), CanOtherThreadRead(l));
					Assert.AreEqual(IgnoringLocking(l), CanOtherThreadWrite(l));
				}
			}
			Assert.IsTrue(CanOtherThreadWrite(l));
		}

		[Test]
		public void TestNesting()
		{
			ILockStrategy l = CreateLock();
			using (IReadLock r = l.Read())
			{
				Assert.AreEqual(CanHaveMultipleReaders(l), CanOtherThreadRead(l));
				Assert.AreEqual(IgnoringLocking(l), CanOtherThreadWrite(l));

				using (l.Read())
				{
					Assert.AreEqual(CanHaveMultipleReaders(l), CanOtherThreadRead(l));
					Assert.AreEqual(IgnoringLocking(l), CanOtherThreadWrite(l));

					using (r.Write())
					{
						Assert.AreEqual(IgnoringLocking(l), CanOtherThreadRead(l));
						Assert.AreEqual(IgnoringLocking(l), CanOtherThreadWrite(l));
					}
				}
				Assert.AreEqual(CanHaveMultipleReaders(l), CanOtherThreadRead(l));
				Assert.AreEqual(IgnoringLocking(l), CanOtherThreadWrite(l));

				using (r.Write())
				{
					Assert.AreEqual(IgnoringLocking(l), CanOtherThreadRead(l));
					Assert.AreEqual(IgnoringLocking(l), CanOtherThreadWrite(l));

					using (l.Read())
					{
						Assert.AreEqual(IgnoringLocking(l), CanOtherThreadRead(l));
						Assert.AreEqual(IgnoringLocking(l), CanOtherThreadWrite(l));

						using (l.Read())
						{ }

						Assert.AreEqual(IgnoringLocking(l), CanOtherThreadRead(l));
						Assert.AreEqual(IgnoringLocking(l), CanOtherThreadWrite(l));

						using (r.Write())
						{ }

						Assert.AreEqual(IgnoringLocking(l), CanOtherThreadRead(l));
						Assert.AreEqual(IgnoringLocking(l), CanOtherThreadWrite(l));
					}

					Assert.AreEqual(IgnoringLocking(l), CanOtherThreadRead(l));
					Assert.AreEqual(IgnoringLocking(l), CanOtherThreadWrite(l));
				}

				Assert.AreEqual(CanHaveMultipleReaders(l), CanOtherThreadRead(l));
				Assert.AreEqual(IgnoringLocking(l), CanOtherThreadWrite(l));
			}

			Assert.IsTrue(CanOtherThreadWrite(l));
		}

		[Test]
		public void TestTryRead()
		{
			ILockStrategy l = CreateLock();
			using (new ThreadedWriteLock(l))
			{
				IReadLock r;
				Assert.AreEqual(IgnoringLocking(l), l.TryRead(TimeSpan.Zero, out r));
			}
		}

		[Test]
		public void TestTryWrite()
		{
			ILockStrategy l = CreateLock();
			using (new ThreadedReadLock(l))
			{
				IWriteLock w;
				Assert.AreEqual(IgnoringLocking(l), l.TryWrite(TimeSpan.Zero, out w));
			}
		}

		[Test]
		public void TestTryReadToWrite()
		{
			ILockStrategy l = CreateLock();
			using (new ThreadedReadLock(l))
			{
				IReadLock r;
				IWriteLock w;

				bool locked = l.TryRead(TimeSpan.Zero, out r);
				Assert.AreEqual(CanHaveMultipleReaders(l), locked);
				if (locked)
					using (r)
						Assert.AreEqual(IgnoringLocking(l), r.TryWrite(TimeSpan.Zero, out w));
			}
		}

		[Test, ExpectedException(typeof(TimeoutException))]
		public void TestTryReadFails()
		{
			ILockStrategy l = CreateLock();
			if (IgnoringLocking(l))
				throw new TimeoutException(); //this will not throw, so we need to for derived test
			
			using (new ThreadedWriteLock(l))
			{
				l.TryRead(TimeSpan.Zero);
			}
		}

		[Test, ExpectedException(typeof(TimeoutException))]
		public void TestTryWriteFails()
		{
			ILockStrategy l = CreateLock();
			if (IgnoringLocking(l))
				throw new TimeoutException(); //this will not throw, so we need to for derived test
			
			using (new ThreadedReadLock(l))
			{
				IWriteLock w = l.TryWrite(TimeSpan.Zero);
			}
		}

		[Test, ExpectedException(typeof(TimeoutException))]
		public void TestTryReadToWriteFails()
		{
			ILockStrategy l = CreateLock();
			if (IgnoringLocking(l))
				throw new TimeoutException(); //this will not throw, so we need to for derived test
            
			using (new ThreadedReadLock(l))
			{
				using (IReadLock r = l.TryRead(TimeSpan.Zero))
					r.TryWrite(TimeSpan.Zero);
			}
		}
	}
}
