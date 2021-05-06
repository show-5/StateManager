

namespace StateManager
{
	/// <summary>
	/// アクションコールバック
	/// </summary>
	/// <typeparam name="TAction">アクションの型</typeparam>
	public interface IActionCallback<TAction>
		where TAction : IAction
	{
		/// <summary>
		/// コールバック
		/// </summary>
		/// <param name="action">アクション</param>
		/// <param name="dispatcher">Dispatcher</param>
		void ActionCallback(TAction action, Dispatcher dispatcher);
	}
}
