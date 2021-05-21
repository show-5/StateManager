
using System;

namespace StateManager
{
	internal class ExecuteActionFunc<TAction> : IExecuteAction<TAction>
		where TAction : IAction
	{
		private Dispatcher dispatcher;
		private Action<TAction, Dispatcher> callback;

		public ExecuteActionFunc(Dispatcher dispatcher, Action<TAction, Dispatcher> callback)
		{
			this.dispatcher = dispatcher;
			this.callback = callback;
		}

		public void ExecuteAction(TAction action)
		{
			callback?.Invoke(action, dispatcher);
		}
	}
}