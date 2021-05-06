using System;

namespace StateManager
{
	internal class DisposableObject<TValue> : IDisposable
	{
		private Action<TValue> disposeAction;
		private TValue value;

		public DisposableObject(Action<TValue> disposeAction, TValue value)
		{
			this.disposeAction = disposeAction;
			this.value = value;
		}

		public void Dispose()
		{
			var action = disposeAction;
			if (action == null) { return; }
			disposeAction = null;
			action?.Invoke(value);
			value = default;
			GC.SuppressFinalize(this);
		}
		~DisposableObject()
		{
			disposeAction?.Invoke(value);
		}
	}
}
