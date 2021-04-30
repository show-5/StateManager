
using System;

namespace StateManager
{
	internal class State<TState> : IState<TState>
	{
		private WeakReference<Store<TState>> storeRef;

		public TState Value
		{
			get
			{
				Store<TState> store;
				if (!storeRef.TryGetTarget(out store)) {
					throw new NullReferenceException();
				}
				return store.State;

			}
		}
		object IState.Value => Value;

		public Type ValueType => typeof(TState);
		public Type StateType => typeof(IState<TState>);

		public State(Store<TState> store)
		{
			this.storeRef = new WeakReference<Store<TState>>(store);
		}
	}
}