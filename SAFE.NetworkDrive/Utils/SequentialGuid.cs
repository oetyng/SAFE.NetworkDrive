using System;
using System.Collections.Generic;
using System.Linq;

namespace SAFE.NetworkDrive.Gateways.Utils
{
    /// <summary>
    /// http://www.siepman.nl/blog/post/2013/10/28/ID-Sequential-Guid-COMB-Vs-Int-Identity-using-Entity-Framework.aspx
    /// </summary>
    public class SequentialGuid
    {
        readonly object _synchronizationObject = new object();

        const int NumberOfBytes = 6;
        const int PermutationsOfAByte = 256;
        readonly long _maximumPermutations = (long)Math.Pow(PermutationsOfAByte, NumberOfBytes);
        long _lastSequence;

        static readonly Lazy<SequentialGuid> InstanceField = new Lazy<SequentialGuid>(() => new SequentialGuid());

        public SequentialGuid(DateTime sequenceStartDate, DateTime sequenceEndDate)
        {
            SequenceStartDate = sequenceStartDate;
            SequenceEndDate = sequenceEndDate;
        }

        public SequentialGuid()
            : this(new DateTime(2011, 10, 15), new DateTime(2100, 1, 1))
        { }
        
        internal static SequentialGuid Instance => InstanceField.Value;

        public DateTime SequenceStartDate { get; }
        public DateTime SequenceEndDate { get; }
        public TimeSpan TotalPeriod => SequenceEndDate - SequenceStartDate;

        public TimeSpan TimePerSequence
        {
            get
            {
                var ticksPerSequence = TotalPeriod.Ticks / _maximumPermutations;
                var result = new TimeSpan(ticksPerSequence);
                return result;
            }
        }

        public Guid GetGuid() => GetGuid(DateTime.UtcNow);
        public static Guid NewGuid() => Instance.GetGuid();

        internal Guid GetGuid(DateTime now)
        {
            if (now < SequenceStartDate || now > SequenceEndDate)
                return Guid.NewGuid(); // Outside the range, use regular Guid

            var sequence = GetCurrentSequence(now);
            return GetGuid(sequence);
        }

        internal Guid GetGuid(long sequence)
        {
            lock (_synchronizationObject)
            {
                if (sequence <= _lastSequence) // Prevent double sequence on same server
                    sequence = _lastSequence + 1;
                _lastSequence = sequence;
            }

            var sequenceBytes = GetSequenceBytes(sequence);
            var guidBytes = GetGuidBytes();
            var totalBytes = guidBytes.Concat(sequenceBytes).ToArray();
            var result = new Guid(totalBytes);
            return result;
        }

        long GetCurrentSequence(DateTime value)
        {
            var ticksUntilNow = value.Ticks - SequenceStartDate.Ticks;
            var result = (decimal)ticksUntilNow / TotalPeriod.Ticks * _maximumPermutations - 1;
            return (long)result;
        }

        IEnumerable<byte> GetSequenceBytes(long sequence)
        {
            var sequenceBytes = BitConverter.GetBytes(sequence);
            var sequenceBytesLongEnough = sequenceBytes.Concat(new byte[NumberOfBytes]);
            var result = sequenceBytesLongEnough.Take(NumberOfBytes).Reverse();
            return result;
        }

        IEnumerable<byte> GetGuidBytes()
        {
            var result = Guid.NewGuid().ToByteArray().Take(10).ToArray();
            return result;
        }
    }
}