using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Entities
{
    public class Worm : Entity
    {
        public Worm()
        {
        }

        public Worm(Vector3 position)
        {
            this.Position = position;
        }
    }
}
