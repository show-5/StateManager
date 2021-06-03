# StateManager

C# の Flux 風状態管理

- [Dispatcher](#dispatcher)
- [Store](#store)
- [Action](#action)
- [Functions](#functions)
- [Others](#others)

## Dispatcher

ストアを登録、アクションの実行、ステートの変更通知などを管理します。  

- 初期化

```C#
// アセンブリからStore、FunctionObjectをそれぞれ継承しているクラスを取得しDispatcherに登録する
Dispatcher dispatcher = new Dispatcher(new DispatcherInitializer()
	.ScanAssembly(typeof(Program).Assembly));

// 個別に登録
Dispatcher dispatcher = new Dispatcher(new DispatcherInitializer()
	.AddStore<FooStore>()
	.AddFunctionObject<FooFunctions>());
```

- 現在の State を取得

```C#
// 同じ型が複数登録してある場合は名前でアクセスしてください。
IState state = dispatcher.GetState(typeof(FooState));
IState state = dispatcher.GetState("State01");

IState<FooState> state = dispatcher.GetState<FooState>();
IState<FooState> state = dispatcher.GetState<FooState>("State01");
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
	// Reducerは副作用のない純粋関数であることが理想です。
	public override IReducer<FooState>[] Reducers => new IReducer<FooState>[]
	{
		// State を更新する場合は、書き換えるのではなく新たに値を生成するようにします。
		new Reducer<FooState, FooIncrementAction>((state, action) => new FooState(state.Count + 1)),
	};
}

// intなどでもStateにできます。
public class Int01Store : Store<int>
{
	// おそらく名前は必要になるでしょう
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

## Functions

FunctionObjectを継承した場合、DispatcherInitialize に登録することが出来ます。  
その場合、引数なしのコンストラクタを使用してインスタンスが生成されます。  
また、 DispatcherInitialize.ScanAssembly では自動で登録されます。

```C#
public class Functions : FunctionObject
	: IExecuteAction<A_Action>
	, IExecuteActionAsync<B_Action>
	, IPreAction<C_Action>
	, IPostAction<D_Action>
	, ISubscribe<FooState>
{
	// アクション実行時
	// Store の Reducer 関数実行後
	public void ExecuteAction(A_Action action)
	{
	}

	// IExecuteActionAsync で非同期にできます。
	public async Task ExecuteAction(B_Action action)
	{
	}

	// アクション実行前
	// Reducer より前
	public void PreAction(C_Action action)
	{
	}

	// アクション実行後
	// 全ての処理（非同期含む）を終了した後
	public void PostAction(D_Action action)
	{
	}

	// State 変更時
	public void Subscribe(string stateName, FooState oldState, FooState newState)
	{
	}
}
```

FunctionObject を使用しない場合や DIspatcherInitializer に登録しない場合、  
インスタンスを直接登録することが出来ます。  
この方法の場合、解除も可能です。  
これにより、Action をメッセージのように扱う事もできます。
```C#
// 戻り値を Dispose() することで解除する。
// 受け取らなかった場合GCで破棄された時解除される。
IDisposable disposable = dispatcher.RegisterFunctions(instance);
```

## Others

### Dispatcher.Subscribe
State の変更を受け取る関数を登録
```C#
// 戻り値を Dispose() することで解除する。
// 受け取らなかった場合GCで破棄された時解除される。
IDisposable disposable = dispatcher.Subscribe<FooState>(
	"FooStateName", 		// （省略可能）State 名
	(oldState, newState) => { },	// コールバック
	true,				// 登録時に一度コールバックを呼び出すか
	SynchronizationContext.Current	// （省略可能）呼び出しを行うコンテキスト
);
```
### Dispatcher.Register○○Action
アクション実行時（実行前、実行後）のコールバックを登録
- IDisposable RegisterExecuteAction<TAction>(Action<TAction, Dispatcher> callback)
- IDisposable RegisterExecuteAction<TAction>(Func<TAction, Dispatcher, Task> callback)
- IDisposable RegisterPreAction<TAction>(Action<TAction, Dispatcher> callback)
- IDisposable RegisterPostAction<TAction>(Action<TAction, Dispatcher> callback)

### ReflectStateAttribute
SetReflectState を呼び出すことで IState<> のフィールドに設定
```C#
public class TestClass
{
	[ReflectState]
	private IState<FooState> FooStateField;

	[field: ReflectState("StateName")]
	private IState<FooState> FooStateProperty { get; }
}
```
```C#
TestClass test = new TestClass();
// ReflectState を指定したフィールドに値を設定する
dispatcher.SetReflectState(test);
```

