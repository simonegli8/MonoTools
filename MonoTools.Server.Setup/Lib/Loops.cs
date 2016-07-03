using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;


namespace Silversite {

	public static class Loops {

		public static void For<T>(this IList<T> set, Action<IList<T>, int> action) { for (var i = 0; i < set.Count; i++) action(set, i); }

		public static void ForEach<T>(this IEnumerable<T> set, Action<T> action) { foreach (var x in set) action(x); }
		public static void Each<T>(this IEnumerable<T> set, Action<T> action) { foreach (var x in set) action(x); }
		public static void AwaitEach<T>(this IEnumerable<T> set, Action<T> action) { Parallel.ForEach<T>(set, action); }
		public static void AwaitEach<T>(this IEnumerable<T> set, Action<T> action, int threads) { Parallel.ForEach<T>(set, new ParallelOptions() { MaxDegreeOfParallelism = threads }, action); }

		public static void AwaitAll<T>(this IEnumerable<T> set, Action<T> action) {
			var tasks = new List<Task>();

			foreach (var x in set) {
				tasks.Add(Services.Tasks.Do(() => action(x)));
			}
			foreach (var t in tasks) t.Wait();
		}

		public static IEnumerable<U> AwaitEach<T, U>(this IEnumerable<T> set, Func<T, U> f, int threads) {
			var results = new Services.PipeQueue<U>();
			var list = set.ToList();
			foreach (var loop in list.Split(threads)) {
				Services.Tasks.Do(() => {
					foreach (var x in loop) {
						results.Enqueue(f(x));
					}
				});
			}
			foreach (var x in results) yield return x;
		}
		public static IEnumerable<U> AwaitEach<T, U>(this IEnumerable<T> set, Func<T, U> f) { return AwaitEach(set, f, Environment.ProcessorCount); }
		public static IEnumerable<U> AwaitAll<T, U>(this IEnumerable<T> set, Func<T, U> f) {
			var results = new Services.PipeQueue<U>();
			var list = set.ToList();
			foreach (var x in list) Services.Tasks.Do(() => { results.Enqueue(f(x)); });
			foreach (var x in results) yield return x;
		}

		/*
		public static IEnumerable<T> WriteDebug<T>(this IEnumerable<T> set) {
			set.Each(x => Debug.Message(x.ToString()));
			return set;
		}*/
	}

}