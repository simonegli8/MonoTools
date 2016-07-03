using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Reflection;

namespace Silversite {

	public static class TypeExtensions {

		const BindingFlags InvokeFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.InvokeMethod;

		public static T GetAttribute<T>(this Type type) where T : Attribute {
			return type.GetCustomAttributes(typeof(T), true).OfType<T>().FirstOrDefault<T>();
		}

		public static object Invoke(this System.Reflection.MethodInfo m, object o, params object[] p) { return m.Invoke(o, p); }
		public static object Invoke(this System.Reflection.MethodInfo m, object o) { return m.Invoke(o, null); }
		public static object Invoke(this Type t, object o, string name, params object[] p) {
			var method = t.GetMethods(InvokeFlags).FirstOrDefault(m => {
				if (m.Name != name) return false;
				var pars = m.GetParameters();
				if (pars.Length != p.Length) return false;
				for (int i = 0; i< pars.Length; i++) {
					if (p[i] == null && pars[i].ParameterType.IsValueType || p[i] != null && pars[i].ParameterType != p[i].GetType()) return false;
				}
				return true;
			});
			if (method == null) throw new TargetInvocationException("Method " + name + " not found.", null);
			return method.Invoke(o, p);
		}
		public static object Invoke(this Type t, object o, string name) { return t.GetMethod(name, InvokeFlags).Invoke(o, null); }
		public static object Invoke(this Type t, object o, string name, Type[] signature, params object[] p) { return t.GetMethod(name, InvokeFlags, null, signature, null).Invoke(o, p); }

		public static object GetValue(this System.Reflection.PropertyInfo p, object o) { return p.GetValue(o, null); }
		public static object GetValue(this Type t, object o, string name) { return t.GetProperty(name).GetValue(o, null); }
		public static void SetValue(this Type t, object o, string name, object x) { t.GetProperty(name).SetValue(o, x, null); }
		public static void SetValue(this System.Reflection.PropertyInfo p, object o, object x) { p.SetValue(o, x, null); }

		public static MethodInfo Method(this Type type, string name) {
			var method = type.GetMethods(InvokeFlags).FirstOrDefault(m => m.Name == name);
			if (method == null) throw new System.ArgumentException(string.Format("No method {0} on type {1} found.", name, type.FullName));
			return method;
		}

		public static MethodInfo Method(this Type type, string name, params Type[] signature) {
			MethodInfo method = null;
			try {
				method = type.GetMethod(name, InvokeFlags, null, signature, null);
			} catch { }
			if (method == null) throw new System.ArgumentException(string.Format("No method {0} on type {1} found.", name, type.FullName));
			return method;
		}
		public static MethodInfo Method<T>(this Type type, string name) { return Method(type, name, new[] { typeof(T) }); }
		public static MethodInfo Method<T, U>(this Type type, string name) { return Method(type, name, new[] { typeof(T), typeof(U) }); }
		public static MethodInfo Method<T, U, V>(this Type type, string name) { return Method(type, name, new[] { typeof(T), typeof(U), typeof(V) }); }
		public static MethodInfo Method<T, U, V, W>(this Type type, string name) { return Method(type, name, new[] { typeof(T), typeof(U), typeof(V), typeof(W) }); }
		public static MethodInfo Method<T, U, V, W, X>(this Type type, string name) { return Method(type, name, new[] { typeof(T), typeof(U), typeof(V), typeof(W), typeof(X) }); }
		public static MethodInfo Method<T, U, V, W, X, Y>(this Type type, string name) { return Method(type, name, new[] { typeof(T), typeof(U), typeof(V), typeof(W), typeof(X), typeof(Y) }); }
	}

}