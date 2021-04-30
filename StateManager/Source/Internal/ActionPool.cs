using System;
using System.Collections.Concurrent;

namespace StateManager
{
	internal class ActionPool<TAction>
		where TAction : IAction
	{
		private static ConcurrentBag<TAction> Pool = new ConcurrentBag<TAction>();
		public static TAction GetOrAdd()
		{
			TAction action;
			if (!Pool.TryTake(out action)) {
				action = Activator.CreateInstance<TAction>();
			}
			return action;
		}
		public static void Return(TAction action)
		{
			Pool.Add(action);
		}
	}
}
