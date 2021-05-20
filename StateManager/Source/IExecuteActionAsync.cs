using System.Threading.Tasks;

namespace StateManager
{
	/// <summary>
	/// アクションの実行関数（非同期）
	/// </summary>
	public interface IExecuteActionAsync<TAction>
	{
		/// <summary>
		/// アクション実行（非同期）
		/// </summary>
		/// <param name="action">アクション</param>
		/// <returns>タスク</returns>
		Task ExecuteAction(TAction action);
	}
}
