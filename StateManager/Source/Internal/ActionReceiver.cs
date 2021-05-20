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
		void SetExecuteActions(IEnumerable<object> executeActions);
		void SetExecuteActionAsyncs(IEnumerable<object> executeActionAsyncs);
		void SetPreActions(IEnumerable<object> preActions);
		void SetPostActions(IEnumerable<object> postActions);
		IDisposable AddCallbackDelegate(Delegate callback);
		IDisposable AddCallbackAsyncDelegate(Delegate callback);
		void Dispatch(IAction action, Dispatcher dispatcher);
	}
	internal class ActionReceiver<TAction> : IActionReceiver
		where TAction : IAction
	{
		private (IStore store, IReducer reducer)[] Reducers = null;
		private IExecuteAction<TAction>[] ExecuteActions = null;
		private IExecuteActionAsync<TAction>[] ExecuteActionAsyncs = null;
		private IPreAction<TAction>[] PreActions = null;
		private IPostAction<TAction>[] PostActions = null;
		private event Action<TAction, Dispatcher> Callback = null;
		private event Func<TAction, Dispatcher, Task> CallbackAsync = null;
		private static ConcurrentBag<List<Task>> WaitListPool = new ConcurrentBag<List<Task>>();

		public void SetReducers(IEnumerable<(IStore store, IReducer reducer)> reducers)
		{
			Reducers = reducers.ToArray();
		}
		public void SetExecuteActions(IEnumerable<object> executeActions)
		{
			ExecuteActions = executeActions.Cast<IExecuteAction<TAction>>().ToArray();
		}
		public void SetExecuteActionAsyncs(IEnumerable<object> executeActionAsyncs)
		{
			ExecuteActionAsyncs = executeActionAsyncs.Cast<IExecuteActionAsync<TAction>>().ToArray();
		}
		public void SetPreActions(IEnumerable<object> preActions)
		{
			PreActions = preActions.Cast<IPreAction<TAction>>().ToArray();
		}
		public void SetPostActions(IEnumerable<object> postActions)
		{
			PostActions = postActions.Cast<IPostAction<TAction>>().ToArray();
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
			if (ExecuteActions != null) {
				foreach (var executeAction in ExecuteActions) {
					executeAction.ExecuteAction(action);
				}
			}
			Callback?.Invoke(action, dispatcher);

			if (ExecuteActionAsyncs != null || CallbackAsync != null) {
				_ = ExecuteExecuteActions(action, dispatcher);
			}
			else {
				PostAction(action);
			}
		}
		public void Dispatch(IAction action, Dispatcher dispatcher)
		{
			Dispatch((TAction)action, dispatcher);
		}

		private async Task ExecuteExecuteActions(TAction action, Dispatcher dispatcher)
		{
			List<Task> waitList;
			if (!WaitListPool.TryTake(out waitList)) {
				waitList = new List<Task>();
			}
			if (ExecuteActionAsyncs != null) {
				foreach (var executeAction in ExecuteActionAsyncs) {
					waitList.Add(executeAction.ExecuteAction(action));
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
			PostAction(action);
		}

		private void PostAction(TAction action)
		{
			if (PostActions != null) {
				foreach (var postAction in PostActions) {
					postAction.PostAction(action);
				}
			}

			ActionPool<TAction>.Return(action);
		}
	}
}
