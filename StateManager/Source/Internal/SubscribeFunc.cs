
using System;

namespace StateManager
{
	internal class SubscribeFunc<TState> : ISubscribe<TState>
	{
		private Action<TState, TState> callback;

		public SubscribeFunc(Action<TState, TState> callback)
		{
			this.callback = callback;
		}

		public void Subscribe(string name, TState oldState, TState newState)
		{
			callback?.Invoke(oldState, newState);
		}
	}
}