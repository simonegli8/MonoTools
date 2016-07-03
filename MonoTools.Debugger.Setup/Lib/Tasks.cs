using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading;
using System.Threading.Tasks;

namespace Silversite.Services {

	public class Tasks {

		public const int Second = 1000;
		public const int Minute = 60 * Second;
		public const int Hour = 60 * Minute;
		public const int Day = 24 * Hour;

		public static HashSet<Timer> timers = new HashSet<Timer>();
		public static readonly TimeSpan Infinite = new TimeSpan(-1);

		static void Run(Action a) {
			try {
				a();
			} catch (Exception ex) {
			}
		}

		public static void DoLater(int delay, Action a) {
			lock(timers) {
				Timer timer = null;
				timer = new Timer((state) => { Run(a); lock(timers) timers.Remove(timer); }, null, delay, Timeout.Infinite);
				timers.Add(timer);
			}
		}

		public static void DoLater(TimeSpan delay, Action a) {
			lock(timers) {
				Timer timer = null;
				timer = new Timer((state) => { Run(a); lock(timers) timers.Remove(timer); }, null, delay, Infinite);
				timers.Add(timer);
			}
		}
		public static void DoLater(Action a) { DoLater(0, a); }

		public static Timer Recurring(int interval, Action a) {
			var timer = new Timer((state) => { Run(a); }, null, interval, interval);
			timers.Add(timer);
			return timer;
		}

		public static Timer Recurring(int delay, int interval, Action a) {
			var timer = new Timer((state) => { Run(a); }, null, delay, interval);
			timers.Add(timer);
			return timer;
		}

		public static Timer Recurring(TimeSpan interval, Action a) {
			var timer = new Timer((state) => { Run(a); }, null, interval, interval);
			timers.Add(timer);
			return timer;
		}

		public static Timer Recurring(TimeSpan delay, TimeSpan interval, Action a) {
			var timer = new Timer((state) => { Run(a); }, null, delay, interval);
			timers.Add(timer);
			return timer;
		}

		public static void Cancel(Timer timer) {
			if (timers.Contains(timer)) timers.Remove(timer);
			timer.Dispose();
		}

		public static Task<T> Do<T>(Func<T> a) {
			return System.Threading.Tasks.Task<T>.Factory.StartNew(() => {
				try {
					return a();
				} catch (Exception ex) {
					return default(T);
				}
			});
		}

		public static Task Do(Action a) {
			return System.Threading.Tasks.Task.Factory.StartNew(() => {
				try {
					a();
				} catch (Exception ex) {
				}
			});
		}

		public static void Await(Action a) { Do(a).Wait(); }
		public static T Await<T>(Func<T> a) { var t =  Do<T>(a); t.Wait(); return t.Result; }

		public static void Await(IEnumerable<Task> set) { Task.WaitAll(set.ToArray()); }
		public static IEnumerable<T> Await<T>(IEnumerable<Task<T>> set) { foreach (var t in set) yield return t.Result; }
		public static void Await(params Task[] set) { Task.WaitAll(set); }
		public static IEnumerable<T> Await<T>(params Task<T>[] set) { foreach (var t in set) yield return t.Result; }

		public static void Sleep(int milliseconds) { Thread.Sleep(milliseconds); }
		public static void Sleep(TimeSpan timeout) { Thread.Sleep(timeout); }
	}
}