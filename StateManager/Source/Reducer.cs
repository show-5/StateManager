using System;

namespace StateManager
{
	/// <summary>
	/// 更新関数
	/// </summary>
	/// <typeparam name="TState">ステートの型</typeparam>
	/// <typeparam name="TAction">アクションの型</typeparam>
	public class Reducer<TState, TAction> : IReducer<TState>
		where TAction : IAction
	{
		private Func<TState, TAction, TState> reduceFunc { get; }

		/// <summary>
		/// アクションの型
		/// </summary>
		public Type ActionType => typeof(TAction);

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="reduceFunc">更新関数</param>
		public Reducer(Func<TState, TAction, TState> reduceFunc)
		{
			this.reduceFunc = reduceFunc;
		}

		/// <summary>
		/// 更新関数
		/// </summary>
		/// <param name="stateValue">ステート</param>
		/// <param name="action">アクション</param>
		/// <returns></returns>
		public TState Reduce(TState stateValue, IAction action) => reduceFunc.Invoke(stateValue, (TAction)action);
	}
}
