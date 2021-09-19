using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG
{
	public class ObjRegistry<T> where T : class, IRegisterable
	{
		private List<T> registry = new List<T>();
		private Dictionary<string, int> registryByName = new Dictionary<string, int>();
		public int Count => registry.Count;

		public virtual void RegisterAll()
		{

		}

		protected virtual void Register(T obj)
		{
			registryByName.Add(obj.Identifier, registry.Count + 1);
			registry.Add(obj);
		}

		public T Get(int index)
		{
			if (index <= 0)
				return null;
			return registry[index - 1];
		}

		public T Get(string name)
		{
			if (registryByName.ContainsKey(name))
				return Get(registryByName[name]);
			else return null;
		}

		public IReadOnlyList<T> GetIterable()
		{
			return registry;
		}
	}
}
