using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace StateManager
{
	internal enum FunctionDataType
	{
		Fixed,
		Flexible,
	}
	internal class FunctionDatas<T>// : IEnumerable<T>
	{
		public T[] Fixed;
		// private HashSet<T> Flexible;
		public T[] Flexible;

		public void AddRange(FunctionDataType type, IEnumerable<T> list)
		{
			if (list == null) { return; }
			if (type == FunctionDataType.Fixed) {
				Fixed = (Fixed?.Concat(list) ?? list).ToArray();
			}
			else {
				Flexible = (Flexible?.Concat(list) ?? list).ToArray();
				// if (Flexible == null) {
				// 	Flexible = new HashSet<T>(list);
				// }
				// else {
				// 	foreach (var o in list) {
				// 		Flexible.Add(o);
				// 	}
				// }
			}
		}
		public void RemoveRange(FunctionDataType type, IEnumerable<T> list)
		{
			if (type != FunctionDataType.Flexible) {
				throw new ArgumentException(nameof(type));
			}
			Flexible = Flexible.Where(ft => !list.Any(lt => EqualityComparer<T>.Default.Equals(ft, lt))).ToArray();
			// lock (RemoveList) {
			// 	RemoveList.AddRange(list);
			// }
			// int ret = 0;
			// if (Flexible == null) { return ret; }
			// foreach (var o in list) {
			// 	if (Flexible.Remove(o)) {
			// 		++ret;
			// 	}
			// }
			// return ret;
		}
		public T[] this[FunctionDataType type]
		{
			get => type == FunctionDataType.Fixed ? Fixed : Flexible;
		}
		// public IEnumerable<T> this[FunctionDataType type]
		// {
		// 	get => type == FunctionDataType.Fixed ? Fixed as IEnumerable<T> : Flexible as IEnumerable<T>;
		// }
		public bool Empty => ((Fixed?.Length ?? 0) + (Flexible?.Length ?? 0)) == 0;

		// public IEnumerator<T> GetEnumerator()
		// {
		// 	var fi = Fixed;
		// 	if (fi != null) {
		// 		foreach (var obj in fi) {
		// 			yield return obj;
		// 		}
		// 	}
		// 	var fl = Flexible;
		// 	if (fl != null) {
		// 		foreach (var obj in fl) {
		// 			yield return obj;
		// 		}
		// 	}
		// }
		public void Execute<TArg>(TArg arg, Action<T, TArg> action)
		{
			var fi = Fixed;
			if (fi != null) {
				foreach (var obj in fi) {
					action(obj, arg);
				}
			}
			var fl = Flexible;
			if (fl != null) {
				foreach (var obj in fl) {
					action(obj, arg);
				}
			}
		}

		// IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
