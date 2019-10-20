using System;
using System.Globalization;
using System.Linq;
using Diagnostics.DataProviders;

namespace Diagnostics.RuntimeHost.Utilities
{
    internal class DateTimeHelper
    {
        internal static bool PrepareStartEndTimeWithTimeGrain(string startTime, string endTime, string timeGrain, out DateTime startTimeUtc, out DateTime endTimeUtc, out TimeSpan timeGrainTimeSpan, out string errorMessage)
        {
            Tuple<TimeSpan, TimeSpan, bool> selectedTimeGrainOption = null;
            const string timeGrainParameterName = "timeGrain";
            bool result = true;
            errorMessage = string.Empty;

            result = PrepareStartEndTimeUtc(startTime, endTime, out startTimeUtc, out endTimeUtc, out errorMessage);

            var defaultTimeGrainOption = DataProviderConstants.TimeGrainOptions.FirstOrDefault(t => t.Item3);
            timeGrainTimeSpan = defaultTimeGrainOption.Item1;

            if (!string.IsNullOrEmpty(timeGrain))
            {
                TimeSpan parsedTimeGrain;
                var success = ParseXmlDurationParameter(timeGrainParameterName, timeGrain, null, out parsedTimeGrain);

                if (!success)
                {
                    result = false;
                    errorMessage = string.Concat(errorMessage, "Invalid Time Grain.");
                }
                else
                {
                    selectedTimeGrainOption = DataProviderConstants.TimeGrainOptions.FirstOrDefault(t => t.Item1.Equals(parsedTimeGrain));
                    if (selectedTimeGrainOption == null)
                    {
                        result = false;
                        errorMessage = string.Concat(errorMessage, "Invalid Time Grain.");
                    }
                    else
                    {
                        timeGrainTimeSpan = selectedTimeGrainOption.Item1;
                    }
                }
            }

            startTimeUtc = GetDateTimeInUtcFormat(RoundDownTime(startTimeUtc, timeGrainTimeSpan));
            endTimeUtc = GetDateTimeInUtcFormat(RoundDownTime(endTimeUtc, timeGrainTimeSpan));

            if (startTimeUtc == endTimeUtc)
            {
                endTimeUtc = selectedTimeGrainOption != null ? startTimeUtc.Add(selectedTimeGrainOption.Item2) : startTimeUtc.Add(TimeSpan.FromDays(1));
            }

            //TODO: Here we could use Item2 in selectedTimeGrainOption to limit the length of time that you can query for each time grain
            return result;
        }

        internal static bool PrepareStartEndTimeUtc(string startTime, string endTime, out DateTime startTimeUtc, out DateTime endTimeUtc, out string errorMessage)
        {
            //1. no startTime, no endTime => return current time - 24 hours, current time
            //2. startTime, no endTime => return start time, end time = start time + 24 hours
            //3. no startTime, endTime => return start time = end time - 24 hours, end time
            //4. startTime, endTime => return start time, end time

            DateTime currentUtcTime = GetDateTimeInUtcFormat(DateTime.UtcNow);
            bool result = true;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(startTime) && string.IsNullOrWhiteSpace(endTime))
            {
                endTimeUtc = currentUtcTime;
                startTimeUtc = endTimeUtc.AddDays(-1);
            }
            else if (string.IsNullOrWhiteSpace(startTime))
            {
                result = ParseDateTimeParameter("endTime", endTime, currentUtcTime, out endTimeUtc);
                startTimeUtc = endTimeUtc.AddDays(-1);
            }
            else if (string.IsNullOrWhiteSpace(endTime))
            {
                result = ParseDateTimeParameter("startTime", startTime, currentUtcTime.AddDays(-1), out startTimeUtc);
                endTimeUtc = startTimeUtc.AddDays(1);
                if (endTimeUtc > currentUtcTime)
                {
                    endTimeUtc = currentUtcTime;
                }
            }
            else
            {
                result = ParseDateTimeParameter("endTime", endTime, currentUtcTime, out endTimeUtc);
                result &= ParseDateTimeParameter("startTime", startTime, currentUtcTime.AddDays(-1), out startTimeUtc);
            }

            if (result == false)
            {
                errorMessage = "Cannot parse invalid date time. Valid Time format is yyyy-mm-ddThh:mm";
                return false;
            }

            if (startTimeUtc > endTimeUtc)
            {
                errorMessage = "Invalid Start Time and End Time. End Time cannot be earlier than Start Time.";
                return false;
            }
            else if (startTimeUtc > currentUtcTime.AddMinutes(-15))
            {
                errorMessage = "Invalid Start Time. Start Time cannot be after 15 minutes before current time.";
                return false;
            }

            if (startTimeUtc < DateTime.UtcNow.Add(DataProviderConstants.KustoDataRetentionPeriod))
            {
                errorMessage = $"Invalid Start Time. Start Time cannot be earlier than {Math.Abs(DataProviderConstants.KustoDataRetentionPeriod.Days)} days.";
                return false;
            }

            if (endTimeUtc - startTimeUtc > TimeSpan.FromHours(72))
            {
                errorMessage = "Invalid Time Range. Time Range cannot be more than 72 hours.";
                return false;
            }

            return true;
        }

        internal static bool ParseDateTimeParameter(string parameterName, string parameterValue, DateTime defaultValue, out DateTime dateObj)
        {
            dateObj = defaultValue;
            if (!string.IsNullOrEmpty(parameterValue))
            {
                DateTime temp;
                bool result = DateTime.TryParse(parameterValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out temp);
                if (result)
                {
                    dateObj = GetDateTimeInUtcFormat(temp);
                    return true;
                }

                return false;
            }

            return true;
        }

        internal static DateTime GetDateTimeInUtcFormat(DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Unspecified)
            {
                return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Millisecond, DateTimeKind.Utc);
            }

            return dateTime.ToUniversalTime();
        }

        /// <summary>
        /// Round down Date time.
        /// </summary>
        /// <param name="dateTime">Date Time to round down.</param>
        /// <param name="roundDownBy">Round down value.</param>
        /// <returns>Rounded down Date Time.</returns>
        public static DateTime RoundDownTime(DateTime dateTime, TimeSpan roundDownBy)
        {
            return new DateTime((dateTime.Ticks / roundDownBy.Ticks) * roundDownBy.Ticks);
        }

        /// <summary>
        /// Round up Date time.
        /// </summary>
        /// <param name="dateTime">Date Time to round up.</param>
        /// <param name="roundUpBy">Round up value.</param>
        /// <returns>Rounded up Date Time.</returns>
        internal static DateTime RoundUpTime(DateTime dateTime, TimeSpan roundUpBy)
        {
            return new DateTime(((dateTime.Ticks + roundUpBy.Ticks) / roundUpBy.Ticks) * roundUpBy.Ticks);
        }

        internal static bool ParseXmlDurationParameter(string parameterName, string parameterValue, TimeSpan? defaultValue, out TimeSpan duration)
        {
            duration = new TimeSpan();
            TimeSpan? ret = defaultValue;
            bool parseFailed = string.IsNullOrEmpty(parameterValue);
            if (!parseFailed)
            {
                try
                {
                    ret = System.Xml.XmlConvert.ToTimeSpan(parameterValue);
                }
                catch (Exception)
                {
                    parseFailed = true;
                }
            }

            // throw exception if parsing failed and no default value is provided
            if (parseFailed && !defaultValue.HasValue)
            {
                return false;
            }

            duration = (TimeSpan)ret;
            return true;
        }

        internal static DateTime EpochTimeToDateTime(long unixTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(unixTime);
        }

        internal static long DateTimeToEpochTime(DateTime date)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToInt64((date - epoch).TotalSeconds);
        }
    }
}
