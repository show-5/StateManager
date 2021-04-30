namespace StateManager
{
	/// <summary>
	/// アクションの実行関数
	/// </summary>
	public interface IEffect
	{
	}

	/// <summary>
	/// アクションの実行関数
	/// </summary>
	/// <typeparam name="TAction">アクションの型</typeparam>
	public interface IEffect<TAction> : IEffect
		where TAction : IAction
	{
		/// <summary>
		/// アクション実行
		/// </summary>
		/// <param name="action">アクション</param>
		/// <param name="dispatcher">ディスパッチャ</param>
		void Effect(TAction action, Dispatcher dispatcher);
	}
}
