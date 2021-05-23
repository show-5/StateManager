
using System;

namespace StateManager
{
	internal class State<TState> : IState<TState>
	{
		public TState Value { get; internal set; }
		object IState.Value => Value;

		public Type StateType => typeof(IState<TState>);

		public State(TState initialState)
		{
			Value = initialState;
		}
	}
}