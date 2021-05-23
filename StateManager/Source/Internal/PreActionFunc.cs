
using System;

namespace StateManager
{
	internal class PreActionFunc<TAction> : IPreAction<TAction>
		where TAction : IAction
	{
		private Dispatcher dispatcher;
		private Action<TAction, Dispatcher> callback;

		public PreActionFunc(Dispatcher dispatcher, Action<TAction, Dispatcher> callback)
		{
			this.dispatcher = dispatcher;
			this.callback = callback;
		}

		public void PreAction(TAction action)
		{
			callback?.Invoke(action, dispatcher);
		}
	}
}