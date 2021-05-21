using System;

namespace StateManager
{
	/// <summary>
	/// ステート参照用アトリビュート
	/// Dispatcher.SetReflectState 呼び出し時に設定される
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public class ReflectStateAttribute : Attribute
	{
		/// <summary>
		/// コンストラクタ
		/// </summary>
		public ReflectStateAttribute()
		{
		}

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="name">ステート名</param>
		public ReflectStateAttribute(string name)
		{
			Name = name;
		}

		/// <summary>
		/// ステート名
		/// </summary>
		public string Name { get; set; }
	}
}
