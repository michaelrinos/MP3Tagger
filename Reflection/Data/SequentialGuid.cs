using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;

namespace Reflection {

    [Serializable]
    public struct SequentialGuid : IComparable<SequentialGuid>, IComparable<Guid>, IComparable {
        public static readonly SequentialGuid Empty = (SequentialGuid)Guid.Empty;

        private const int _numberOfSequenceBytes = 6;
        private const int _permutationsOfAByte = 256;
        private static readonly long _maximumPermutations =
            (long)Math.Pow(_permutationsOfAByte, _numberOfSequenceBytes);
        private static long _lastSequence;


        private static readonly DateTime _sequencePeriodStart =
            new DateTime(2011, 11, 15, 0, 0, 0, DateTimeKind.Utc); // Start = 000000

        private static readonly DateTime _sequencePeriodeEnd =
            new DateTime(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc);   // End   = FFFFFF

        private readonly Guid _guidValue;

        public SequentialGuid(Guid guidValue) {
            _guidValue = guidValue;
        }

        public SequentialGuid(string guidValue)
            : this(new Guid(guidValue)) {
        }

        [SecuritySafeCritical]
        public static SequentialGuid NewSequentialGuid() {
            // You might want to inject DateTime.Now in production code
            return new SequentialGuid(GetGuidValue(DateTime.Now));
        }

        public static TimeSpan TimePerSequence {
            get {
                var ticksPerSequence = TotalPeriod.Ticks / _maximumPermutations;
                var result = new TimeSpan(ticksPerSequence);
                return result;
            }
        }

        public static TimeSpan TotalPeriod {
            get {
                var result = _sequencePeriodeEnd - _sequencePeriodStart;
                return result;
            }
        }

        #region FromDateTimeToGuid

        // Internal for testing
        internal static Guid GetGuidValue(DateTime now) {
            if (now < _sequencePeriodStart || now >= _sequencePeriodeEnd) {
                return Guid.NewGuid(); // Outside the range, use regular Guid
            }

            var sequence = GetCurrentSequence(now);
            return GetGuid(sequence);
        }

        private static long GetCurrentSequence(DateTime now) {
            var ticksUntilNow = now.Ticks - _sequencePeriodStart.Ticks;
            var factor = (decimal)ticksUntilNow / TotalPeriod.Ticks;
            var resultDecimal = factor * _maximumPermutations;
            var resultLong = (long)resultDecimal;
            return resultLong;
        }

        private static readonly object _synchronizationObject = new object();
        private static Guid GetGuid(long sequence) {
            lock (_synchronizationObject) {
                if (sequence <= _lastSequence) {
                    // Prevent double sequence on same server
                    sequence = _lastSequence + 1;
                }
                _lastSequence = sequence;
            }

            var sequenceBytes = GetSequenceBytes(sequence);
            var guidBytes = GetGuidBytes();
            var totalBytes = guidBytes.Concat(sequenceBytes).ToArray();
            var result = new Guid(totalBytes);
            return result;
        }

        private static IEnumerable<byte> GetSequenceBytes(long sequence) {
            var sequenceBytes = BitConverter.GetBytes(sequence);
            var sequenceBytesLongEnough = sequenceBytes.Concat(new byte[_numberOfSequenceBytes]);
            var result = sequenceBytesLongEnough.Take(_numberOfSequenceBytes).Reverse();
            return result;
        }

        private static IEnumerable<byte> GetGuidBytes() {
            return Guid.NewGuid().ToByteArray().Take(10);
        }

        #endregion

        #region FromGuidToDateTime

        public DateTime CreatedDateTime => GetCreatedDateTime(_guidValue);

        internal static DateTime GetCreatedDateTime(Guid value) {
            var sequenceBytes = GetSequenceLongBytes(value).ToArray();
            var sequenceLong = BitConverter.ToInt64(sequenceBytes, 0);
            var sequenceDecimal = (decimal)sequenceLong;
            var factor = sequenceDecimal / _maximumPermutations;
            var ticksUntilNow = factor * TotalPeriod.Ticks;
            var nowTicksDecimal = ticksUntilNow + _sequencePeriodStart.Ticks;
            var nowTicks = (long)nowTicksDecimal;
            var result = new DateTime(nowTicks);
            return result;
        }

        private static IEnumerable<byte> GetSequenceLongBytes(Guid value) {
            const int numberOfBytesOfLong = 8;
            var sequenceBytes = value.ToByteArray().Skip(10).Reverse().ToArray();
            var additionalBytesCount = numberOfBytesOfLong - sequenceBytes.Length;
            return sequenceBytes.Concat(new byte[additionalBytesCount]);
        }

        #endregion

        #region Relational Operators

        public static bool operator <(SequentialGuid value1, SequentialGuid value2) {
            return value1.CompareTo(value2) < 0;
        }

        public static bool operator >(SequentialGuid value1, SequentialGuid value2) {
            return value1.CompareTo(value2) > 0;
        }

        public static bool operator <(Guid value1, SequentialGuid value2) {
            return value1.CompareTo(value2) < 0;
        }

        public static bool operator >(Guid value1, SequentialGuid value2) {
            return value1.CompareTo(value2) > 0;
        }

        public static bool operator <(SequentialGuid value1, Guid value2) {
            return value1.CompareTo(value2) < 0;
        }

        public static bool operator >(SequentialGuid value1, Guid value2) {
            return value1.CompareTo(value2) > 0;
        }

        public static bool operator <=(SequentialGuid value1, SequentialGuid value2) {
            return value1.CompareTo(value2) <= 0;
        }

        public static bool operator >=(SequentialGuid value1, SequentialGuid value2) {
            return value1.CompareTo(value2) >= 0;
        }

        public static bool operator <=(Guid value1, SequentialGuid value2) {
            return value1.CompareTo(value2) <= 0;
        }

        public static bool operator >=(Guid value1, SequentialGuid value2) {
            return value1.CompareTo(value2) >= 0;
        }

        public static bool operator <=(SequentialGuid value1, Guid value2) {
            return value1.CompareTo(value2) <= 0;
        }

        public static bool operator >=(SequentialGuid value1, Guid value2) {
            return value1.CompareTo(value2) >= 0;
        }

        #endregion

        #region Equality Operators

        public static bool operator ==(SequentialGuid value1, SequentialGuid value2) {
            return value1.CompareTo(value2) == 0;
        }

        public static bool operator !=(SequentialGuid value1, SequentialGuid value2) {
            return !(value1 == value2);
        }

        public static bool operator ==(Guid value1, SequentialGuid value2) {
            return value1.CompareTo(value2) == 0;
        }

        public static bool operator !=(Guid value1, SequentialGuid value2) {
            return !(value1 == value2);
        }

        public static bool operator ==(SequentialGuid value1, Guid value2) {
            return value1.CompareTo(value2) == 0;
        }

        public static bool operator !=(SequentialGuid value1, Guid value2) {
            return !(value1 == value2);
        }

        #endregion

        #region CompareTo

        public int CompareTo(object obj) {
            if (obj is SequentialGuid) {
                return CompareTo((SequentialGuid)obj);
            }
            if (obj is Guid) {
                return CompareTo((Guid)obj);
            }
            throw new ArgumentException("Parameter is not of the right type");
        }

        public int CompareTo(SequentialGuid other) {
            return CompareTo(other._guidValue);
        }

        public int CompareTo(Guid other) {
            return CompareImplementation(_guidValue, other);
        }

        private static readonly int[] _indexOrderingHighLow =
        { 10, 11, 12, 13, 14, 15, 8, 9, 7, 6, 5, 4, 3, 2, 1, 0 };

        private static int CompareImplementation(Guid left, Guid right) {
            var leftBytes = left.ToByteArray();
            var rightBytes = right.ToByteArray();

            return _indexOrderingHighLow.Select(i => leftBytes[i].CompareTo(rightBytes[i]))
            .FirstOrDefault(r => r != 0);
        }

        #endregion

        #region Equals

        public override bool Equals(object obj) {
            if (obj is SequentialGuid || obj is Guid) {
                return CompareTo(obj) == 0;
            }

            return false;
        }

        public bool Equals(SequentialGuid other) {
            return CompareTo(other) == 0;
        }

        public bool Equals(Guid other) {
            return CompareTo(other) == 0;
        }

        public override int GetHashCode() {
            return _guidValue.GetHashCode();
        }

        #endregion

        #region Conversion operators

        public static implicit operator Guid(SequentialGuid value) {
            return value._guidValue;
        }

        public static explicit operator SequentialGuid(Guid value) {
            return new SequentialGuid(value);
        }

        public static explicit operator SequentialGuid(string value) {
            return new SequentialGuid(value);
        }

        #endregion

        #region ToString

        public override string ToString() {
            var roundedCreatedDateTime = Round(CreatedDateTime, TimeSpan.FromMilliseconds(1));
            return $"{_guidValue} ({roundedCreatedDateTime:yyyy-MM-dd HH:mm:ss.fff})";
        }

        /// <summary>
        /// Returns a string representation of the value of this <see cref="T:System.Guid"/> instance, according to the provided format specifier.
        /// </summary>
        /// 
        /// <returns>
        /// The value of this <see cref="T:System.Guid"/>, represented as a series of lowercase hexadecimal digits in the specified format.
        /// </returns>
        /// <param name="format">A single format specifier that indicates how to format the value of this <see cref="T:System.Guid"/>.
        /// The <paramref name="format"/> parameter can be "N", "D", "B", "P", or "X". If <paramref name="format"/> is null or an empty string (""), "D" is used.
        /// </param>
        /// <exception cref="T:System.FormatException">
        /// The value of <paramref name="format"/> is not null, an empty string (""), "N", "D", "B", "P", or "X". 
        /// </exception>
        /// <filterpriority>1</filterpriority>
        public string ToString(string format) {
            return _guidValue.ToString(format);
        }

        /// <summary>
        /// Returns a string representation of the value of this instance of the <see cref="T:System.Guid"/> class, according to the provided format specifier and culture-specific format information.
        /// </summary>
        /// 
        /// <returns>
        /// The value of this <see cref="T:System.Guid"/>, represented as a series of lowercase hexadecimal digits in the specified format.
        /// </returns>
        /// <param name="format">A single format specifier that indicates how to format the value of this <see cref="T:System.Guid"/>. 
        /// The <paramref name="format"/> parameter can be "N", "D", "B", "P", or "X". If <paramref name="format"/> is null or an empty string (""), "D" is used. 
        /// </param>
        /// <param name="provider">(Reserved) An object that supplies culture-specific formatting information. </param>
        /// <exception cref="T:System.FormatException">The value of <paramref name="format"/> is not null, an empty string (""), "N", "D", "B", "P", or "X". </exception>
        /// <filterpriority>1</filterpriority>
        [SecuritySafeCritical]
        public string ToString(string format, IFormatProvider provider) {
            return _guidValue.ToString(format, provider);
        }

        private static DateTime Round(DateTime dateTime, TimeSpan interval) {
            var halfIntervalTicks = (interval.Ticks + 1) >> 1;

            return dateTime.AddTicks(halfIntervalTicks -
            ((dateTime.Ticks + halfIntervalTicks) % interval.Ticks));
        }

        #endregion
    }
}
