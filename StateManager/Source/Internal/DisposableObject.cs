using System;

namespace StateManager
{
	internal class DisposableObject<TValue> : IDisposable
	{
		private TValue value;
		private Action<TValue> disposeAction;

		public DisposableObject(TValue value, Action<TValue> disposeAction)
		{
			this.value = value;
			this.disposeAction = disposeAction;
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
