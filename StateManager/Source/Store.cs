using System;
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
		private TState state;
		private event StateOnUpdate<TState> OnUpdate;
		// private readonly object eventLock = new object();
		private readonly object stateUpdateLock = new object();

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

		internal TState State => state;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		protected Store()
		{
			state = InitialState();
		}

		Type IStore.StateType => typeof(TState);

		internal IDisposable AddBindState(StateOnUpdate<TState> onUpdate, SynchronizationContext context, bool initialCall)
		{
			StateOnUpdate<TState> func;
			if (context != null) {
				func = state => context.Post(ss => onUpdate((TState)ss), state);
			}
			else {
				func = onUpdate;
			}
			OnUpdate += func;
			if (initialCall) {
				func.Invoke(state);
			}
			return new DisposableObject<(Store<TState> store, StateOnUpdate<TState> callback)>(arg =>
			{
				arg.store.OnUpdate -= arg.callback;
			}, (this, func));
		}
		IDisposable IStore.Subscribe(StateOnUpdate onUpdate, SynchronizationContext context, bool initialCall) => AddBindState((TState state) => onUpdate(state), context, initialCall);

		internal void Reduce(IReducer<TState> reducer, IAction action)
		{
			TState newState;
			bool update = false;
			lock (stateUpdateLock) {
				var rd = reducer as IReducer<TState>;
				if (rd != null) {
					newState = rd.Reduce(state, action);
				}
				else {
					newState = (TState)reducer.Reduce(state, action);
				}
				newState = Validate(newState);
				if (!IsEquivalent(state, newState)) {
					state = newState;
					update = true;
				}
			}

			if (update) {
				OnUpdate?.Invoke(newState);
			}
		}
		void IStore.Reduce(IReducer reducer, IAction action) => Reduce(reducer as IReducer<TState>, action);

		internal IState<TState> CreateStateReference() => new State<TState>(this);
		IState IStore.CreateStateReference() => CreateStateReference();

	}
}
