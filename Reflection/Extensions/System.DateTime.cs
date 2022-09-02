using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection {
	public static partial class SystemExtensions
	{
		public static string FormatMonthDay(this DateTime input)
		{
			return String.Format("{0:MMM d}", input);
		}

		public static string FormatMonthDay(this DateTime? input)
		{
			return String.Format("{0:MMM d}", input);
		}

		public static string FormatMonthDayLong(this DateTime input)
		{
			return String.Format("{0:MMMM d}", input);
		}

		public static string FormatMonthDayLong(this DateTime? input)
		{
			return String.Format("{0:MMMM d}", input);
		}

		public static string ToShortDateString(this DateTime? input)
		{
			if (input.HasValue == false)
				return "";

			return input.Value.ToShortDateString();
		}

		public static string TimeZoneName(this DateTime date, TimeZoneInfo tz)
		{
			switch (tz.Id)
			{
				// handle special cases here
				//case "America/Phoenix":
				//	break;

				case "SA Western Standard Time":
					return "Atlantic Standard Time"; //case 13839: puerto ricans don't believe in timezones
					
				default:
					if (tz.IsDaylightSavingTime(date))
						return tz.DaylightName;
					else
						return tz.StandardName;
			}
		}

		public static string TimeZoneAbbreviation(this DateTime date, TimeZoneInfo tz)
		{
			switch (tz.Id)
			{
				case "SA Western Standard Time":
					return "AST"; //case 13839: puerto ricans don't believe in timezones

				default:
					return date.TimeZoneName(tz).ToAcronym();
			}
		}

		/// <summary>
		/// Truncates the millisecond and fractional millisecond from a .NET DateTime
		/// </summary>
		/// <param name="dateTime">The DateTime to truncate</param>
		/// <returns></returns>
		/// <remarks>Useful for when you need to compare .NET dates with SQL</remarks>
		/// <ref>http://stackoverflow.com/a/1005222/444382</ref>
		public static DateTime TruncateMilliseconds(this DateTime dateTime)
		{
			var timeSpan = TimeSpan.FromMilliseconds(1);
			if (timeSpan == TimeSpan.Zero) return dateTime;
			var msTicks = dateTime.Millisecond * TimeSpan.TicksPerMillisecond;
			var fractionalMsTicks = dateTime.Ticks % timeSpan.Ticks;
			return dateTime.AddTicks(-(msTicks + fractionalMsTicks));
		}

		/// <summary>
		/// Generates a DateTime equivilent for the given epoch time
		/// </summary>
		/// <param name="unixTime">The epoch time to generate a DateTime for</param>
		/// <returns>The corresponding DateTime</returns>
		/// <remarks>Can be removed after move to .NET 4.6</remarks>
		public static DateTime FromUnixTime(this long unixTime)
		{
			var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			return epoch.AddSeconds(unixTime);
		}

		/// <summary>
		/// Generates an epoch time from the given DateTime
		/// </summary>
		/// <param name="date">The date to calculate the epoch time for</param>
		/// <returns>number of seconds since midnight, January 1, 1970</returns>
		/// <remarks>Can be removed after move to .NET 4.6</remarks>
		public static long ToUnixTime(this DateTime date)
		{
			var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			return Convert.ToInt64((date - epoch).TotalMilliseconds);
		}

		public static DateTime ToESTTime(this DateTime time)
		{	
			return TimeZoneInfo.ConvertTime(
				time, 
				TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time")
			);
		}

		public static DateTime ToPSTFromUtc(this DateTime d)
		{
			var tz = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
			return TimeZoneInfo.ConvertTimeFromUtc(d, tz);
		}
	}
}
// */