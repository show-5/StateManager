using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace StateManager
{
	/// <summary>
	/// ストア初期化用
	/// </summary>
	public class DispatcherInitializer
	{
		internal HashSet<Type> Stores { get; private set; } = new HashSet<Type>();
		internal HashSet<Type> FunctionObjects { get; private set; } = new HashSet<Type>();

		/// <summary>
		/// アセンブリから自動でスキャン
		/// </summary>
		/// <param name="assembly">アセンブリ</param>
		/// <returns>自身</returns>
		public DispatcherInitializer ScanAssembly(Assembly assembly)
		{
			var types = assembly.GetTypes()
				.Select(type =>
				{
					if (!type.IsAbstract) {
						if (typeof(IStore).IsAssignableFrom(type)) {
							return (typeof(IStore), type);
						}
						if (typeof(FunctionObject).IsAssignableFrom(type)) {
							return (typeof(FunctionObject), type);
						}
					}
					return (null, null);
				})
				.Where(d => d.Item1 != null)
				.ToLookup(d => d.Item1, d => d.type);
			Stores = new HashSet<Type>(types[typeof(IStore)].Concat(Stores));
			FunctionObjects = new HashSet<Type>(types[typeof(FunctionObject)].Concat(FunctionObjects));
			return this;
		}

		/// <summary>
		/// 構造定義追加
		/// </summary>
		/// <typeparam name="TStore">構造定義</typeparam>
		/// <returns>自身</returns>
		public DispatcherInitializer AddStore<TStore>() where TStore : IStore
		{
			Stores.Add(typeof(TStore));
			return this;
		}

		/// <summary>
		/// 実行関数追加
		/// </summary>
		/// <typeparam name="TFunctionObject">実行関数オブジェクト</typeparam>
		/// <returns>自身</returns>
		public DispatcherInitializer AddEffect<TFunctionObject>() where TFunctionObject : FunctionObject
		{
			FunctionObjects.Add(typeof(TFunctionObject));
			return this;
		}
	}
}
