using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace StateManager
{
	internal static class Utility
	{
		public static TOut[] Execute<TIn, TOut>(this IEnumerable<TIn> e, Func<TIn, TOut> func)
		{
			return e.Select(func).ToArray();
		}

		public static Type[] GetGenericArgTypes(this Type type, Type genericDefType, int argIndex)
		{
			if (genericDefType.IsInterface) {
				return type.GetInterfaces()
					.Where(it => it.IsGenericType)
					.Where(it => it.GetGenericTypeDefinition() == genericDefType)
					.Select(it => it.GetGenericArguments()[argIndex])
					.ToArray();
			}
			else {
				for (; type != typeof(object); type = type.BaseType) {
					if (!type.IsGenericType) { continue; }
					if (type.GetGenericTypeDefinition() != genericDefType) { continue; }
					return new Type[] { type.GetGenericArguments()[argIndex] };
				}
			}
			return null;
		}
		public static ILookup<Type, Type> GetGenericInterfaceArgTypes(this Type type, params (Type genericDefType, int argIndex)[] args)
		{
			return type.GetInterfaces()
				.Where(it => it.IsGenericType)
				.Select(it => (it: it, arg: args.FirstOrDefault(arg => arg.genericDefType == it.GetGenericTypeDefinition())))
				.Where(data => data.arg.genericDefType != null)
				.ToLookup(data => data.arg.genericDefType, data => data.it.GetGenericArguments()[data.arg.argIndex]);
		}
	}
}
