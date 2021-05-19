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
	}
}
