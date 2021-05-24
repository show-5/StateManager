
using System;

namespace StateManager
{
	internal class State<TState> : IState<TState>
	{
		public TState Value { get; internal set; }
		object IState.Value => Value;


		public Type ValueType => typeof(TState);
		public string Name { get; }
		public Type StateType => typeof(IState<TState>);

		public State(TState initialState, string name)
		{
			Value = initialState;
			Name = name;
		}
	}
}