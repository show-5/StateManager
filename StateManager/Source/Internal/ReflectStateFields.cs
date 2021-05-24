using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace StateManager
{
	internal class ReflectStateFields : IEnumerable<(FieldInfo fi, Type type, string name)>
	{
		private static Dictionary<Type, ReflectStateFields> TypeTable = new Dictionary<Type, StateManager.ReflectStateFields>();

		private ReflectStateFields baseFields;
		private (FieldInfo fi, Type type, string name)[] fieldInfos;

		public ReflectStateFields(ReflectStateFields baseFields, (FieldInfo fi, Type type, string name)[] fieldInfos)
		{
			this.baseFields = baseFields;
			this.fieldInfos = fieldInfos;
		}

		private static ReflectStateFields GetOrAdd(Type type)
		{
			ReflectStateFields ret;
			if (TypeTable.TryGetValue(type, out ret)) {
				return ret;
			}
			var infos = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
				.Select(fi => (fi, attribute: fi.GetCustomAttributes(typeof(ReflectStateAttribute), false).FirstOrDefault() as ReflectStateAttribute))
				.Where(fia => fia.attribute != null)
				.Select(fia => (fia.fi, fia.fi.FieldType.GetGenericArguments()[0], fia.attribute.Name));
			ReflectStateFields baseFields = null;
			var baseType = type.BaseType;
			if (baseType != typeof(object) && baseType != typeof(ValueType)) {
				baseFields = GetOrAdd(baseType);
			}
			ret = new ReflectStateFields(baseFields, infos.ToArray());
			TypeTable.Add(type, ret);
			return ret;
		}
		public static (FieldInfo fi, IState state)[] GetReflectInfos(Dispatcher dispatcher, Type type)
		{
			return GetOrAdd(type).Execute(d =>
			{
				IState state;
				if (!string.IsNullOrEmpty(d.name)) {
					state = dispatcher.GetState(d.name);
				}
				else {
					state = dispatcher.GetState(d.type);
				}
				return (d.fi, state);
			});
		}
		public static void Reflect(Dispatcher dispatcher, object o)
		{
			foreach (var d in GetReflectInfos(dispatcher, o.GetType())) {
				d.fi.SetValue(o, d.state);
			}
		}

		public IEnumerator<(FieldInfo fi, Type type, string name)> GetEnumerator()
		{
			if (baseFields != null) {
				foreach (var d in baseFields) {
					yield return d;
				}
			}
			foreach (var d in fieldInfos) {
				yield return d;
			}
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
