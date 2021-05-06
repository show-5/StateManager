
using System.Threading.Tasks;

namespace StateManager
{
	/// <summary>
	/// アクションコールバック（非同期）
	/// </summary>
	/// <typeparam name="TAction">アクションの型</typeparam>
	public interface IActionCallbackAsync<TAction>
		where TAction : IAction
	{
		/// <summary>
		/// コールバック
		/// </summary>
		/// <param name="action">アクション</param>
		/// <param name="dispatcher">Dispatcher</param>
		/// <returns>タスク</returns>
		Task ActionCallback(TAction action, Dispatcher dispatcher);
	}
}
