#region Copyright 2013-2014 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Threading;

namespace CSharpTest.Net.Threading
{
    /// <summary>
    /// A low-precision, but fast, counter that estimates how often the Increment() method is being
    /// called on this instance.  The precision is roughly +/- 1.7% for every 1000 calls/sec; 
    /// so at 20000 calls/second expect the Value property to be +/- 34%.  If you looking for 
    /// something cheap to use to 'push-back' on overbearing clients this should suite most needs. 
    /// If more accuracy is desired at higher volumes, specify the sampleFreq to scale the data
    /// samples under 1000/sec.
    /// </summary>
    /// <remarks>
    /// Yes you can build this with a Stopwatch.GetTimestamp() instead of DateTime.UtcNow; hoewever
    /// the Stopwatch API is 100x slower than using DateTime.UtcNow and although more precise, it 
    /// generally does not provide significant gains in the sub 50k call range.  Generally you can
    /// just increase sampleFreq to make up for the lack of precision in DateTime.UtcNow.
    /// </remarks>
    public class CallsPerSecondCounter
    {
        private long _writeIx;
        private readonly int _sampleFreq;
        private readonly int _sampleSize;
        private readonly long[] _samples;
        private readonly long _sampleScale;

        /// <summary>
        /// Creates a CallsPerSecondCounter with a specified sample size.  A reasonable sample size
        /// is 100, although even as low as 25 will often yeild reasonable results.  The higher the
        /// sample size, the longer it takes for Value to drop to zero, for a sample size of 100 the
        /// counter value will reach zero aprox 50 seconds after the last activity.
        /// </summary>
        public CallsPerSecondCounter([System.ComponentModel.DefaultValue(50)] int sampleSize) : this(sampleSize, 1)
        { }

        /// <summary>
        /// Creates a CallsPerSecondCounter with a specified sample size and freqency of collection.
        /// A reasonable sample size is 100, although even as low as 25 will often yeild reasonable 
        /// results.  The higher the sample size, the longer it takes for Value to drop to zero, for 
        /// a sample size of 100 the counter value will reach zero aprox 50 seconds after the last 
        /// activity.  The sampleFreq defines how often a sample is taken, for instance a value of 5
        /// means to collect a sample only every 5th call to Increment() whereas a value of 1 will
        /// collect on every call.  For high sampling rates this value can be increased to improve
        /// accuracy; however, it will also multiply the warm-up and cool-down time required for the
        /// Value property to move off of zero or move to zero.
        /// </summary>
        public CallsPerSecondCounter([System.ComponentModel.DefaultValue(50)] int sampleSize, int sampleFreq)
        {
            if (sampleSize <= 0) throw new ArgumentOutOfRangeException("sampleSize");
            if (sampleFreq <= 0) throw new ArgumentOutOfRangeException("sampleFreq");
            _sampleSize = sampleSize;
            _sampleFreq = sampleFreq;
            _sampleScale = TimeSpan.TicksPerSecond*_sampleFreq*_sampleSize/2;

            _writeIx = 0;
            _samples = new long[_sampleSize];
            var now = DateTime.UtcNow.AddDays(-1).Ticks;
            for (int i = 0; i < _sampleSize; i++)
                _samples[i] = now;
        }

        /// <summary>
        /// Returns an estimated number of times per second the Increment() call is being used.
        /// </summary>
        public long Value
        {
            get
            {
                double avg = 0;
                long before = DateTime.UtcNow.Ticks;

                for (int i = 0; i < _sampleSize; i++)
                    avg += Interlocked.CompareExchange(ref _samples[i], 0, 0);

                long now = (DateTime.UtcNow.Ticks + before) / 2;
                avg = avg / _sampleSize;
                double delta = now - avg;
                if (delta <= 0) return TimeSpan.TicksPerSecond;
                double result = _sampleScale / delta;
                return (long)(result + 0.5);
            }
        }

        /// <summary>
        /// Called to increment the usage by 1.
        /// </summary>
        public void Increment()
        {
            var ix = Interlocked.Increment(ref _writeIx);
            if (ix % _sampleFreq == 0)
            {
                Interlocked.Exchange(
                    ref _samples[(ix/_sampleFreq)%_sampleSize],
                    DateTime.UtcNow.Ticks);
            }
        }
    }
}
