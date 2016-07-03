using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Silversite {

	public static class EnumerableExtensions {

		//TODO obsolete? (ToLookup)
		public static HashSet<T> ToHashSet<T>(this IEnumerable<T> set) { return new HashSet<T>(set); }

		public static int IndexOf<T>(this IEnumerable<T> set, T item) where T : class {
			if (set is IList) return ((IList)set).IndexOf(item);
			int i = 0;
			foreach (T x in set) {
				if (x == item) return i;
				i++;
			}
			return -1;
		}

		public static IEnumerable<T> Append<T>(this IEnumerable<T> set, params T[] items) {
			foreach (var item in set) yield return item;
			foreach (var item in items) yield return item;
		}
		public static IEnumerable<T> Prepend<T>(this IEnumerable<T> set, params T[] items) {
			foreach (var item in items) yield return item;
			foreach (var item in set) yield return item;
		}
		public static IEnumerable<T> Append<T>(this IEnumerable<T> set, IEnumerable<T> items) {
			foreach (var item in set) yield return item;
			foreach (var item in items) yield return item;
		}
		public static IEnumerable<T> Prepend<T>(this IEnumerable<T> set, IEnumerable<T> items) {
			foreach (var item in items) yield return item;
			foreach (var item in set) yield return item;
		}


		public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> set, int parts) {
			int size, p = 0;
			if (set.GetType().IsArray) {
				var a = (T[])set;
				size = (a.Length + parts - 1) / parts;
				while (p < a.Length) {
					yield return a.Skip(p).Take(size);
					p += size;
				}
			} else {
				if (!(set is ICollection<T>)) set = set.ToList();
				var col = (ICollection<T>)set;
				size = (col.Count + parts - 1) / parts;
				while (p < col.Count) {
					yield return col.Skip(p).Take(size);
					p += size;
				}
			}
		}

		public static void Remove<T>(this ICollection<T> set, IEnumerable<T> elements) where T : class { foreach (var e in elements.ToArray()) set.Remove(e); }
		public static void Remove<T>(this ICollection<T> set, Func<T, bool> selector) where T : class { set.Remove(set.OfType<T>().Where(selector)); }
		//public static void Remove<T>(this IList<T> set, IEnumerable<T> elements) where T: class { foreach (var e in elements) set.Remove(e); }
		//public static void Remove<T>(this IList<T> set, Predicate<T> selector) where T: class { foreach (var e in set) if (selector(e)) set.Remove(e); }
		public static void RemoveAll<T>(this ICollection<T> set) where T : class { set.Clear(); }
		public static void AddTo<T, U>(this IEnumerable<T> set, ICollection<U> collection, Func<T, U> select) { foreach (T x in set) collection.Add(select(x)); }
		public static void AddRange<T>(this ICollection<T> set, IEnumerable<T> elements) { foreach (T e in elements) set.Add(e); }

		public static IEnumerable<T> Sequence<T>(this int n, Func<int, T> generator) { int i = 0; while (i < n) yield return generator(i++); }
		public static IEnumerable<T> Sequence<T>(this long n, Func<long, T> generator) { long i = 0; while (i < n) yield return generator(i++); }
		public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> list, int n) {
			int i = 0, j = 0;
			var buf = new T[n];
			foreach (T item in list) {
				buf[j] = item;
				j = (j + 1) % n;
				if (i == j) {
					yield return buf[i];
					i = (i + 1) % n;
				}
			}
		}
	}
}