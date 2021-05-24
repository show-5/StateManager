
using System;

namespace StateManager
{
	/// <summary>
	/// ステート
	/// </summary>
	public interface IState
	{
		/// <summary>
		/// 値
		/// </summary>
		object Value { get; }

		/// <summary>
		/// 値の型
		/// </summary>
		Type ValueType { get; }

		/// <summary>
		/// 名前
		/// </summary>
		string Name { get; }

		/// <summary>
		/// IState&lt;TState&gt; の型
		/// </summary>
		Type StateType { get; }
	}

	/// <summary>
	/// ステート
	/// </summary>
	/// <typeparam name="TState">ステートの型</typeparam>
	public interface IState<TState> : IState
	{
		/// <summary>
		/// 値
		/// </summary>
		/// <value></value>
		new TState Value { get; }
	}
}
