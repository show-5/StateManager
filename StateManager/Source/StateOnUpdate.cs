using System;
using System.Collections.Generic;

namespace StateManager
{
	/// <summary>
	/// ステート更新通知関数
	/// </summary>
	/// <param name="state">ステート</param>
	public delegate void StateOnUpdate(object state);

	/// <summary>
	/// ステート更新通知関数
	/// </summary>
	/// <param name="state">ステート</param>
	/// <typeparam name="TState">ステートの型</typeparam>
	public delegate void StateOnUpdate<TState>(TState state);
}
