using System;
using System.Collections;
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
		// private event Action<TState, TState> OnUpdate;
		private readonly object stateUpdateLock = new object();
		// private ISubscribe<TState>[] subscribes;
		private FunctionDatas<ISubscribe<TState>> subscribes = new FunctionDatas<ISubscribe<TState>>();

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
		/// <param name="oldState">変更前ステート</param>
		/// <param name="newState">ステート</param> 
		/// <returns>変更後のステート</returns>
		public virtual TState Validate(TState oldState, TState newState) => newState;

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

		Type IStore.StateType => typeof(TState);

		internal IDisposable AddBindState(Dispatcher dispatcher, Action<TState, TState> onUpdate, SynchronizationContext context, bool initialCall)
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
			var disposable = dispatcher.RegisterFunctions(new SubscribeFunc<TState>(func));
			if (initialCall) {
				func.Invoke(state.Value, state.Value);
			}
			return disposable;
		}
		IDisposable IStore.Subscribe(Dispatcher dispatcher, Action<object, object> onUpdate, SynchronizationContext context, bool initialCall)
			=> AddBindState(dispatcher, (TState oldState, TState newState) => onUpdate(oldState, newState), context, initialCall);

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
				newState = Validate(oldState, newState);
				if (!IsEquivalent(oldState, newState)) {
					state.Value = newState;
					update = true;
				}
			}

			if (update) {
				// if (!subscribes.Empty) {
				// 	foreach (var subscribe in subscribes) {
				// 		subscribe.Subscribe(Name, oldState, newState);
				// 	}
				// }
				subscribes.Execute<(string Name, TState oldState, TState newState)>((Name, oldState, newState), (subscribe, args) =>
				{
					subscribe.Subscribe(args.Name, args.oldState, args.newState);
					// postAction.PostAction(act);
				});
				// OnUpdate?.Invoke(oldState, newState);
			}
		}
		void IStore.Reduce(IReducer reducer, IAction action) => Reduce(reducer as IReducer<TState>, action);

		internal IState<TState> StateReference() => state;
		IState IStore.StateReference() => StateReference();

		void IStore.AddSubscribes(FunctionDataType type, IEnumerable subscribes)
		{
			this.subscribes.AddRange(type, subscribes.OfType<ISubscribe<TState>>());
		}
		void IStore.RemoveSubscribes(FunctionDataType type, IEnumerable subscribes)
		{
			this.subscribes.RemoveRange(type, subscribes.OfType<ISubscribe<TState>>());
		}
	}

}
