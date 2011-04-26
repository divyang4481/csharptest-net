#region Copyright 2008-2011 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Diagnostics;

#pragma warning disable 1591
namespace CSharpTest.Net.Logging.Test
{
	[TestFixture]
	public partial class TraceListeningTest
	{
		protected String _lastTrace;
		private TraceListener _myListener;

		#region TestFixture SetUp/TearDown
		[TestFixtureSetUp]
		public virtual void Setup()
		{
			Trace.Listeners.Add(_myListener = new TraceListener(this));
		}

		[TestFixtureTearDown]
		public virtual void Teardown()
		{
			Trace.Listeners.Remove(_myListener);
		}
		#endregion

		class TraceListener : System.Diagnostics.TraceListener
		{
			private readonly TraceListeningTest _test;
			public TraceListener(TraceListeningTest test) { _test = test; }
			public override void Write(string message) { _test._lastTrace = message; }
			public override void WriteLine(string message) { Write(message); }
		}
	}
}
