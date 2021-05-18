
using System;

namespace StateManager
{
	internal class State<TState> : IState<TState>
	{
		public TState Value { get; internal set; }
		object IState.Value => Value;

		public State(TState initialState)
		{
			Value = initialState;
		}
	}
}