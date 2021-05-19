﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace StateManager
{
	/// <summary>
	/// ステート管理
	/// </summary>
	/// <typeparam name="TState">ステート</typeparam>
	public abstract class Store<TState> : IStore
	{
		// private TState state;
		private State<TState> state_;
		private State<TState> state
		{
			get
			{
				if (state_ == null) {
					state_ = new State<TState>(InitialState());
				}
				return state_;
			}
		}
		private event Action<TState, TState> OnUpdate;
		// private readonly object eventLock = new object();
		private readonly object stateUpdateLock = new object();
		private ISubscribe<TState>[] subscribes;

		/// <summary>
		/// ステート名
		/// nullだと名前を登録しない
		/// </summary>
		public virtual string Name => null;

		/// <summary>
		/// 初期値
		/// </summary>
		/// <returns>ステート</returns>
		public virtual TState InitialState() => Activator.CreateInstance<TState>();

		/// <summary>
		/// 値のチェック
		/// </summary>
		/// <param name="state">ステート</param>
		/// <returns>変更後のステート</returns>
		public virtual TState Validate(TState state) => state;

		/// <summary>
		/// 値が等しいか
		/// trueが返った時、値を更新しない
		/// </summary>
		/// <param name="v1">値１</param>
		/// <param name="v2">値２</param>
		/// <returns>更新不要ならtrue</returns>
		public virtual bool IsEquivalent(TState v1, TState v2) => EqualityComparer<TState>.Default.Equals(v1, v2);

		/// <summary>
		/// 更新関数リスト
		/// </summary>
		public abstract IReducer<TState>[] Reducers { get; }
		IReducer[] IStore.Reducers => Reducers;

		// internal TState State => state;
		internal TState State => state.Value;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		// protected Store()
		// {
		// 	// state = InitialState();
		// }

		Type IStore.StateType => typeof(TState);

		internal IDisposable AddBindState(Action<TState, TState> onUpdate, SynchronizationContext context, bool initialCall)
		{
			Action<TState, TState> func;
			if (context != null) {
				func = (oldState, newState) =>
				{
					if (SynchronizationContext.Current != context) {
						context.Post(ss => onUpdate(oldState, newState), null);
					}
					else {
						onUpdate(oldState, newState);
					}
				};
			}
			else {
				func = onUpdate;
			}
			OnUpdate += func;
			if (initialCall) {
				func.Invoke(state.Value, state.Value);
			}
			return new DisposableObject<(Store<TState> store, Action<TState, TState> callback)>(arg =>
			{
				arg.store.OnUpdate -= arg.callback;
			}, (this, func));
		}
		IDisposable IStore.Subscribe(Action<object, object> onUpdate, SynchronizationContext context, bool initialCall) => AddBindState((TState oldState, TState newState) => onUpdate(oldState, newState), context, initialCall);

		internal void Reduce(IReducer<TState> reducer, IAction action)
		{
			TState oldState = state.Value;
			TState newState;
			bool update = false;
			lock (stateUpdateLock) {
				var rd = reducer as IReducer<TState>;
				if (rd != null) {
					newState = rd.Reduce(oldState, action);
				}
				else {
					newState = (TState)reducer.Reduce(oldState, action);
				}
				newState = Validate(newState);
				if (!IsEquivalent(oldState, newState)) {
					state.Value = newState;
					update = true;
				}
			}

			if (update) {
				if (subscribes != null) {
					foreach (var subscribe in subscribes) {
						subscribe.Subscribe(Name, oldState, newState);
					}
				}
				OnUpdate?.Invoke(oldState, newState);
			}
		}
		void IStore.Reduce(IReducer reducer, IAction action) => Reduce(reducer as IReducer<TState>, action);

		// internal IState<TState> StateReference() => new State<TState>(this);
		internal IState<TState> StateReference() => state;
		IState IStore.StateReference() => StateReference();

		void IStore.SetSubscribes(IEnumerable<object> subscribes)
		{
			this.subscribes = subscribes.OfType<ISubscribe<TState>>().ToArray();
		}
	}

}
