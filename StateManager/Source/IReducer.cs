using System;

namespace StateManager
{
	/// <summary>
	/// 更新関数
	/// </summary>
	public interface IReducer
	{
		/// <summary>
		/// アクションの型
		/// </summary>
		Type ActionType { get; }
	}

	/// <summary>
	/// 更新関数
	/// </summary>
	/// <typeparam name="TState">ステートの型</typeparam>
	public interface IReducer<TState> : IReducer
	{
		/// <summary>
		/// 更新関数
		/// </summary>
		/// <param name="stateValue">ステート</param>
		/// <param name="action">アクション</param>
		/// <returns>変更後のステート</returns>
		TState Reduce(TState stateValue, IAction action);
	}
}
