using System;
using System.Collections;
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

		internal Type StateType { get; }

		internal void Reduce(IReducer reducer, IAction action);

		internal IState StateReference();

		internal IDisposable Subscribe(Dispatcher dispatcher, Action<object, object> onUpdate, bool initialCall, SynchronizationContext context);

		internal IReducer[] Reducers { get; }

		internal void AddSubscribes(FunctionDataType type, IEnumerable subscribes);
		internal void RemoveSubscribes(FunctionDataType type, IEnumerable subscribes);
	}
}
