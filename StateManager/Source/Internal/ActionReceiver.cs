using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StateManager
{
	internal interface IActionReceiver
	{
		void SetReducers(IEnumerable<(IStore store, IReducer reducer)> reducers);
		void SetEffects(IEnumerable<IEffect> effects);
		void SetEffectAsyncs(IEnumerable<IEffectAsync> effectAsyncs);
		IDisposable AddCallbackDelegate(Delegate callback);
		IDisposable AddCallbackAsyncDelegate(Delegate callback);
	}
	internal class ActionReceiver<TAction> : IActionReceiver
		where TAction : IAction
	{
		private (IStore store, IReducer reducer)[] Reducers = null;
		private IEffect<TAction>[] Effects = null;
		private IEffectAsync<TAction>[] EffectAsyncs = null;
		private event Action<TAction, Dispatcher> Callback = null;
		private event Func<TAction, Dispatcher, Task> CallbackAsync = null;
		private static ConcurrentBag<List<Task>> WaitListPool = new ConcurrentBag<List<Task>>();

		public void SetReducers(IEnumerable<(IStore store, IReducer reducer)> reducers)
		{
			Reducers = reducers.ToArray();
		}
		public void SetEffects(IEnumerable<IEffect> effects)
		{
			Effects = effects.Cast<IEffect<TAction>>().ToArray();
		}
		public void SetEffectAsyncs(IEnumerable<IEffectAsync> effectAsyncs)
		{
			EffectAsyncs = effectAsyncs.Cast<IEffectAsync<TAction>>().ToArray();
		}

		public IDisposable AddCallback(Action<TAction, Dispatcher> callback)
		{
			Callback += callback;
			return new DisposableObject<(ActionReceiver<TAction> actionReceiver, Action<TAction, Dispatcher> func)>(arg =>
			{
				arg.actionReceiver.Callback -= arg.func;
			}, (this, callback));
		}
		public IDisposable AddCallback(Func<TAction, Dispatcher, Task> callback)
		{
			CallbackAsync += callback;
			return new DisposableObject<(ActionReceiver<TAction> actionReceiver, Func<TAction, Dispatcher, Task> func)>(arg =>
			{
				arg.actionReceiver.CallbackAsync -= arg.func;
			}, (this, callback));
		}
		public IDisposable AddCallbackDelegate(Delegate callback)
		{
			return AddCallback((Action<TAction, Dispatcher>)callback);
		}
		public IDisposable AddCallbackAsyncDelegate(Delegate callback)
		{
			return AddCallback((Func<TAction, Dispatcher, Task>)callback);
		}
		public void Dispatch(TAction action, Dispatcher dispatcher)
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
		private async Task ExecuteEffects(TAction action, Dispatcher dispatcher)
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

		private static void ReturnAction(TAction action)
		{
			ActionPool<TAction>.Return(action);
		}
	}
}
