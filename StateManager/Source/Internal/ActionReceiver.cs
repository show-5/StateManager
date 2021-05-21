using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StateManager
{
	internal interface IActionReceiver
	{
		void SetReducers(IEnumerable<(IStore store, IReducer reducer)> reducers);
		void AddExecuteActions(FunctionDataType type, IEnumerable executeActions);
		void AddExecuteActionAsyncs(FunctionDataType type, IEnumerable executeActionAsyncs);
		void AddPreActions(FunctionDataType type, IEnumerable preActions);
		void AddPostActions(FunctionDataType type, IEnumerable postActions);

		void RemoveExecuteActions(FunctionDataType type, IEnumerable executeActions);
		void RemoveExecuteActionAsyncs(FunctionDataType type, IEnumerable executeActionAsyncs);
		void RemovePreActions(FunctionDataType type, IEnumerable preActions);
		void RemovePostActions(FunctionDataType type, IEnumerable postActions);

		// IDisposable AddCallbackDelegate(Delegate callback);
		// IDisposable AddCallbackAsyncDelegate(Delegate callback);
		void Dispatch(IAction action, Dispatcher dispatcher);
	}
	internal class ActionReceiver<TAction> : IActionReceiver
		where TAction : IAction
	{
		private (IStore store, IReducer reducer)[] Reducers = null;
		private FunctionDatas<IExecuteAction<TAction>> ExecuteActions = new FunctionDatas<IExecuteAction<TAction>>();
		private FunctionDatas<IExecuteActionAsync<TAction>> ExecuteActionAsyncs = new FunctionDatas<IExecuteActionAsync<TAction>>();
		private FunctionDatas<IPreAction<TAction>> PreActions = new FunctionDatas<IPreAction<TAction>>();
		private FunctionDatas<IPostAction<TAction>> PostActions = new FunctionDatas<IPostAction<TAction>>();

		// private IExecuteAction<TAction>[] ExecuteActions = null;
		// private IExecuteActionAsync<TAction>[] ExecuteActionAsyncs = null;
		// private IPreAction<TAction>[] PreActions = null;
		// private IPostAction<TAction>[] PostActions = null;
		// private event Action<TAction, Dispatcher> Callback = null;
		// private event Func<TAction, Dispatcher, Task> CallbackAsync = null;
		private static ConcurrentBag<List<Task>> WaitListPool = new ConcurrentBag<List<Task>>();

		public void SetReducers(IEnumerable<(IStore store, IReducer reducer)> reducers)
		{
			Reducers = reducers.ToArray();
		}
		public void AddExecuteActions(FunctionDataType type, IEnumerable executeActions)
		{
			ExecuteActions.AddRange(type, executeActions.Cast<IExecuteAction<TAction>>());
		}
		public void AddExecuteActionAsyncs(FunctionDataType type, IEnumerable executeActionAsyncs)
		{
			ExecuteActionAsyncs.AddRange(type, executeActionAsyncs.Cast<IExecuteActionAsync<TAction>>());
		}
		public void AddPreActions(FunctionDataType type, IEnumerable preActions)
		{
			PreActions.AddRange(type, preActions.Cast<IPreAction<TAction>>());
		}
		public void AddPostActions(FunctionDataType type, IEnumerable postActions)
		{
			PostActions.AddRange(type, postActions.Cast<IPostAction<TAction>>());
		}

		public void RemoveExecuteActions(FunctionDataType type, IEnumerable executeActions)
		{
			ExecuteActions.RemoveRange(type, executeActions.Cast<IExecuteAction<TAction>>());
		}
		public void RemoveExecuteActionAsyncs(FunctionDataType type, IEnumerable executeActionAsyncs)
		{
			ExecuteActionAsyncs.RemoveRange(type, executeActionAsyncs.Cast<IExecuteActionAsync<TAction>>());
		}
		public void RemovePreActions(FunctionDataType type, IEnumerable preActions)
		{
			PreActions.RemoveRange(type, preActions.Cast<IPreAction<TAction>>());
		}
		public void RemovePostActions(FunctionDataType type, IEnumerable postActions)
		{
			PostActions.RemoveRange(type, postActions.Cast<IPostAction<TAction>>());
		}

		// public IDisposable AddCallback(Action<TAction, Dispatcher> callback)
		// {
		// 	Callback += callback;
		// 	return new DisposableObject<(ActionReceiver<TAction> actionReceiver, Action<TAction, Dispatcher> func)>(
		// 		(this, callback),
		// 		arg =>
		// 		{
		// 			arg.actionReceiver.Callback -= arg.func;
		// 		}
		// 	);
		// }
		// public IDisposable AddCallback(Func<TAction, Dispatcher, Task> callback)
		// {
		// 	CallbackAsync += callback;
		// 	return new DisposableObject<(ActionReceiver<TAction> actionReceiver, Func<TAction, Dispatcher, Task> func)>(
		// 		(this, callback),
		// 		arg =>
		// 		{
		// 			arg.actionReceiver.CallbackAsync -= arg.func;
		// 		}
		// 	);
		// }
		// public IDisposable AddCallbackDelegate(Delegate callback)
		// {
		// 	return AddCallback((Action<TAction, Dispatcher>)callback);
		// }
		// public IDisposable AddCallbackAsyncDelegate(Delegate callback)
		// {
		// 	return AddCallback((Func<TAction, Dispatcher, Task>)callback);
		// }
		private void Dispatch(TAction action, Dispatcher dispatcher)
		{
			PreActions.Execute<TAction>(action, (preAction, act) =>
			{
				preAction.PreAction(act);
			});
			// if (!PreActions.Empty) {
			// 	foreach (var preAction in PreActions) {
			// 		preAction.PreAction(action);
			// 	}
			// }
			if (Reducers != null) {
				foreach (var reducer in Reducers) {
					reducer.store.Reduce(reducer.reducer, action);
				}
			}
			ExecuteActions.Execute<TAction>(action, (executeAction, act) =>
			{
				executeAction.ExecuteAction(act);
			});
			// if (!ExecuteActions.Empty) {
			// 	foreach (var executeAction in ExecuteActions) {
			// 		executeAction.ExecuteAction(action);
			// 	}
			// }
			// Callback?.Invoke(action, dispatcher);

			// if (!ExecuteActionAsyncs.Empty || CalllbackAsync != null) {
			if (!ExecuteActionAsyncs.Empty) {
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
			ExecuteActionAsyncs.Execute<(TAction action, List<Task> waitList)>((action, waitList), (executeAction, arg) =>
			{
				arg.waitList.Add(executeAction.ExecuteAction(arg.action));
			});
			// if (!ExecuteActionAsyncs.Empty) {
			// 	foreach (var executeAction in ExecuteActionAsyncs) {
			// 		waitList.Add(executeAction.ExecuteAction(action));
			// 	}
			// }
			// var callback = CallbackAsync;
			// if (callback != null) {
			// 	waitList.Add(callback.Invoke(action, dispatcher));
			// }
			foreach (var wait in waitList) {
				await wait.ConfigureAwait(false);
			}
			waitList.Clear();
			WaitListPool.Add(waitList);
			PostAction(action);
		}

		private void PostAction(TAction action)
		{
			if (!PostActions.Empty) {
				PostActions.Execute<TAction>(action, (postAction, act) =>
				{
					postAction.PostAction(act);
				});
				// foreach (var postAction in PostActions) {
				// 	postAction.PostAction(action);
				// }
			}

			ActionPool<TAction>.Return(action);
		}
	}
}
