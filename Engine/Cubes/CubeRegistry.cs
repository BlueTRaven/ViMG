using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ViMG.Cubes
{
	public class CubeRegistry : ObjRegistry<Cube>
	{
		public readonly Cube Air = new CubeAir();

		//flags:
		//sparse arrays where flag[cubeid] is the value of the flag, with the intention of low memory size and fast lookups.
		public bool[] noAo;

        protected override void DoRegistration()
		{
          
        }

        public override void PostRegistration()
        {
			noAo = new bool[Count + 1];
			noAo[0] = true;

			for (int i = 1; i <= Count; i++)
            {
				Get(i).SetId((ushort)(i));
			}

			base.PostRegistration();
        }

        public override void Register(Cube obj)
		{
			base.Register(obj);
		}
	}
}
