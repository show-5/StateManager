
namespace StateManager
{
	/// <summary>
	/// アクション<br/>
	/// キャッシュされ、再利用されます。
	/// </summary>
	public interface IAction
	{
		/// <summary>
		/// 初期化
		/// </summary>
		void Reset();
	}
}
