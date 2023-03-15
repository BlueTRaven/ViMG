using BrUtility;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG
{
	public class ObjRegistry<T> where T : class, IRegisterable
	{
		private FastList<T> registry = new FastList<T>();
		private Dictionary<string, int> registryByName = new Dictionary<string, int>();
		public int Count => registry.Length;

		public void RegisterAll()
		{
			DoRegistration();
			PostRegistration();
		}

		protected virtual void DoRegistration()
        {

        }

		protected virtual void PostRegistration()
        {

        }

		protected virtual void Register(T obj)
		{
			registryByName.Add(obj.Identifier, registry.Length + 1);
			registry.Add(obj);
		}

		public T Get(int index)
		{
			if (index <= 0)
				return null;
			return registry[index - 1];
		}

		public T GetOrDefault(int index, T def)
        {
			var g = Get(index);

			if (g == null)
				g = def;
			return g;
        }

		public T Get(string name)
		{
			if (registryByName.ContainsKey(name))
				return Get(registryByName[name]);
			else
			{
				Console.WriteLine("Tried to get item with identifier {0} which does not exist.", name);
				return null;
			}
		}

		public IReadOnlyList<T> GetIterable()
		{
			return registry.Buffer;
		}
	}
}
