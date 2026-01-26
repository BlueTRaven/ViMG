using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using ViMG.Cubes;
using ViMG.Rendering;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Engine;

namespace ViMG.Entities.Renderers
{
    public class RendererSkullheadEye : EntityRenderer
    {
        private static RendererDeferred.DrawMaterial material = new RendererDeferred.DrawMaterial("skullhead_eye");
        private static EntityHelper.DirectionalSourceRect dsr = new EntityHelper.DirectionalSourceRect()
        {
            above = new RectangleF(0, 104, 52, 52),
            below = new RectangleF(0, 104, 52, 52),
            back = new RectangleF(0, 52, 52, 52),
            front = new RectangleF(0, 0, 52, 52),
            sideLeft = new RectangleF(0, 156, 52, 52),
            sideRight = new RectangleF(0, 104, 52, 52),
        };

        private VerySimpleMesh mesh;
        private VerySimpleMesh lineMesh;

        public RendererSkullheadEye(GraphicsDevice device) : base("skullhead_eye", device)
        {
            mesh = MeshHelper.MakeQuad(device, Cube.PIXEL_SCALE * 32, Cube.PIXEL_SCALE * 32, Enums.Alignment.Center);
            lineMesh = MeshHelper.MakeQuad(device, 1, 1, Enums.Alignment.Bottom);
        }

        private static int[]? types = null;
        public override int[] GetRenderedTypes()
        {
            if (types == null)
                types = [GlobalState.Registry.EntityRegistry.Get<SkullheadEye>().Id];
            return types;
        }

        //public override void Render(GraphicsDevice device, double deltaTime, EntityManager entityManager, int renderedTypeIndex, List<Entity> entities)
        //{
        //    var iter = new Iterator<SkullheadEye>(entities);
        //    //var eyes = entityManager.GetAll<SkullheadEye>();

        //    //foreach (SkullheadEye eye in eyes)
        //    while (iter.Next(out SkullheadEye eye))
        //    {
        //        RectangleF sourceRect = EntityHelper.GetEntityDirectionalSourceRect(eye.ai.Facing, dsr);

        //        Vector3 tintColor = eye.ai.InvulnTimer > 0 ? Color.Red.ToVector3() : Color.White.ToVector3();

        //        Main.Renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(material, mesh,
        //            Matrix.CreateRotationX(Math.Clamp(-Main.camera.RotationEuler.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
        //            Matrix.CreateRotationY(-Main.camera.RotationEuler.Y) *
        //            Matrix.CreateTranslation(eye.Position), sourceRect, tintColor));

        //        //if (eye.ai.Health < eye.MaxHealth)
        //        //    DrawHelper3D.DrawHealthbar(device, eye.ai.Health, eye.MaxHealth, eye.Position + new Vector3(0, Cube.CUBE_SCALE / 2f, 0));

        //        Vector3 offsetAnchor = eye.parent.Position;
        //        //Offset it slightly so we don't see the line poking through the billboard
        //        Vector3 offset = Vector3.Normalize(offsetAnchor - eye.Position) * Cube.CUBE_SCALE / 10f;
        //        DrawHelper3D.DrawLineTiled(eye.Position + offset, offsetAnchor - offset, Cube.PIXEL_SCALE * 2f, Cube.CUBE_SCALE, material,
        //            lineMesh, new RectangleF(52, 0, 4, 16), Color.White);
        //    }
        //}
    }
}
