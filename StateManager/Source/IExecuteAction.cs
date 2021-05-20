namespace StateManager
{
	/// <summary>
	/// アクションの実行関数
	/// </summary>
	/// <typeparam name="TAction">アクションの型</typeparam>
	public interface IExecuteAction<TAction>
		where TAction : IAction
	{
		/// <summary>
		/// アクション実行
		/// </summary>
		/// <param name="action">アクション</param>
		void ExecuteAction(TAction action);
	}
}
