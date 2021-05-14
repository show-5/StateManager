using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StateManager
{
	/// <summary>
	/// アクションの実行、ステートの変更通知など
	/// </summary>
	public class Dispatcher
	{
		private Stores stores = new Stores();
		private Dictionary<Type, IActionReceiver> actionReceivers = new Dictionary<Type, IActionReceiver>();
		private static ConcurrentBag<List<Task>> waitListPool = new ConcurrentBag<List<Task>>();

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="initializer">Initialization information.</param>
		public Dispatcher(DispatcherInitializer initializer)
		{
			InitializeStores(initializer.Stores);
			InitializeEffects(initializer.Effects);
			// InitializeEffectAsyncs(initializer.EffectAsyncs);
		}

		/// <summary>
		/// ステートIDを取得
		/// </summary>
		/// <param name="name">ステート名</param>
		/// <returns>ステートID</returns>
		public int GetStateID(string name)
		{
			return stores.GetID(name);
		}

		/// <summary>
		/// ステートIDを取得<br/>
		/// 同じ型が複数登録されている場合は、例外が発生します。
		/// </summary>
		/// <typeparam name="TState">ステートの型</typeparam>
		/// <returns>ステートID</returns>
		public int GetStateID<TState>()
		{
			return stores.GetID(typeof(TState));
		}

		/// <summary>
		/// ステートIDを取得<br/>
		/// 同じ型が複数登録されている場合は、例外が発生します。
		/// </summary>
		/// <param name="type">ステートの型</param>
		/// <returns>ステートID</returns>
		public int GetStateID(Type type)
		{
			return stores.GetID(type);
		}

		/// <summary>
		/// ステートIDを取得
		/// </summary>
		/// <param name="name">ステート名</param>
		/// <param name="id">返されるステートID<br/>失敗時は0</param>
		/// <returns>取得できた場合はtrue</returns>
		public bool TryGetStateID(string name, out int id)
		{
			return stores.TryGetID(name, out id);
		}

		/// <summary>
		/// ステートを取得<br/>
		/// 同じ型が複数登録されている場合は、例外が発生します。
		/// </summary>
		/// <typeparam name="TState">ステートの型</typeparam>
		/// <param name="id">返されるステートID<br/>失敗時は0</param>
		/// <returns>取得できた場合はtrue</returns>
		public bool TryGetStateID<TState>(out int id)
		{
			return stores.TryGetID(typeof(TState), out id);
		}

		/// <summary>
		/// ステートを取得<br/>
		/// 同じ型が複数登録されている場合は、例外が発生します。
		/// </summary>
		/// <param name="type">ステートの型</param>
		/// <param name="id">返されるステートID<br/>失敗時は0</param>
		/// <returns>取得できた場合はtrue</returns>
		public bool TryGetStateID(Type type, out int id)
		{
			return stores.TryGetID(type, out id);
		}

		/// <summary>
		/// ステートを取得
		/// </summary>
		/// <param name="id">ステートID</param>
		/// <returns>ステート</returns>
		public IState GetState(int id)
		{
			return stores.Get(id).CreateStateReference();
		}

		/// <summary>
		/// ステートを取得
		/// </summary>
		/// <param name="name">ステート名</param>
		/// <returns>ステート</returns>
		public IState GetState(string name)
		{
			return stores.Get(name).CreateStateReference();
		}

		/// <summary>
		/// ステートを取得<br/>
		/// 同じ型が複数登録されている場合は、例外が発生します。
		/// </summary>
		/// <param name="type">ステートの型</param>
		/// <returns>ステート</returns>
		public IState GetState(Type type)
		{
			return stores.Get(type).CreateStateReference();
		}

		/// <summary>
		/// ステートを取得
		/// </summary>
		/// <param name="id">ステートID</param>
		/// <typeparam name="TState">ステートの型</typeparam>
		/// <returns>ステート</returns>
		public IState<TState> GetState<TState>(int id)
		{
			return stores.Get<TState>(id).CreateStateReference();
		}

		/// <summary>
		/// ステートを取得
		/// </summary>
		/// <param name="name">ステート名</param>
		/// <typeparam name="TState">ステートの型</typeparam>
		/// <returns>ステート</returns>
		public IState<TState> GetState<TState>(string name)
		{
			return stores.Get<TState>(name).CreateStateReference();
		}

		/// <summary>
		/// ステートを取得<br/>
		/// 同じ型が複数登録されている場合は、例外が発生します。
		/// </summary>
		/// <typeparam name="TState">ステートの型</typeparam>
		/// <returns>ステート</returns>
		public IState<TState> GetState<TState>()
		{
			return stores.Get<TState>(typeof(TState)).CreateStateReference();
		}

		/// <summary>
		/// ステート一覧を取得
		/// </summary>
		/// <returns>ステート一覧</returns>
		public IEnumerable<IState> GetStates()
		{
			return stores.GetStates();
		}

		/// <summary>
		/// 変更があったときに通知<br/>
		/// 終了時は返ってきたオブジェクトをDispose<br/>
		/// 返り値を受け取らないなど、参照がなくなった場合にGCで解放
		/// </summary>
		/// <param name="id">ステートID</param>
		/// <param name="onUpdate">通知関数</param>
		/// <param name="initialCall">最初に1回呼ぶ</param>
		/// <param name="context">コンテキスト（指定不要ならnull）</param>
		/// <returns>購読解除用Disposable</returns>
		public IDisposable Subscribe(int id, Action<object, object> onUpdate, SynchronizationContext context = null, bool initialCall = true)
		{
			return stores.Get(id).Subscribe(onUpdate, context, initialCall);
		}

		/// <summary>
		/// 変更があったときに通知<br/>
		/// 終了時は返ってきたオブジェクトをDispose<br/>
		/// 返り値を受け取らないなど、参照がなくなった場合にGCで解放
		/// </summary>
		/// <param name="name">ステート名</param>
		/// <param name="onUpdate">通知関数</param>
		/// <param name="initialCall">最初に1回呼ぶ</param>
		/// <param name="context">コンテキスト（指定不要ならnull）</param>
		/// <returns>購読解除用Disposable</returns>
		public IDisposable Subscribe(string name, Action<object, object> onUpdate, SynchronizationContext context = null, bool initialCall = true)
		{
			return stores.Get(name).Subscribe(onUpdate, context, initialCall);
		}

		/// <summary>
		/// 変更があったときに通知<br/>
		/// 終了時は返ってきたオブジェクトをDispose<br/>
		/// 同じ型が複数登録されている場合は、例外が発生します。<br/>
		/// 返り値を受け取らないなど、参照がなくなった場合にGCで解放
		/// </summary>
		/// <param name="type">ステートの型</param>
		/// <param name="onUpdate">通知関数</param>
		/// <param name="initialCall">最初に1回呼ぶ</param>
		/// <param name="context">コンテキスト（指定不要ならnull）</param>
		/// <returns>購読解除用Disposable</returns>
		public IDisposable Subscribe(Type type, Action<object, object> onUpdate, SynchronizationContext context = null, bool initialCall = true)
		{
			return stores.Get(type).Subscribe(onUpdate, context, initialCall);
		}

		/// <summary>
		/// 変更があったときに通知<br/>
		/// 終了時は返ってきたオブジェクトをDispose<br/>
		/// 返り値を受け取らないなど、参照がなくなった場合にGCで解放
		/// </summary>
		/// <param name="id">ステートID</param>
		/// <param name="onUpdate">通知関数</param>
		/// <param name="initialCall">最初に1回呼ぶ</param>
		/// <param name="context">コンテキスト（指定不要ならnull）</param>
		/// <typeparam name="TState">ステートの型</typeparam>
		/// <returns>購読解除用Disposable</returns>
		public IDisposable Subscribe<TState>(int id, Action<TState, TState> onUpdate, SynchronizationContext context = null, bool initialCall = true)
		{
			return stores.Get<TState>(id).AddBindState(onUpdate, context, initialCall);
		}

		/// <summary>
		/// 変更があったときに通知<br/>
		/// 終了時は返ってきたオブジェクトをDispose<br/>
		/// 返り値を受け取らないなど、参照がなくなった場合にGCで解放
		/// </summary>
		/// <param name="name">ステート名</param>
		/// <param name="onUpdate">通知関数</param>
		/// <param name="initialCall">最初に1回呼ぶ</param>
		/// <param name="context">コンテキスト（指定不要ならnull）</param>
		/// <typeparam name="TState">ステートの型</typeparam>
		/// <returns>購読解除用Disposable</returns>
		public IDisposable Subscribe<TState>(string name, Action<TState, TState> onUpdate, SynchronizationContext context = null, bool initialCall = true)
		{
			return stores.Get<TState>(name).AddBindState(onUpdate, context, initialCall);
		}

		/// <summary>
		/// 変更があったときに通知<br/>
		/// 終了時は返ってきたオブジェクトをDispose<br/>
		/// 同じ型が複数登録されている場合は、例外が発生します。<br/>
		/// 返り値を受け取らないなど、参照がなくなった場合にGCで解放
		/// </summary>
		/// <param name="onUpdate">通知関数</param>
		/// <param name="initialCall">最初に1回呼ぶ</param>
		/// <param name="context">コンテキスト（指定不要ならnull）</param>
		/// <typeparam name="TState">ステートの型</typeparam>
		/// <returns>購読解除用Disposable</returns>
		public IDisposable Subscribe<TState>(Action<TState, TState> onUpdate, SynchronizationContext context = null, bool initialCall = true)
		{
			return stores.Get<TState>(typeof(TState)).AddBindState(onUpdate, context, initialCall);
		}

		/// <summary>
		/// アクションを実行
		/// </summary>
		/// <typeparam name="TAction">アクションタイプ</typeparam>
		public void Dispatch<TAction>()
			where TAction : ActionBase
		{
			DispatchInternal(GetNewAction<TAction>());
		}

		/// <summary>
		/// アクションを実行
		/// </summary>
		/// <param name="payload">パラメータ</param>
		/// <typeparam name="TAction">アクションの型</typeparam>
		/// <typeparam name="TPayload">パラメータの型</typeparam>
		public void Dispatch<TAction, TPayload>(TPayload payload)
			where TAction : ActionBase<TPayload>
		{
			var action = GetNewAction<TAction>();
			action.Payload = payload;
			DispatchInternal(action);
		}

		/// <summary>
		/// アクションを実行
		/// </summary>
		/// <param name="setupper">アクションのセットアップ関数</param>
		/// <typeparam name="TAction">アクションタイプ</typeparam>
		public void Dispatch<TAction>(Func<TAction, TAction> setupper)
			where TAction : IAction
		{
			if (setupper == null) {
				throw new ArgumentNullException(nameof(setupper));
			}
			DispatchInternal(setupper(GetNewAction<TAction>()));
		}

		/// <summary>
		/// アクションを実行
		/// </summary>
		/// <param name="arg">セットアップ関数に渡す引数</param>
		/// <param name="setupper">アクションのセットアップ関数</param>
		/// <typeparam name="TAction">アクションタイプ</typeparam>
		/// <typeparam name="TArg">セットアップ関数の引数タイプ</typeparam>
		public void Dispatch<TAction, TArg>(TArg arg, Func<TAction, TArg, TAction> setupper)
			where TAction : IAction
		{
			if (setupper == null) {
				throw new ArgumentNullException(nameof(setupper));
			}
			DispatchInternal(setupper(GetNewAction<TAction>(), arg));
		}

		/// <summary>
		/// アクションのコールバック登録
		/// </summary>
		/// <param name="callback">コールバック</param>
		/// <typeparam name="TAction">アクション</typeparam>
		/// <returns>終了用</returns>
		public IDisposable RegisterActionCallback<TAction>(Action<TAction, Dispatcher> callback)
			where TAction : IAction
		{
			return GetOrAddActionReceiver<TAction>().AddCallback(callback);
		}

		/// <summary>
		/// アクションのコールバック登録
		/// </summary>
		/// <param name="callback">コールバック</param>
		/// <typeparam name="TAction">アクション</typeparam>
		/// <returns>終了用</returns>
		public IDisposable RegisterActionCallback<TAction>(Func<TAction, Dispatcher, Task> callback)
			where TAction : IAction
		{
			return GetOrAddActionReceiver<TAction>().AddCallback(callback);
		}

		/// <summary>
		/// コールバックオブジェクト登録
		/// </summary>
		/// <param name="callbackObject">
		/// コーバックオブジェクト<br/>
		/// IActionCallback&lt;&gt;、IActionCallbackAsync&lt;&gt;を継承
		/// </param>
		/// <returns></returns>
		public IDisposable RegisterActionCallback(object callbackObject)
		{
			List<IDisposable> disposables = new List<IDisposable>();
			var type = callbackObject.GetType();
			foreach (var iType in type.GetInterfaces()) {
				if (!iType.IsGenericType) { continue; }
				var iDef = iType.GetGenericTypeDefinition();
				if (iDef == typeof(IActionCallback<>)) {
					var actionType = iType.GenericTypeArguments[0];
					var callback = ActionCallbackDelegateCreater.CreateCallbackDelegate(actionType, callbackObject);
					disposables.Add(GetOrAddActionReceiver(actionType).AddCallbackDelegate(callback));
					continue;
				}
				if (iDef == typeof(IActionCallbackAsync<>)) {
					var actionType = iType.GenericTypeArguments[0];
					var callback = ActionCallbackDelegateCreater.CreateCallbackAsyncDelegate(actionType, callbackObject);
					disposables.Add(GetOrAddActionReceiver(actionType).AddCallbackAsyncDelegate(callback));
					continue;
				}
			}
			return new DisposableObject<List<IDisposable>>(list =>
			{
				foreach (var d in list) {
					d.Dispose();
				}
			}, disposables);
		}


		private void DispatchInternal<TAction>(TAction action)
			where TAction : IAction
		{
			IActionReceiver actionReceiver;
			if (!actionReceivers.TryGetValue(action.GetType(), out actionReceiver)) { return; }
			(actionReceiver as ActionReceiver<TAction>).Dispatch(action, this);
		}

		private TAction GetNewAction<TAction>()
			where TAction : IAction
		{
			TAction action = ActionPool<TAction>.GetOrAdd();
			action.Reset();
			return action;
		}

		private void InitializeStores(IEnumerable<Type> types)
		{
			var instances = types.Execute(type => Activator.CreateInstance(type) as IStore);
			var ids = stores.Initialize(instances);
			var reducerGroup = ids.SelectMany(sid => sid.store.Reducers.Select(reducer => (sid.store, reducer))).GroupBy(data => data.reducer.ActionType);
			foreach (var g in reducerGroup) {
				GetOrAddActionReceiver(g.Key).SetReducers(g);
			}
		}
		private void InitializeEffects(IEnumerable<Type> types)
		{
			var typeInstances = types.Execute(type => (type: type, instance: Activator.CreateInstance(type)));
			var effectGroup = typeInstances.SelectMany(ti => ti.type.GetGenericArgTypes(typeof(IEffect<>), 0)
				.Select(actionType => (actionType: actionType, instance: ti.instance)))
			.GroupBy(ti => ti.actionType, ti => ti.instance);
			foreach (var g in effectGroup) {
				GetOrAddActionReceiver(g.Key).SetEffects(g.OfType<IEffect>());
			}
			var effectAsyncGroup = typeInstances.SelectMany(ti => ti.type.GetGenericArgTypes(typeof(IEffectAsync<>), 0)
				.Select(actionType => (actionType: actionType, instance: ti.instance)))
			.GroupBy(ti => ti.actionType, ti => ti.instance);
			foreach (var g in effectAsyncGroup) {
				GetOrAddActionReceiver(g.Key).SetEffectAsyncs(g.OfType<IEffectAsync>());
			}
		}
		// private void InitializeEffectAsyncs(IEnumerable<Type> types)
		// {
		// 	var typeInstances = types.Execute(type => (type: type, instance: Activator.CreateInstance(type) as IEffectAsync));
		// 	var group = typeInstances.SelectMany(ti => ti.type.GetGenericArgTypes(typeof(IEffectAsync<>), 0)
		// 		.Select(actionType => (actionType: actionType, instance: ti.instance)))
		// 	.GroupBy(ti => ti.actionType, ti => ti.instance);
		// 	foreach (var g in group) {
		// 		GetOrAddActionReceiver(g.Key).SetEffectAsyncs(g);
		// 	}
		// }

		private IActionReceiver GetOrAddActionReceiver(Type actionType)
		{
			IActionReceiver receiver;
			if (!actionReceivers.TryGetValue(actionType, out receiver)) {
				var actionReceiverType = typeof(ActionReceiver<>).MakeGenericType(actionType);
				receiver = Activator.CreateInstance(actionReceiverType) as IActionReceiver;
				actionReceivers.Add(actionType, receiver);
			}
			return receiver;
		}
		private ActionReceiver<TAction> GetOrAddActionReceiver<TAction>()
			where TAction : IAction
		{
			return GetOrAddActionReceiver(typeof(TAction)) as ActionReceiver<TAction>;
		}

	}
}
