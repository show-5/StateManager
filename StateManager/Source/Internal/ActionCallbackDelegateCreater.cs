using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Tasks;

namespace StateManager
{
	internal static class ActionCallbackDelegateCreater
	{
		private static ConcurrentDictionary<Type, (MethodInfo methodInfo, Type delegateType)> actionCallbackMethods = new ConcurrentDictionary<Type, (MethodInfo methodInfo, Type delegateType)>();
		private static ConcurrentDictionary<Type, (MethodInfo methodInfo, Type delegateType)> actionCallbackAsyncMethods = new ConcurrentDictionary<Type, (MethodInfo methodInfo, Type delegateType)>();
		public static Delegate CreateCallbackDelegate(Type actionType, object instance)
		{
			var method = actionCallbackMethods.GetOrAdd(actionType, type => (
				methodInfo: typeof(IActionCallback<>).MakeGenericType(type).GetMethod("ActionCallback"),
				delegateType: typeof(Action<,>).MakeGenericType(type, typeof(Dispatcher))
			));
			return Delegate.CreateDelegate(method.delegateType, instance, method.methodInfo);
		}
		public static Delegate CreateCallbackAsyncDelegate(Type actionType, object instance)
		{
			var method = actionCallbackAsyncMethods.GetOrAdd(actionType, type => (
				methodInfo: typeof(IActionCallbackAsync<>).MakeGenericType(type).GetMethod("ActionCallback"),
				delegateType: typeof(Func<,,>).MakeGenericType(type, typeof(Dispatcher), typeof(Task))
			));
			return Delegate.CreateDelegate(method.delegateType, instance, method.methodInfo);
		}
	}
}
