using System.Threading.Tasks;

namespace StateManager
{
	/// <summary>
	/// 関数オブジェクト
	/// Dispatcherの初期化時に渡す
	/// DispatcherInitializer.ScanAssembly 時に自動登録される
	/// 引数無しのコンストラクタで生成される
	/// </summary>
	public abstract class FunctionObject
	{
		/// <summary>
		/// Dispatcher
		/// </summary>
		protected Dispatcher Dispatcher { get; private set; }
		internal void SetDispatcher(Dispatcher dispatcher) => Dispatcher = dispatcher;
	}
}
