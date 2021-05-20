namespace StateManager
{
	/// <summary>
	/// アクション実行後処理関数
	/// </summary>
	/// <typeparam name="TAction">アクションの型</typeparam>
	public interface IPostAction<TAction>
		where TAction : IAction
	{
		/// <summary>
		/// アクション実行後処理
		/// </summary>
		/// <param name="action">アクション</param>
		void PostAction(TAction action);
	}
}
