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
		void SetEffects(IEnumerable<object> effects);
		void SetEffectAsyncs(IEnumerable<object> effectAsyncs);
		void SetPreActions(IEnumerable<object> preActions);
		IDisposable AddCallbackDelegate(Delegate callback);
		IDisposable AddCallbackAsyncDelegate(Delegate callback);
		void Dispatch(IAction action, Dispatcher dispatcher);
	}
	internal class ActionReceiver<TAction> : IActionReceiver
		where TAction : IAction
	{
		private (IStore store, IReducer reducer)[] Reducers = null;
		private IEffect<TAction>[] Effects = null;
		private IEffectAsync<TAction>[] EffectAsyncs = null;
		private IPreAction<TAction>[] PreActions = null;
		private event Action<TAction, Dispatcher> Callback = null;
		private event Func<TAction, Dispatcher, Task> CallbackAsync = null;
		private static ConcurrentBag<List<Task>> WaitListPool = new ConcurrentBag<List<Task>>();

		public void SetReducers(IEnumerable<(IStore store, IReducer reducer)> reducers)
		{
			Reducers = reducers.ToArray();
		}
		public void SetEffects(IEnumerable<object> effects)
		{
			Effects = effects.Cast<IEffect<TAction>>().ToArray();
		}
		public void SetEffectAsyncs(IEnumerable<object> effectAsyncs)
		{
			EffectAsyncs = effectAsyncs.Cast<IEffectAsync<TAction>>().ToArray();
		}
		public void SetPreActions(IEnumerable<object> preActions)
		{
			PreActions = preActions.Cast<IPreAction<TAction>>().ToArray();
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
		private void Dispatch(TAction action, Dispatcher dispatcher)
		{
			if (PreActions != null) {
				foreach (var preAction in PreActions) {
					preAction.PreAction(action);
				}
			}
			if (Reducers != null) {
				foreach (var reducer in Reducers) {
					reducer.store.Reduce(reducer.reducer, action);
				}
			}
			if (Effects != null) {
				foreach (var effect in Effects) {
					effect.Effect(action);
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
		public void Dispatch(IAction action, Dispatcher dispatcher)
		{
			Dispatch((TAction)action, dispatcher);
		}

		private async Task ExecuteEffects(TAction action, Dispatcher dispatcher)
		{
			List<Task> waitList;
			if (!WaitListPool.TryTake(out waitList)) {
				waitList = new List<Task>();
			}
			if (EffectAsyncs != null) {
				foreach (var effect in EffectAsyncs) {
					waitList.Add(effect.Effect(action));
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
