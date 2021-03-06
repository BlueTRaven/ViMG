using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG
{
	public struct Optional<T> where T : class
	{
		public Optional(T obj)
		{
			this.obj = obj;
		}

		private T obj;

		public bool HasValue()
		{
			return obj != null;
		}

		public T Get()
		{
			if (obj == null)
				throw new NullReferenceException();
			else return obj;
		}

		public T GetOrDefault(T def)
		{
			if (obj == null)
				return def;
			else return obj;
		}
	}
}
