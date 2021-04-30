using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StateManager
{
	internal class ActionReceiver
	{
		public (IStore store, IReducer reducer)[] Reducers = null;
		public IEffect[] Effects = null;
		public IEffectAsync[] EffectAsyncs = null;
		public event Action<IAction, Dispatcher> Callback = null;
		public event Func<IAction, Dispatcher, Task> CallbackAsync = null;
		private static ConcurrentBag<List<Task>> WaitListPool = new ConcurrentBag<List<Task>>();


		public IDisposable AddCallback<TAction>(Action<TAction, Dispatcher> callback)
			where TAction : IAction
		{
			Action<IAction, Dispatcher> func = (action, dispatcher) => callback((TAction)action, dispatcher);
			Callback += func;
			return new DisposableObject<(ActionReceiver actionReceiver, Action<IAction, Dispatcher> func)>(arg =>
			{
				arg.actionReceiver.Callback -= arg.func;
			}, (this, func));
		}
		public IDisposable AddCallback<TAction>(Func<TAction, Dispatcher, Task> callback)
			where TAction : IAction
		{
			Func<IAction, Dispatcher, Task> func = (action, dispatcher) => callback((TAction)action, dispatcher);
			CallbackAsync += func;
			return new DisposableObject<(ActionReceiver actionReceiver, Func<IAction, Dispatcher, Task> func)>(arg =>
			{
				arg.actionReceiver.CallbackAsync -= arg.func;
			}, (this, func));
		}
		public void Dispatch<TAction>(TAction action, Dispatcher dispatcher)
			where TAction : IAction
		{
			if (Reducers != null) {
				foreach (var reducer in Reducers) {
					reducer.store.Reduce(reducer.reducer, action);
				}
			}
			if (Effects != null) {
				foreach (IEffect<TAction> effect in Effects) {
					effect.Effect(action, dispatcher);
				}
			}
			Callback?.Invoke(action, dispatcher);

			if (EffectAsyncs != null || CallbackAsync != null) {
				_ = ExecuteEffects(action, dispatcher);
			}
			else {
				ReturnAction(action);
			}
		}
		private async Task ExecuteEffects<TAction>(TAction action, Dispatcher dispatcher)
			where TAction : IAction
		{
			// await Task.WhenAll(effects.Cast<IEffectAsync<TAction>>().Select(effect => effect.Effect(action, this))).ConfigureAwait(false);
			// ReturnAction(action);
			List<Task> waitList;
			if (!WaitListPool.TryTake(out waitList)) {
				waitList = new List<Task>();
			}
			if (EffectAsyncs != null) {
				foreach (IEffectAsync<TAction> effect in EffectAsyncs) {
					waitList.Add(effect.Effect(action, dispatcher));
				}
			}
			var callback = CallbackAsync;
			if (callback != null) {
				waitList.Add(callback.Invoke(action, dispatcher));
			}
			foreach (var wait in waitList) {
				await wait.ConfigureAwait(false);
			}
			waitList.Clear();
			WaitListPool.Add(waitList);
			ReturnAction(action);
		}

		private static void ReturnAction<TAction>(TAction action)
			where TAction : IAction
		{
			ActionPool<TAction>.Return(action);
		}
	}
}
