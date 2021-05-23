
using System;

namespace StateManager
{
	internal class PostActionFunc<TAction> : IPostAction<TAction>
		where TAction : IAction
	{
		private Dispatcher dispatcher;
		private Action<TAction, Dispatcher> callback;

		public PostActionFunc(Dispatcher dispatcher, Action<TAction, Dispatcher> callback)
		{
			this.dispatcher = dispatcher;
			this.callback = callback;
		}

		public void PostAction(TAction action)
		{
			callback?.Invoke(action, dispatcher);
		}
	}
}