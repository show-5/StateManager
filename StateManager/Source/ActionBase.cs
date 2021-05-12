
namespace StateManager
{
	/// <summary>
	/// アクション<br/>
	/// キャッシュされ、再利用されます。
	/// </summary>
	public abstract class ActionBase : IAction
	{
		/// <summary>
		/// 初期化
		/// </summary>
		public virtual void Reset() { }

		public static void Dispatch<TAction>(Dispatcher dispatcher)
			where TAction : ActionBase
		{
			dispatcher.Dispatch<TAction>();
		}
	}

	/// <summary>
	/// アクション<br/>
	/// キャッシュされ、再利用されます。
	/// </summary>
	/// <typeparam name="TPayload">引数</typeparam>
	public abstract class ActionBase<TPayload> : IAction
	{
		/// <summary>
		/// パラメータ
		/// </summary>
		public TPayload Payload { get; internal set; }

		/// <summary>
		/// 初期化
		/// </summary>
		public virtual void Reset() { }

		public static void Dispatch<TAction>(Dispatcher dispatcher, TPayload payload)
			where TAction : ActionBase<TPayload>
		{
			dispatcher.Dispatch<TAction, TPayload>(payload);
		}
	}
}
