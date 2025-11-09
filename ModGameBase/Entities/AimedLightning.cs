using BrUtility;
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
    public class AimedLightning : Entity, IHitboxOwner
    {
        private static VerySimpleMesh mesh;
        private const float ADVANCE_TIME = 3f / 60f;

        private readonly HitboxManager.HitboxParameters parameters;
        private readonly Vector3 advanceDirection;
        private readonly float advanceLength;
        private readonly float maxLength;
        private readonly float advanceVariance;
        private readonly HitboxManager.HitboxStats stats;

        private float advanceTimer;
        public FastList<Vector3> positions = new FastList<Vector3>();
        private FastList<Vector3> basePositions = new FastList<Vector3>();

        private bool hasTouched;
        private int hitbox = -1;

        private float timer;

        public AimedLightning(Vector3 position, Vector3 advanceDirection, float advanceLength, float maxLength, float advanceVariance, HitboxManager.HitboxStats stats)
        {
            AlwaysRender = true;

            timer = 6f;

            this.Position = position;

            this.advanceDirection = advanceDirection;
            this.advanceLength = advanceLength;
            this.maxLength = maxLength;
            this.advanceVariance = advanceVariance;
            this.stats = stats;
            positions.Add(Position);
            basePositions.Add(Position);

            parameters = new HitboxManager.HitboxParameters()
            {
                owner = this,
                manager = this,
                bounds = Rectangle3D.Empty,
                direction = advanceDirection,
                stats = stats,
                canInteract = true
            };
        }

        public override void OnUnload()
        {
            base.OnUnload();

            if (hitbox != -1)
                world.HitboxManager.Remove(hitbox);
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            timer -= (float)deltaTime;
            advanceTimer -= (float)deltaTime;

            //TODO: make this collide with world

            float totalLength = 0;
            if (advanceTimer <= 0 && positions.Length < 8)
            {
                advanceTimer = ADVANCE_TIME;

                Vector3 o = new Vector3(Main.random.NextFloat(-advanceVariance, advanceVariance), 0, 0);
                o = Vector3.Transform(o, Matrix.CreateRotationZ(Main.random.NextFloat(0, MathF.PI * 2)));
                o = Vector3.Transform(o, Matrix.CreateRotationX(-Main.camera.Rotation.X) * Matrix.CreateRotationY(-Main.camera.Rotation.Y));

                Vector3 previousPosition = basePositions[positions.Length - 1];

                float realAdvanceLength = advanceLength;

                totalLength += advanceLength;
                if (totalLength > maxLength)
                    realAdvanceLength = totalLength - maxLength;

                Vector3 advance = advanceDirection * realAdvanceLength;
                Vector3 nextPosition = previousPosition + advance;
                positions.Add(nextPosition + o);
                basePositions.Add(nextPosition);

                if (positions.Length >= 8 || totalLength > maxLength)
                {
                    hasTouched = true;
                    timer = float.Min(timer, 5f / 60f);
                }

                if (hitbox == -1)
                {
                    HitboxManager.HitboxParameters parameters = this.parameters with 
                    {
                        bounds = Rectangle3D.FromTwoPositions(positions[positions.Length - 2], positions[positions.Length - 1]) 
                    };

                    hitbox = world.HitboxManager.Add(parameters);
                }
                else world.HitboxManager.Update(hitbox, Rectangle3D.FromTwoPositions(positions[positions.Length - 2], positions[positions.Length - 1]));
            }

            if (timer <= 0)
                world.EntityManager.Remove(this);
        }

        //public override void Draw(GraphicsDevice device, Effect effect)
        //{
        //    base.Draw(device, effect);

        //    if (mesh.IBO == null)
        //        mesh = MeshHelper.MakeQuad(device, 1, 1, Enums.Alignment.Bottom);
        //        //mesh = MeshHelper.MakeEnemyQuad(device, 1, 1);

        //    for (int i = 0; i < positions.Length; i++)
        //    {
        //        Vector3 prev;
        //        if (i == 0)
        //            prev = Position;
        //        else prev = positions[i - 1];

        //        Vector3 current = positions[i];

        //        DrawHelper3D.DrawLine(prev, current, Cube.CUBE_SCALE / 4f, new Rendering.RendererDeferred.DrawMaterial(DrawHelper.WhitePixel), 
        //            mesh, RectangleF.Empty, Lightning.LightningColor);
        //    }
        //}

        public void OnInteractWithOther(HitboxManager.Hitbox us, HitboxManager.Hitbox other)
        {
            if (((int)us.group & HitboxManager.GROUP_SOURCE_MASK) != ((int)other.group & HitboxManager.GROUP_SOURCE_MASK) &&
                ((int)other.group & HitboxManager.DAMAGE_TYPE_TAKE) > 0 && other.canInteract)
            {
                if (hitbox != -1)
                {
                    world.HitboxManager.Remove(hitbox);
                    hitbox = -1;
                }

                hasTouched = true;
                timer = float.Min(timer, 5f / 60f);
            }
        }
    }
}
