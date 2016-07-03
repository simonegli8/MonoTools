using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Silversite {

	public static class EnumExtensions {
		public static T Parse<T>(this Enum e, string s) { return (T)Enum.Parse(typeof(T), s, true); }
	}

}