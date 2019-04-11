﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Joint.Core.Utilities
{
    public delegate void CrontabFieldAccumulator(int start, int end, int interval);

    [Serializable]
    public enum CrontabFieldKind
    {
        Minute,
        Hour,
        Day,
        Month,
        DayOfWeek
    }

    /// <summary>
    /// Represents a single crontab field.
    /// </summary>
    [Serializable]
    public sealed class CrontabField
    {
        private readonly BitArray _bits;
        private readonly CrontabFieldImpl _impl;
        private /* readonly */ int _maxValueSet;
        private /* readonly */ int _minValueSet;

        private CrontabField(CrontabFieldImpl impl, string expression)
        {
            _impl = impl ?? throw new ArgumentNullException(nameof(impl));
            _bits = new BitArray(impl.ValueCount);

            _bits.SetAll(false);
            _minValueSet = int.MaxValue;
            _maxValueSet = -1;

            _impl.Parse(expression, Accumulate);
        }

        #region ICrontabField Members

        /// <summary>
        /// Gets the first value of the field or -1.
        /// </summary>
        public int GetFirst()
        {
            return _minValueSet < int.MaxValue ? _minValueSet : -1;
        }

        /// <summary>
        /// Gets the next value of the field that occurs after the given 
        /// start value or -1 if there is no next value available.
        /// </summary>
        public int Next(int start)
        {
            if (start < _minValueSet)
                return _minValueSet;

            var startIndex = ValueToIndex(start);
            var lastIndex = ValueToIndex(_maxValueSet);

            for (var i = startIndex; i <= lastIndex; i++)
            {
                if (_bits[i])
                    return IndexToValue(i);
            }

            return -1;
        }

        /// <summary>
        /// Determines if the given value occurs in the field.
        /// </summary>
        public bool Contains(int value)
        {
            return _bits[ValueToIndex(value)];
        }

        #endregion

        /// <summary>
        /// Parses a crontab field expression given its kind.
        /// </summary>
        public static CrontabField Parse(CrontabFieldKind kind, string expression)
        {
            return new CrontabField(CrontabFieldImpl.FromKind(kind), expression);
        }

        /// <summary>
        /// Parses a crontab field expression representing minutes.
        /// </summary>
        public static CrontabField Minutes(string expression)
        {
            return new CrontabField(CrontabFieldImpl.Minute, expression);
        }

        /// <summary>
        /// Parses a crontab field expression representing hours.
        /// </summary>
        public static CrontabField Hours(string expression)
        {
            return new CrontabField(CrontabFieldImpl.Hour, expression);
        }

        /// <summary>
        /// Parses a crontab field expression representing days in any given month.
        /// </summary>
        public static CrontabField Days(string expression)
        {
            return new CrontabField(CrontabFieldImpl.Day, expression);
        }

        /// <summary>
        /// Parses a crontab field expression representing months.
        /// </summary>
        public static CrontabField Months(string expression)
        {
            return new CrontabField(CrontabFieldImpl.Month, expression);
        }

        /// <summary>
        /// Parses a crontab field expression representing days of a week.
        /// </summary>
        public static CrontabField DaysOfWeek(string expression)
        {
            return new CrontabField(CrontabFieldImpl.DayOfWeek, expression);
        }

        private int IndexToValue(int index)
        {
            return index + _impl.MinValue;
        }

        private int ValueToIndex(int value)
        {
            return value - _impl.MinValue;
        }

        /// <summary>
        /// Accumulates the given range (start to end) and interval of values
        /// into the current set of the field.
        /// </summary>
        /// <remarks>
        /// To set the entire range of values representable by the field,
        /// set <param name="start" /> and <param name="end" /> to -1 and
        /// <param name="interval" /> to 1.
        /// </remarks>
        private void Accumulate(int start, int end, int interval)
        {
            var minValue = _impl.MinValue;
            var maxValue = _impl.MaxValue;

            if (start == end)
            {
                if (start < 0)
                {
                    //
                    // We're setting the entire range of values.
                    //

                    if (interval <= 1)
                    {
                        _minValueSet = minValue;
                        _maxValueSet = maxValue;
                        _bits.SetAll(true);
                        return;
                    }

                    start = minValue;
                    end = maxValue;
                }
                else
                {
                    //
                    // We're only setting a single value - check that it is in range.
                    //

                    if (start < minValue)
                    {
                        throw new FormatException(string.Format(
                            "'{0} is lower than the minimum allowable value for this field. Value must be between {1} and {2} (all inclusive).",
                            start, _impl.MinValue, _impl.MaxValue));
                    }

                    if (start > maxValue)
                    {
                        throw new FormatException(string.Format(
                            "'{0} is higher than the maximum allowable value for this field. Value must be between {1} and {2} (all inclusive).",
                            end, _impl.MinValue, _impl.MaxValue));
                    }
                }
            }
            else
            {
                //
                // For ranges, if the start is bigger than the end value then
                // swap them over.
                //

                if (start > end)
                {
                    end ^= start;
                    start ^= end;
                    end ^= start;
                }

                if (start < 0)
                {
                    start = minValue;
                }
                else if (start < minValue)
                {
                    throw new FormatException(string.Format(
                        "'{0} is lower than the minimum allowable value for this field. Value must be between {1} and {2} (all inclusive).",
                        start, _impl.MinValue, _impl.MaxValue));
                }

                if (end < 0)
                {
                    end = maxValue;
                }
                else if (end > maxValue)
                {
                    throw new FormatException(string.Format(
                        "'{0} is higher than the maximum allowable value for this field. Value must be between {1} and {2} (all inclusive).",
                        end, _impl.MinValue, _impl.MaxValue));
                }
            }

            if (interval < 1)
                interval = 1;

            int i;

            //
            // Populate the _bits table by setting all the bits corresponding to
            // the valid field values.
            //

            for (i = start - minValue; i <= (end - minValue); i += interval)
                _bits[i] = true;

            //
            // Make sure we remember the minimum value set so far Keep track of
            // the highest and lowest values that have been added to this field
            // so far.
            //

            if (_minValueSet > start)
                _minValueSet = start;

            i += (minValue - interval);

            if (_maxValueSet < i)
                _maxValueSet = i;
        }

        public override string ToString()
        {
            return ToString(null);
        }

        public string ToString(string format)
        {
            var writer = new StringWriter(CultureInfo.InvariantCulture);

            switch (format)
            {
                case "G":
                case null:
                    Format(writer, true);
                    break;
                case "N":
                    Format(writer);
                    break;
                default:
                    throw new FormatException();
            }

            return writer.ToString();
        }

        public void Format(TextWriter writer)
        {
            Format(writer, false);
        }

        public void Format(TextWriter writer, bool noNames)
        {
            _impl.Format(this, writer, noNames);
        }
    }

    /// <summary>
    /// Represents a schedule initialized from the crontab expression.
    /// </summary>
    [Serializable]
    public sealed class CrontabSchedule
    {
        private static readonly char[] Separators = { ' ' };
        private readonly CrontabField _days;
        private readonly CrontabField _daysOfWeek;
        private readonly CrontabField _hours;
        private readonly CrontabField _minutes;
        private readonly CrontabField _months;

        private CrontabSchedule(string expression)
        {
            Debug.Assert(expression != null);

            var fields = expression.Split((char[])Separators, StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length != 5)
            {
                throw new FormatException(string.Format(
                    "'{0}' is not a valid crontab expression. It must contain at least 5 components of a schedule "
                    + "(in the sequence of minutes, hours, days, months, days of week).",
                    expression));
            }

            _minutes = CrontabField.Minutes(fields[0]);
            _hours = CrontabField.Hours(fields[1]);
            _days = CrontabField.Days(fields[2]);
            _months = CrontabField.Months(fields[3]);
            _daysOfWeek = CrontabField.DaysOfWeek(fields[4]);
        }

        private static Calendar Calendar
        {
            get { return CultureInfo.InvariantCulture.Calendar; }
        }

        public static CrontabSchedule Parse(string expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            return new CrontabSchedule(expression);
        }

        public IEnumerable<DateTime> GetNextOccurrences(DateTime baseTime, DateTime endTime)
        {
            for (var occurrence = GetNextOccurrence(baseTime, endTime);
                occurrence < endTime;
                occurrence = GetNextOccurrence(occurrence, endTime))
            {
                yield return occurrence;
            }
        }

        public DateTime GetNextOccurrence(DateTime baseTime)
        {
            return GetNextOccurrence(baseTime, DateTime.MaxValue);
        }

        public DateTime GetNextOccurrence(DateTime baseTime, DateTime endTime)
        {
            const int nil = -1;

            var baseYear = baseTime.Year;
            var baseMonth = baseTime.Month;
            var baseDay = baseTime.Day;
            var baseHour = baseTime.Hour;
            var baseMinute = baseTime.Minute;

            var endYear = endTime.Year;
            var endMonth = endTime.Month;
            var endDay = endTime.Day;

            var year = baseYear;
            var month = baseMonth;
            var day = baseDay;
            var hour = baseHour;
            var minute = baseMinute + 1;

            //
            // Minute
            //

            minute = _minutes.Next(minute);

            if (minute == nil)
            {
                minute = _minutes.GetFirst();
                hour++;
            }

            //
            // Hour
            //

            hour = _hours.Next(hour);

            if (hour == nil)
            {
                minute = _minutes.GetFirst();
                hour = _hours.GetFirst();
                day++;
            }
            else if (hour > baseHour)
            {
                minute = _minutes.GetFirst();
            }

            //
            // Day
            //

            day = _days.Next(day);

            RetryDayMonth:

            if (day == nil)
            {
                minute = _minutes.GetFirst();
                hour = _hours.GetFirst();
                day = _days.GetFirst();
                month++;
            }
            else if (day > baseDay)
            {
                minute = _minutes.GetFirst();
                hour = _hours.GetFirst();
            }

            //
            // Month
            //

            month = _months.Next(month);

            if (month == nil)
            {
                minute = _minutes.GetFirst();
                hour = _hours.GetFirst();
                day = _days.GetFirst();
                month = _months.GetFirst();
                year++;
            }
            else if (month > baseMonth)
            {
                minute = _minutes.GetFirst();
                hour = _hours.GetFirst();
                day = _days.GetFirst();
            }

            //
            // The day field in a cron expression spans the entire range of days
            // in a month, which is from 1 to 31. However, the number of days in
            // a month tend to be variable depending on the month (and the year
            // in case of February). So a check is needed here to see if the
            // date is a border case. If the day happens to be beyond 28
            // (meaning that we're dealing with the suspicious range of 29-31)
            // and the date part has changed then we need to determine whether
            // the day still makes sense for the given year and month. If the
            // day is beyond the last possible value, then the day/month part
            // for the schedule is re-evaluated. So an expression like "0 0
            // 15,31 * *" will yield the following sequence starting on midnight
            // of Jan 1, 2000:
            //
            //  Jan 15, Jan 31, Feb 15, Mar 15, Apr 15, Apr 31, ...
            //

            var dateChanged = day != baseDay || month != baseMonth || year != baseYear;

            if (day > 28 && dateChanged && day > Calendar.GetDaysInMonth(year, month))
            {
                if (year >= endYear && month >= endMonth && day >= endDay)
                    return endTime;

                day = nil;
                goto RetryDayMonth;
            }

            var nextTime = new DateTime(year, month, day, hour, minute, 0, 0, baseTime.Kind);

            if (nextTime >= endTime)
                return endTime;

            //
            // Day of week
            //

            if (_daysOfWeek.Contains((int)nextTime.DayOfWeek))
                return nextTime;

            return GetNextOccurrence(new DateTime(year, month, day, 23, 59, 0, 0, baseTime.Kind), endTime);
        }

        public override string ToString()
        {
            var writer = new StringWriter(CultureInfo.InvariantCulture);

            _minutes.Format(writer, true);
            writer.Write(' ');
            _hours.Format(writer, true);
            writer.Write(' ');
            _days.Format(writer, true);
            writer.Write(' ');
            _months.Format(writer, true);
            writer.Write(' ');
            _daysOfWeek.Format(writer, true);

            return writer.ToString();
        }
    }

    [Serializable]
    public sealed class CrontabFieldImpl : IObjectReference
    {
        public static readonly CrontabFieldImpl Minute = new CrontabFieldImpl(CrontabFieldKind.Minute, 0, 59, null);
        public static readonly CrontabFieldImpl Hour = new CrontabFieldImpl(CrontabFieldKind.Hour, 0, 23, null);
        public static readonly CrontabFieldImpl Day = new CrontabFieldImpl(CrontabFieldKind.Day, 1, 31, null);

        public static readonly CrontabFieldImpl Month = new CrontabFieldImpl(CrontabFieldKind.Month, 1, 12,
            new[]
            {
                "January", "February", "March", "April",
                "May", "June", "July", "August",
                "September", "October", "November",
                "December"
            });

        public static readonly CrontabFieldImpl DayOfWeek = new CrontabFieldImpl(CrontabFieldKind.DayOfWeek, 0, 6,
            new[]
            {
                "Sunday", "Monday", "Tuesday",
                "Wednesday", "Thursday", "Friday",
                "Saturday"
            });

        private static readonly CrontabFieldImpl[] FieldByKind = { Minute, Hour, Day, Month, DayOfWeek };

        private static readonly CompareInfo Comparer = CultureInfo.InvariantCulture.CompareInfo;
        private static readonly char[] Comma = { ',' };

        private readonly CrontabFieldKind _kind;
        private readonly int _maxValue;
        private readonly int _minValue;
        private readonly string[] _names;

        private CrontabFieldImpl(CrontabFieldKind kind, int minValue, int maxValue, string[] names)
        {
            Debug.Assert(Enum.IsDefined(typeof(CrontabFieldKind), kind));
            Debug.Assert(minValue >= 0);
            Debug.Assert(maxValue >= minValue);
            Debug.Assert(names == null || names.Length == (maxValue - minValue + 1));

            _kind = kind;
            _minValue = minValue;
            _maxValue = maxValue;
            _names = names;
        }

        public CrontabFieldKind Kind
        {
            get { return _kind; }
        }

        public int MinValue
        {
            get { return _minValue; }
        }

        public int MaxValue
        {
            get { return _maxValue; }
        }

        public int ValueCount
        {
            get { return _maxValue - _minValue + 1; }
        }

        #region IObjectReference Members

        object IObjectReference.GetRealObject(StreamingContext context)
        {
            return FromKind(Kind);
        }

        #endregion

        public static CrontabFieldImpl FromKind(CrontabFieldKind kind)
        {
            if (!Enum.IsDefined(typeof(CrontabFieldKind), kind))
            {
                throw new ArgumentException(string.Format(
                    "Invalid crontab field kind. Valid values are {0}.",
                    string.Join(", ", Enum.GetNames(typeof(CrontabFieldKind)))), nameof(kind));
            }

            return FieldByKind[(int)kind];
        }

        public void Format(CrontabField field, TextWriter writer, bool noNames)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field));

            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            var next = field.GetFirst();
            var count = 0;

            while (next != -1)
            {
                var first = next;
                int last;

                do
                {
                    last = next;
                    next = field.Next(last + 1);
                } while (next - last == 1);

                if (count == 0
                    && first == _minValue && last == _maxValue)
                {
                    writer.Write('*');
                    return;
                }

                if (count > 0)
                    writer.Write(',');

                if (first == last)
                {
                    FormatValue(first, writer, noNames);
                }
                else
                {
                    FormatValue(first, writer, noNames);
                    writer.Write('-');
                    FormatValue(last, writer, noNames);
                }

                count++;
            }
        }

        private void FormatValue(int value, TextWriter writer, bool noNames)
        {
            Debug.Assert(writer != null);

            if (noNames || _names == null)
            {
                if (value >= 0 && value < 100)
                {
                    FastFormatNumericValue(value, writer);
                }
                else
                {
                    writer.Write(value.ToString(CultureInfo.InvariantCulture));
                }
            }
            else
            {
                var index = value - _minValue;
                writer.Write((string)_names[index]);
            }
        }

        private static void FastFormatNumericValue(int value, TextWriter writer)
        {
            Debug.Assert(value >= 0 && value < 100);
            Debug.Assert(writer != null);

            if (value >= 10)
            {
                writer.Write((char)('0' + (value / 10)));
                writer.Write((char)('0' + (value % 10)));
            }
            else
            {
                writer.Write((char)('0' + value));
            }
        }

        public void Parse(string str, CrontabFieldAccumulator acc)
        {
            if (acc == null)
                throw new ArgumentNullException(nameof(acc));

            if (string.IsNullOrEmpty(str))
                return;

            try
            {
                InternalParse(str, acc);
            }
            catch (FormatException e)
            {
                ThrowParseException(e, str);
            }
        }

        private static void ThrowParseException(Exception innerException, string str)
        {
            Debug.Assert(str != null);
            Debug.Assert(innerException != null);

            throw new FormatException(string.Format("'{0}' is not a valid crontab field expression.", str),
                innerException);
        }

        private void InternalParse(string str, CrontabFieldAccumulator acc)
        {
            Debug.Assert(str != null);
            Debug.Assert(acc != null);

            if (str.Length == 0)
                throw new FormatException("A crontab field value cannot be empty.");

            //
            // Next, look for a list of values (e.g. 1,2,3).
            //

            var commaIndex = str.IndexOf(",", StringComparison.Ordinal);

            if (commaIndex > 0)
            {
                foreach (var token in str.Split(Comma))
                    InternalParse(token, acc);
            }
            else
            {
                var every = 1;

                //
                // Look for stepping first (e.g. */2 = every 2nd).
                // 

                var slashIndex = str.IndexOf("/", StringComparison.Ordinal);

                if (slashIndex > 0)
                {
                    every = int.Parse(str.Substring(slashIndex + 1), CultureInfo.InvariantCulture);
                    str = str.Substring(0, slashIndex);
                }

                //
                // Next, look for wildcard (*).
                //

                if (str.Length == 1 && str[0] == '*')
                {
                    acc(-1, -1, every);
                    return;
                }

                //
                // Next, look for a range of values (e.g. 2-10).
                //

                var dashIndex = str.IndexOf("-", StringComparison.Ordinal);

                if (dashIndex > 0)
                {
                    var first = ParseValue(str.Substring(0, dashIndex));
                    var last = ParseValue(str.Substring(dashIndex + 1));

                    acc(first, last, every);
                    return;
                }

                //
                // Finally, handle the case where there is only one number.
                //

                var value = ParseValue(str);

                if (every == 1)
                {
                    acc(value, value, 1);
                }
                else
                {
                    Debug.Assert(every != 0);

                    acc(value, _maxValue, every);
                }
            }
        }

        private int ParseValue(string str)
        {
            Debug.Assert(str != null);

            if (str.Length == 0)
                throw new FormatException("A crontab field value cannot be empty.");

            var firstChar = str[0];

            if (firstChar >= '0' && firstChar <= '9')
                return int.Parse(str, CultureInfo.InvariantCulture);

            if (_names == null)
            {
                throw new FormatException(string.Format(
                    "'{0}' is not a valid value for this crontab field. It must be a numeric value between {1} and {2} (all inclusive).",
                    str, _minValue, _maxValue));
            }

            for (var i = 0; i < _names.Length; i++)
            {
                if (Comparer.IsPrefix(_names[i], str, CompareOptions.IgnoreCase))
                    return i + _minValue;
            }

            throw new FormatException(string.Format(
                "'{0}' is not a known value name. Use one of the following: {1}.",
                str, string.Join(", ", _names)));
        }
    }
}
