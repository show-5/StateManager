
using System;
using System.Threading.Tasks;

namespace StateManager
{
	internal class ExecuteActionAsyncFunc<TAction> : IExecuteActionAsync<TAction>
		where TAction : IAction
	{
		private Dispatcher dispatcher;
		private Func<TAction, Dispatcher, Task> callback;

		public ExecuteActionAsyncFunc(Dispatcher dispatcher, Func<TAction, Dispatcher, Task> callback)
		{
			this.dispatcher = dispatcher;
			this.callback = callback;
		}

		public Task ExecuteAction(TAction action)
		{
			return callback?.Invoke(action, dispatcher);
		}
	}
}