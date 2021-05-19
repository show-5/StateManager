namespace StateManager
{
	/// <summary>
	/// アクション実行前処理関数
	/// </summary>
	/// <typeparam name="TAction">アクションの型</typeparam>
	public interface IPreAction<TAction>
		where TAction : IAction
	{
		/// <summary>
		/// アクション実行前処理
		/// </summary>
		/// <param name="action">アクション</param>
		void PreAction(TAction action);
	}
}
