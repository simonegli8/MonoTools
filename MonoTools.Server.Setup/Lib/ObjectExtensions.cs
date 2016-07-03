using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Silversite {

	public static class ObjectExtensions {

		public static T As<T>(this object obj) { return (T)(obj == null ? default(T) : (T)obj); }
		public static IEnumerable<T> Follow<T>(this T obj, Func<T, T> field) {
			if (obj == null) yield break;
			else {
				yield return obj;
				var next = field(obj);
				if (next != null) foreach (var n in next.Follow(field)) yield return n;
			}
		}

		public static IEnumerable<T> FollowReverse<T>(this T obj, Func<T, T> field) {
			if (obj == null) yield break;
			else {
				var next = field(obj);
				if (next != null) foreach (var n in next.FollowReverse(field)) yield return n;
				yield return obj;
			}
		}

	}

}