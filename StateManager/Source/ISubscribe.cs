namespace StateManager
{
	/// <summary>
	/// 変更通知関数
	/// </summary>
	/// <typeparam name="TState">Stateの型</typeparam>
	public interface ISubscribe<TState>
	{
		/// <summary>
		/// 変更通知
		/// </summary>
		/// <param name="name">State名</param>
		/// <param name="oldState">旧State</param>
		/// <param name="newState">新State</param>
		void Subscribe(string name, TState oldState, TState newState);
	}
}
