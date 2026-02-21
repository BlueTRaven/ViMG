using BrUtility;
using Engine.Networking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Rendering;

namespace ViMG.Entities
{
    public class Line : Entity, ISyncedEntity
    {
        public Vector3 endPosition;
        public float width;
        public float tileHeight;
        public int materialSet;
        //public readonly RendererDeferred.DrawMaterial material;
        //public readonly RectangleF sourceRectangle;
        public Color color;

		private float alive;
		private float time;

        public static (RendererDeferred.DrawMaterial, RectangleF) GetMaterialFromSet(int materialSet)
        {
            switch (materialSet) 
            {
                case 0:
                default:
                    return (new Rendering.RendererDeferred.DrawMaterial(DrawHelper.WhitePixel), RectangleF.Empty);
            }
        }

        public Line()
        {
        }

        public Line(Vector3 position, Vector3 endPosition, float width, float tileHeight, int materialSet, Color color, float time)
        {
            this.Position = position;
            this.endPosition = endPosition;
            this.width = width;
            this.tileHeight = tileHeight;
            this.materialSet = materialSet;
            //this.material = material;
            //this.sourceRectangle = sourceRectangle;
            this.color = color;
            this.time = time;
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

			AlwaysRender = true;
        }

        public override void Update(double deltaTime)
		{
			base.Update(deltaTime);

			alive += (float)deltaTime;

			if (alive >= time)
				world.EntityManager.Kill(this);
		}

        public Color GetColor()
        {
            if (Alive > time - 2)
            {
                float p = (Alive - (time - 2f)) / 2f;
                return color * p;
            }
            else return color;
        }

        public void GetSyncedEntity(out SyncedEntity state)
        {

            state = new SyncedEntity
            {
                position = Position,
                rotation = new Quaternion(endPosition.X, endPosition.Y, endPosition.Z, 1),
                timers = { [0] = time, [1] = width, [2] = tileHeight, [3] = alive},
                counters = { [0] = (int)color.PackedValue, [1] = materialSet }
            };
        }
    }
}
