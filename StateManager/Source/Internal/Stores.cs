using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace StateManager
{
	internal class Stores
	{
		private IStore[] stores;
		private Dictionary<string, int> nameTable = new Dictionary<string, int>();
		// private ConcurrentDictionary<Type, int> typeTable = new ConcurrentDictionary<Type, int>();
		private ILookup<Type, int> typeTable;
		/// <summary>
		/// 上位16ビット
		/// </summary>
		private uint idUpper;
		private static int idUpperBase = 100;

		public Stores()
		{
			idUpper = (uint)Interlocked.Increment(ref idUpperBase) << 16;
		}

		public bool TryGetID(string name, out int id)
		{
			if (nameTable.TryGetValue(name, out id)) {
				return true;
			}
			// var type = Type.GetType(name);
			// if (type != null) {
			// 	return typeTable.TryGetValue(type, out id);
			// }
			return false;
		}
		public bool TryGetID(Type type, out int id)
		{
			var list = typeTable[type];
			var count = list.Count();
			if (count == 1) {
				id = list.First();
				return true;
			}
			id = default;
			return false;
		}
		public bool TryGet(int id, out IStore store)
		{
			lock (stores) {
				uint l = (uint)id & 0x0000ffff;
				uint u = (uint)id & 0xffff0000;
				if (u != idUpper) {
					store = default;
					return false;
				}
				if (l < 0 || l >= stores.Length) {
					store = default;
					return false;
				}
				store = stores[(int)l];
			}
			return true;
		}
		public bool TryGet(string name, out IStore store)
		{
			int id;
			if (!TryGetID(name, out id)) {
				store = default;
				return false;
			}
			return TryGet(id, out store);
		}
		public bool TryGet(Type type, out IStore store)
		{
			int id;
			if (!TryGetID(type, out id)) {
				store = default;
				return false;
			}
			return TryGet(id, out store);
		}
		public bool TryGet<TState>(int id, out Store<TState> store)
		{
			lock (stores) {
				uint l = (uint)id & 0x0000ffff;
				uint u = (uint)id & 0xffff0000;
				if (u != idUpper) {
					store = default;
					return false;
				}
				if (l < 0 || l >= stores.Length) {
					store = default;
					return false;
				}
				store = stores[(int)l] as Store<TState>;
			}
			if (store == null) {
				return false;
			}
			return true;
		}
		public bool TryGet<TState>(string name, out Store<TState> store)
		{
			int id;
			if (!TryGetID(name, out id)) {
				store = default;
				return false;
			}
			return TryGet(id, out store);
		}
		public bool TryGet<TState>(Type type, out Store<TState> store)
		{
			int id;
			if (!TryGetID(type, out id)) {
				store = default;
				return false;
			}
			return TryGet(id, out store);
		}

		public int GetID(string name)
		{
			return nameTable[name];
		}
		public int GetID(Type type)
		{
			var list = typeTable[type];
			if (list.Count() > 1) {
				throw new InvalidOperationException();
			}
			return list.First();
		}
		public IEnumerable<int> GetIDs(Type type)
		{
			return typeTable[type];
		}
		public IStore Get(int id)
		{
			uint l = (uint)id & 0x0000ffff;
			uint u = (uint)id & 0xffff0000;
			if (u != idUpper) {
				throw new IndexOutOfRangeException();
			}
			return stores[(int)l];
		}
		public IStore Get(string name)
		{
			return Get(GetID(name));
		}
		public IStore Get(Type type)
		{
			return Get(GetID(type));
		}
		public Store<TState> Get<TState>(int id)
		{
			uint l = (uint)id & 0x0000ffff;
			uint u = (uint)id & 0xffff0000;
			if (u != idUpper) {
				throw new IndexOutOfRangeException();
			}
			return (Store<TState>)stores[(int)l];
		}
		public Store<TState> Get<TState>(string name)
		{
			return (Store<TState>)Get(GetID(name));
		}
		public Store<TState> Get<TState>(Type type)
		{
			return (Store<TState>)Get(GetID(type));
		}
		public (IStore store, int id)[] Initialize(IEnumerable<IStore> strs)
		{
			stores = strs.ToArray();
			var storeIDs = stores.Select((store, i) => (store: store, id: ((int)((uint)i | idUpper)))).ToArray();
			typeTable = storeIDs.ToLookup(si => si.store.StateType, si => si.id);
			nameTable = storeIDs.Where(si => si.store.Name != null).ToDictionary(si => si.store.Name, si => si.id);
			return storeIDs;
		}
		public IEnumerable<IState> GetStates()
		{
			return stores.Select(store => store.CreateStateReference());
		}
	}
}
