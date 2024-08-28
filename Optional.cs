using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ViMG
{
	[DebuggerDisplay("HasValue = {HasValue()} Value = {Get()}")]
	public ref struct Optional<T> where T : class
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
			return obj;
		}

		public bool GetOut(out T obj)
		{
			obj = this.obj;
			if (HasValue()) return true; 
			else return false;
		}

		public T GetOrDefault(T def)
		{
			if (obj == null)
				return def;
			else return obj;
		}
	}

	public ref struct OptionalValue<T> where T : struct
	{
		public OptionalValue(T obj)
		{
			this.obj = obj;
		}

		private T obj;

		public bool HasValue()
		{
			return !obj.Equals(default(T));
		}

		public T Get()
		{
			if (obj.Equals(default(T)))
				throw new NullReferenceException();
			else return obj;
		}

		public T GetOrDefault(T def)
		{
			if (obj.Equals(default(T)))
				return def;
			else return obj;
		}
	}
}
