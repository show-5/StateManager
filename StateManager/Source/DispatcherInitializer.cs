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
		internal HashSet<Type> Effects { get; private set; } = new HashSet<Type>();
		// internal HashSet<Type> EffectAsyncs { get; private set; } = new HashSet<Type>();

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
						if (typeof(IEffect).IsAssignableFrom(type)) {
							return (typeof(IEffect), type);
						}
						// if (typeof(IEffectAsync).IsAssignableFrom(type)) {
						// 	return (typeof(IEffectAsync), type);
						// }
					}
					return (null, null);
				})
				.Where(d => d.Item1 != null)
				.ToLookup(d => d.Item1, d => d.type);
			// Stores = types[typeof(IStore)].Concat(Stores).ToHashSet();
			// Effects = types[typeof(IEffector)].Concat(Effects).ToHashSet();
			// EffectAsyncs = types[typeof(IAsyncEffector)].Concat(EffectAsyncs).ToHashSet();
			Stores = new HashSet<Type>(types[typeof(IStore)].Concat(Stores));
			Effects = new HashSet<Type>(types[typeof(IEffect)].Concat(Effects));
			// EffectAsyncs = new HashSet<Type>(types[typeof(IEffectAsync)].Concat(EffectAsyncs));
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
		/// <typeparam name="TEffect">実行関数オブジェクト</typeparam>
		/// <returns>自身</returns>
		public DispatcherInitializer AddEffect<TEffect>() where TEffect : IEffect
		{
			Effects.Add(typeof(TEffect));
			return this;
		}

		// /// <summary>
		// /// 実行関数追加
		// /// </summary>
		// /// <typeparam name="TEffectAsync">実行関数オブジェクト</typeparam>
		// /// <returns>自身</returns>
		// public DispatcherInitializer AddEffectAsync<TEffectAsync>() where TEffectAsync : IEffectAsync
		// {
		// 	EffectAsyncs.Add(typeof(IEffectAsync));
		// 	return this;
		// }
	}
}
