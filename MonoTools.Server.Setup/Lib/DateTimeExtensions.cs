using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Silversite {

	public static class DateTimeExtensions {

		public static int Week(this DateTime date) {
			return (date.DayOfYear - (int)date.DayOfWeek) / 7;
		}

		public static DateTime Last(this DateTime date, DayOfWeek weekday) {
			return date.AddDays((int)(weekday - date.DayOfWeek));
		}

		public static DateTime Next(this DateTime date, DayOfWeek weekday) {
			return date.AddDays((int)(date.DayOfWeek - weekday));
		}

		public static bool OlderThan(this DateTime date, TimeSpan time) { return DateTime.Now - time > date; }
	}
}