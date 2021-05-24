using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
		private ConcurrentQueue<IAction> actionQueue = new ConcurrentQueue<IAction>();
		private bool runningAction = false;

		private enum FuncInterface
		{
			Subscribe,
			ExecuteAction,
			ExecuteActionAsync,
			PreAction,
			PostAction,
			_Count_,
		}
		private static readonly (Type, int)[] FuncInterfaceGeneric = new (Type, int)[(int)FuncInterface._Count_] {
			(typeof(ISubscribe<>), 0),
			(typeof(IExecuteAction<>), 0),
			(typeof(IExecuteActionAsync<>), 0),
			(typeof(IPreAction<>), 0),
			(typeof(IPostAction<>), 0)
		};

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="initializer">Initialization information.</param>
		public Dispatcher(DispatcherInitializer initializer)
		{
			InitializeStores(initializer.Stores);
			InitializeFunctionObjects(initializer.FunctionObjects);
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
			return stores.Get(id).StateReference();
		}

		/// <summary>
		/// ステートを取得
		/// </summary>
		/// <param name="name">ステート名</param>
		/// <returns>ステート</returns>
		public IState GetState(string name)
		{
			return stores.Get(name).StateReference();
		}

		/// <summary>
		/// ステートを取得<br/>
		/// 同じ型が複数登録されている場合は、例外が発生します。
		/// </summary>
		/// <param name="type">ステートの型</param>
		/// <returns>ステート</returns>
		public IState GetState(Type type)
		{
			return stores.Get(type).StateReference();
		}

		/// <summary>
		/// ステートを取得
		/// </summary>
		/// <param name="id">ステートID</param>
		/// <typeparam name="TState">ステートの型</typeparam>
		/// <returns>ステート</returns>
		public IState<TState> GetState<TState>(int id)
		{
			return stores.Get<TState>(id).StateReference();
		}

		/// <summary>
		/// ステートを取得
		/// </summary>
		/// <param name="name">ステート名</param>
		/// <typeparam name="TState">ステートの型</typeparam>
		/// <returns>ステート</returns>
		public IState<TState> GetState<TState>(string name)
		{
			return stores.Get<TState>(name).StateReference();
		}

		/// <summary>
		/// ステートを取得<br/>
		/// 同じ型が複数登録されている場合は、例外が発生します。
		/// </summary>
		/// <typeparam name="TState">ステートの型</typeparam>
		/// <returns>ステート</returns>
		public IState<TState> GetState<TState>()
		{
			return stores.Get<TState>(typeof(TState)).StateReference();
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
		/// <param name="initialCall">最初に1回呼ぶ</param>
		/// <param name="onUpdate">通知関数</param>
		/// <param name="context">コンテキスト（指定不要ならnull）</param>
		/// <returns>購読解除用Disposable</returns>
		public IDisposable Subscribe(int id, Action<object, object> onUpdate, bool initialCall, SynchronizationContext context = null)
		{
			return stores.Get(id).Subscribe(this, onUpdate, initialCall, context);
		}

		/// <summary>
		/// 変更があったときに通知<br/>
		/// 終了時は返ってきたオブジェクトをDispose<br/>
		/// 返り値を受け取らないなど、参照がなくなった場合にGCで解放
		/// </summary>
		/// <param name="name">ステート名</param>
		/// <param name="initialCall">最初に1回呼ぶ</param>
		/// <param name="onUpdate">通知関数</param>
		/// <param name="context">コンテキスト（指定不要ならnull）</param>
		/// <returns>購読解除用Disposable</returns>
		public IDisposable Subscribe(string name, Action<object, object> onUpdate, bool initialCall, SynchronizationContext context = null)
		{
			return stores.Get(name).Subscribe(this, onUpdate, initialCall, context);
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
		public IDisposable Subscribe(Type type, Action<object, object> onUpdate, bool initialCall, SynchronizationContext context = null)
		{
			return stores.Get(type).Subscribe(this, onUpdate, initialCall, context);
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
		public IDisposable Subscribe<TState>(int id, Action<TState, TState> onUpdate, bool initialCall, SynchronizationContext context = null)
		{
			return stores.Get<TState>(id).AddBindState(this, onUpdate, initialCall, context);
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
		public IDisposable Subscribe<TState>(string name, Action<TState, TState> onUpdate, bool initialCall, SynchronizationContext context = null)
		{
			return stores.Get<TState>(name).AddBindState(this, onUpdate, initialCall, context);
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
		public IDisposable Subscribe<TState>(Action<TState, TState> onUpdate, bool initialCall, SynchronizationContext context = null)
		{
			return stores.Get<TState>(typeof(TState)).AddBindState(this, onUpdate, initialCall, context);
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
		public IDisposable RegisterExecuteAction<TAction>(Action<TAction, Dispatcher> callback)
			where TAction : IAction
		{
			return RegisterFunctions(new ExecuteActionFunc<TAction>(this, callback));
		}

		/// <summary>
		/// アクションのコールバック登録
		/// </summary>
		/// <param name="callback">コールバック</param>
		/// <typeparam name="TAction">アクション</typeparam>
		/// <returns>終了用</returns>
		public IDisposable RegisterExecuteAction<TAction>(Func<TAction, Dispatcher, Task> callback)
			where TAction : IAction
		{
			return RegisterFunctions(new ExecuteActionAsyncFunc<TAction>(this, callback));
		}

		/// <summary>
		/// アクションのコールバック登録
		/// </summary>
		/// <param name="callback">コールバック</param>
		/// <typeparam name="TAction">アクション</typeparam>
		/// <returns>終了用</returns>
		public IDisposable RegisterPreAction<TAction>(Action<TAction, Dispatcher> callback)
			where TAction : IAction
		{
			return RegisterFunctions(new PreActionFunc<TAction>(this, callback));
		}

		/// <summary>
		/// アクションのコールバック登録
		/// </summary>
		/// <param name="callback">コールバック</param>
		/// <typeparam name="TAction">アクション</typeparam>
		/// <returns>終了用</returns>
		public IDisposable RegisterPostAction<TAction>(Action<TAction, Dispatcher> callback)
			where TAction : IAction
		{
			return RegisterFunctions(new PostActionFunc<TAction>(this, callback));
		}

		/// <summary>
		/// ReflectStateAttribute が設定されたフィールドにステートを設定
		/// </summary>
		/// <param name="o">設定先のオブジェクト</param>
		public void SetReflectState<T>(T o)
			where T : class
		{
			if (o == null) { return; }
			ReflectStateFields.Reflect(this, o);
		}
		/// <summary>
		/// ReflectStateAttribute が設定されたフィールドにステートを設定
		/// </summary>
		/// <param name="o">設定先のオブジェクト</param>
		public void SetReflectState<T>(ref T o)
			where T : struct
		{
			object obj = o;
			ReflectStateFields.Reflect(this, obj);
			o = (T)obj;
		}

		/// <summary>
		/// ReflectState情報取得
		/// </summary>
		/// <param name="type">型</param>
		/// <returns>情報</returns>
		public (FieldInfo fi, IState state)[] GetReflectInfos(Type type)
		{
			return ReflectStateFields.GetReflectInfos(this, type);
		}

		/// <summary>
		/// オブジェクトが持つ以下のインターフェースの関数を登録<br/>
		/// ISubscribe<br/>
		/// IExecuteAction<br/>
		/// IExecuteActionAsync<br/> 
		/// IPreAction<br/>
		/// IPostAction<br/>
		/// </summary>
		/// <param name="objs">オブジェクト（複数同時登録できます）</param>
		/// <returns>解除用Disposable</returns>
		public IDisposable RegisterFunctions(params object[] objs)
		{
			return new DisposableObject<(Dispatcher self, ILookup<Type, object>[] list)>(
				(this, RegisterFunctionsInternal(FunctionDataType.Flexible, objs)),
				args =>
				{
					args.self.stores.RemoveSubscribes(FunctionDataType.Flexible, args.list[(int)FuncInterface.Subscribe]);

					foreach (var g in args.list[(int)FuncInterface.ExecuteAction]) {
						args.self.GetOrAddActionReceiver(g.Key).RemoveExecuteActions(FunctionDataType.Flexible, g);
					}
					foreach (var g in args.list[(int)FuncInterface.ExecuteActionAsync]) {
						args.self.GetOrAddActionReceiver(g.Key).RemoveExecuteActionAsyncs(FunctionDataType.Flexible, g);
					}
					foreach (var g in args.list[(int)FuncInterface.PreAction]) {
						args.self.GetOrAddActionReceiver(g.Key).RemovePreActions(FunctionDataType.Flexible, g);
					}
					foreach (var g in args.list[(int)FuncInterface.PostAction]) {
						args.self.GetOrAddActionReceiver(g.Key).RemovePostActions(FunctionDataType.Flexible, g);
					}
				}
			);
		}

		private void DispatchInternal(IAction action)
		{
			lock (actionQueue) {
				if (runningAction) {
					actionQueue.Enqueue(action);
					return;
				}
				actionQueue.Enqueue(action);
				runningAction = true;
			}
			try {
				while (true) {
					IAction executeAction;
					lock (actionQueue) {
						if (!actionQueue.TryDequeue(out executeAction)) {
							runningAction = false;
							return;
						}
					}
					IActionReceiver actionReceiver;
					if (actionReceivers.TryGetValue(executeAction.GetType(), out actionReceiver)) {
						actionReceiver.Dispatch(executeAction, this);
					}
				}
			}
			finally {
				runningAction = false;
			}
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
		private void InitializeFunctionObjects(IEnumerable<Type> types)
		{
			RegisterFunctionsInternal(FunctionDataType.Fixed, types.Execute(t => Activator.CreateInstance(t)));
		}

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

		private ILookup<Type, object>[] RegisterFunctionsInternal(FunctionDataType functionDataType, IEnumerable<object> objs)
		{
			List<(Type dataType, object obj)>[] typeObjs = new List<(Type dataType, object obj)>[(int)FuncInterface._Count_];
			for (int i = 0; i < typeObjs.Length; ++i) {
				typeObjs[i] = new List<(Type dataType, object obj)>();
			};

			foreach (var obj in objs) {
				if (obj is FunctionObject fo) {
					fo.SetDispatcher(this);
				}
				var lookup = obj.GetType().GetGenericInterfaceArgTypes(FuncInterfaceGeneric);
				for (int i = 0; i < typeObjs.Length; ++i) {
					typeObjs[i].AddRange(lookup[FuncInterfaceGeneric[i].Item1].Select(t => (t, obj)));
				}
			}
			var list = typeObjs.Select(to => to.ToLookup(d => d.dataType, d => d.obj)).ToArray();

			stores.AddSubscribes(functionDataType, list[(int)FuncInterface.Subscribe]);

			foreach (var g in list[(int)FuncInterface.ExecuteAction]) {
				GetOrAddActionReceiver(g.Key).AddExecuteActions(functionDataType, g);
			}
			foreach (var g in list[(int)FuncInterface.ExecuteActionAsync]) {
				GetOrAddActionReceiver(g.Key).AddExecuteActionAsyncs(functionDataType, g);
			}
			foreach (var g in list[(int)FuncInterface.PreAction]) {
				GetOrAddActionReceiver(g.Key).AddPreActions(functionDataType, g);
			}
			foreach (var g in list[(int)FuncInterface.PostAction]) {
				GetOrAddActionReceiver(g.Key).AddPostActions(functionDataType, g);
			}

			return list;
		}
	}
}
