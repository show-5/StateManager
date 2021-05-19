using System.Threading.Tasks;

namespace StateManager
{
	/// <summary>
	/// アクションの実行関数（非同期）
	/// </summary>
	public interface IEffectAsync<TAction>
	{
		/// <summary>
		/// アクション実行（非同期）
		/// </summary>
		/// <param name="action">アクション</param>
		/// <returns>タスク</returns>
		Task Effect(TAction action);
	}
}
