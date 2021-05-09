# StateManager

C# の Flux 風状態管理

- [Dispatcher](#dispatcher)
- [Store](#store)
- [Action](#action)
- [Effect](#effect)
- [ActionCallback](#actioncallback)
- [Subscribe](#subscribe)

## Dispatcher

ストアを登録、アクションの実行、ステートの変更通知などを管理します。  
Store、Effect は初期化時のみ登録可能です。

- 初期化

```C#
// アセンブリからIStore、IEffect、IEffectAsyncをそれぞれ継承しているクラスを取得しDispatcherに登録する
Dispatcher dispatcher = new Dispatcher(new DispatcherInitializer()
	.ScanAssembly(typeof(Program).Assembly));

// 個別に登録
Dispatcher dispatcher = new Dispatcher(new DispatcherInitializer()
	.AddStore<FooStore>()
	.AddEffect<FooEffect>()
	.AddEffectAsync<FooEffectAsync>());
```

- 現在の State を取得

```C#
// 同じ型が複数登録してある場合、例外が発生します。
// 名前でアクセスしてください。
IState state = dispatcher.GetState(typeof(FooState));
IState state = dispatcher.GetState("State01");

IState<FooState> state = dispatcher.GetState<FooState>();
IState<FooState> state = dispatcher.GetState<FooState>("State01");

// 内部で管理されているIDでもアクセスできます。
int stateID = dispatcher.GetStateID("State01");
// int stateID = dispatcher.GetStateID<FooState>();
// if (dispatcher.TryGetStateID<FooState>(out stateID)) {}
IState<FooState> state = dispatcher.GetState<FooState>(stateID);

// 一覧取得
IEnumerable<IState> states = dispatcher.GetStates();
```

## Store

State の管理、更新関数などを定義します。

```C#
using StateManager;

// State
public class FooState
{
	public int Count { get; }
	public FooState(int count)
	{
		Count = count;
	}
}

// Store
public class FooStore : Store<FooState>
{
	// 名前を指定しなかった（又はnullの）場合、Stateの型でのみアクセスできます。
	// 同じ型を複数登録する場合は名前の登録がないとアクセスできなくなります。
	public override string Name => "State01";

	// Stateの初期化関数
	// オーバーライドしなかった場合は Activator.CreateInstance<TState>() を使用して生成されますので
	// 引数無しのコンストラクタがない場合、例外になります。
	public override FooState InitialState() => new FooState(0);

	// Stateの補正
	public override FooState Validate(FooState state) => state;

	// Stateが同じとみなせるか
	// trueが返った場合、Stateは更新されず、通知も行われません。
	// オーバーライドしなかった場合は、 EqualityComparer<TState>.Default.Equals(v1, v2) で比較されます。
	public override bool IsEquivalent(FooState v1, FooState v2) => v1.Count == v2.Count;

	// 更新関数
	// Reducerは純粋関数であることが理想です。
	// 外部APIや乱数、時間関数の呼び出しなどがある場合は、Effectの使用を検討してください。
	public override IReducer<FooState>[] Reducers => new IReducer<FooState>[]
	{
		new Reducer<FooState, FooIncrementAction>((state, action) => new FooState(state.Count + 1)),
	};
}

// intなどでもStateにできます。
public class Int01Store : Store<int>
{
	// おそらく名前は必須になるでしょう
	public override string Name => "Int01";

	public override IReducer<int>[] Reducers => new IReducer<int>[]
	{
		new Reducer<int, FooIncrementAction>((state, action) => state + 1),
	};
}
```

## Action

アクションは引数無しのコンストラクタが必要です。  
実行後、アクションはキャッシュされ、次の呼び出しで使われます。

- ActionBase を継承する場合

```C#
public class BarAction : ActionBase
{
}
public class FooBarAction : ActionBase<(int IntValue, string StringValue)>
{
}

dispatcher.Dispatch<BarAction>();
// ここで渡した値は"Payload"という名前で取得できます。
dispatcher.Dispatch<FooBarAction, (int, string)>((100, "hogehoge"));
```

- 自前で実装

```C#
// 引数無しで生成できる必要がある
public class FooAction : IAction
{
	public string Value { get; private set; }

	// 値のリセットを行う
	public void Reset()
	{
		Value = "";
	}

	public FooAction SetValue(string value)
	{
		Value = value;
		return this;
	}
}

// 実行
// 値をセットする関数を渡す
string text = "abcde";
dispatcher.Dispatch<FooAction, string>(text, (action, value) => action.SetValue(value));
```

## Effect

副作用のある処理などはこちら

```C#
public class Effects
	: IEffect<FooAction>
	, IEffectAsync<FooBarAction>
{
	public void Effect(FooAction action, Dispatcher dispatcher)
	{
	}
	public async Task Effect(FooBarAction action, Dispatcher dispatcher)
	{
	}
}
```

## ActionCallback

登録、解除可能なアクション実行時の処理です。

```C#
public class TestClass
{
	private List<IDisposable> disposables = new List<IDisposable>();

	public TestClass()
	{
		// Dispatcherはstaticやシングルトンなどで保持しておく
		// Dispatcher dispatcher = ...

		// 戻り値のIDisposableを受け取らないとGCで回収される
		disposables.Add(dispatcher.RegisterActionCallback<FooAction>(ActionCallback));
		disposables.Add(dispatcher.RegisterActionCallback<FooAction>(ActionCallbackAsync));
	}
	// 終了処理
	public void Release()
	{
		foreach (var disposable in disposables) {
			disposable.Dispose();
		}
		disposables.Clear();
	}
	private void ActionCallback(FooAction action, Dispatcher dispatcher)
	{
	}
	private async Task ActionCallbackAsync(FooAction action, Dispatcher dispatcher)
	{
	}
}
```

IActionCallback、IActionCallbackAsync を継承することによりまとめて登録できます。

```C#
public class TestClass
	: IActionCallback<FooAction>
	, IActionCallbackAsync<FooBarAction>
{
	private IDisposable disposable;

	public TestClass()
	{
		disposable = dispatcher.RegisterActionCallback(this);
	}
	// 終了処理
	public void Release()
	{
		disposable?.Dispose();
		disposable = null;
	}
	public void ActionCallback(FooAction action, Dispatcher dispatcher)
	{
	}
	public async Task ActionCallbackAsync(FooBarAction action, Dispatcher dispatcher)
	{
	}
}
```

## Subscribe

State の変更を受け取ります。

```C#
// 戻り値のIDisposableを受け取らないとGCで回収される
// 第２引数がnullの場合は別コンテキストで呼び出される可能性がある
// 　第１引数：コールバック
// 　第２引数：[Default=null]SynchronizationContext
// 　第３引数：[Default=true]初期化用に１度呼び出すか
IDisposable disposable = dispatcher.Subscribe<FooState>(
	state => Console.WriteLine(state.Count.ToString()),
	SynchronizationContext.Current,
	true));

```
