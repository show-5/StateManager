using System;
using System.Collections.Generic;
using System.Threading;

namespace StateManager
{
	/// <summary>
	/// ステート管理
	/// </summary>
	public interface IStore
	{
		/// <summary>
		/// ステート名
		/// nullだと名前を登録しない
		/// </summary>
		string Name { get; }

		/// <summary>
		/// ステートタイプ
		/// </summary>
		Type StateType { get; }

		/// <summary>
		/// アクション実行
		/// </summary>
		/// <param name="reducer">更新関数</param>
		/// <param name="action">アクション</param>
		void Reduce(IReducer reducer, IAction action);

		/// <summary>
		/// ステートへの参照を生成
		/// </summary>
		/// <returns>ステート</returns>
		IState CreateStateReference();

		/// <summary>
		/// 変更通知
		/// </summary>
		/// <param name="onUpdate">通知関数</param>
		/// <param name="initialCall">最初に1回呼ぶ</param>
		/// <param name="context">コンテキスト</param>
		/// <returns>解除用</returns>
		IDisposable Subscribe(StateOnUpdate onUpdate, SynchronizationContext context, bool initialCall);

		/// <summary>
		/// 更新関数リスト
		/// </summary>
		IReducer[] Reducers { get; }
	}
}
