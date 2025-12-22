using BepuPhysics.Constraints;
using BrUtility;
using Engine;
using Engine.Clients;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Rendering;
using static ViMG.HitboxManager;

namespace ViMG.Entities.Renderers
{
    public class RendererLightning : EntityRenderer
    {
        private static Color LightningColor = new Color(255, 253, 141);
        private VerySimpleMesh mesh;
        public RendererLightning(GraphicsDevice device) : base("lightning", device)
        {
            mesh = MeshHelper.MakeQuad(device, 1, 1, Enums.Alignment.Bottom);
        }

        private static Type[] types = [
            typeof(Lightning),
            typeof(AimedLightning),
        ];
        public override Type[] GetRenderedTypes()
        {
            return types;
        }

        public override void Render(GraphicsDevice device, double deltaTime, EntityManager entityManager, int renderedTypeIndex, List<Entity> entities)
        {
            return;

            //var lightnings = entityManager.GetAll<Lightning>();

            //foreach (Lightning lightning in lightnings)

            if (renderedTypeIndex == 0)
            {
                var iter = new Iterator<Lightning>(entities);
                while (iter.Next(out Lightning lightning))
                {
                    for (int i = 0; i < lightning.positions.Length; i++)
                    {
                        Vector3 prev;
                        if (i == 0)
                            prev = lightning.Position;
                        else prev = lightning.positions[i - 1];

                        Vector3 current = lightning.positions[i];

                        DrawHelper3D.DrawLine(prev, current, Cube.CUBE_SCALE / 4f, new Rendering.RendererDeferred.DrawMaterial(DrawHelper.WhitePixel), mesh, RectangleF.Empty, LightningColor);
                    }
                }
            }

            //var aimedLightnings = entityManager.GetAll<AimedLightning>();

            //foreach (AimedLightning lightning in aimedLightnings)
            if (renderedTypeIndex == 1)
            {
                var iter = new Iterator<AimedLightning>(entities);

                while (iter.Next(out AimedLightning lightning)) 
                {
                    for (int i = 0; i < lightning.positions.Length; i++)
                    {
                        Vector3 prev;
                        if (i == 0)
                            prev = lightning.Position;
                        else prev = lightning.positions[i - 1];

                        Vector3 current = lightning.positions[i];

                        DrawHelper3D.DrawLine(prev, current, Cube.CUBE_SCALE / 4f, new Rendering.RendererDeferred.DrawMaterial(DrawHelper.WhitePixel),
                            mesh, RectangleF.Empty, LightningColor);
                    }
                }
            }
        }

        private static FastList<Vector3> positions = new FastList<Vector3>();
        private static FastList<Vector3> basePositions = new FastList<Vector3>();

        public override void RenderClientEnt(GraphicsDevice device, double deltaTime, ClientStates client, string type)
        {
            for (int i = 0; i < client.Current().entities.MaxEnts; i++)
            {
                var reference = client.Current().entities.GetReference(i);
                // TODO get rid of str compare
                if (client.Current().entities.GetTypeById(reference.id) != type) continue;

                var entCurr = client.Current().entities.GetById(reference.id);
                var entPrev = client.Previous(1).entities.GetById(reference.id);

                if (type == typeof(Lightning).FullName)
                {
                    positions.Clear();

                    Vector3 position = entPrev.GetInterpPosition(entCurr);
                    Vector3 bottomPosition = entPrev.GetInterpVelocity(entCurr);

                    Vector3 direction = bottomPosition - position;
                    float distance = direction.Length();
                    direction.Normalize();

                    int numSplits = (int)(distance / Lightning.SPLIT_DISTANCE);

                    int seed = entCurr.counters[0];

                    for (int j = 0; j < numSplits; j++)
                    {
                        PCG32 pcg = new PCG32((ulong)(seed + j));

                        Vector3 newPos = position + direction * Lightning.SPLIT_DISTANCE * (j + 1);
                        newPos += new Vector3(pcg.NextFloat(-Cube.CUBE_SCALE * 2f, Cube.CUBE_SCALE * 2f), 0, pcg.NextFloat(-Cube.CUBE_SCALE * 2f, Cube.CUBE_SCALE * 2f));
                        positions.Add(newPos);
                    }

                    positions.Add(bottomPosition);

                    for (int j = 1; j < positions.Length; j++)
                    {
                        Vector3 prev = positions[j - 1];
                        Vector3 current = positions[j];

                        DrawHelper3D.DrawLine(prev, current, Cube.CUBE_SCALE / 4f, new RendererDeferred.DrawMaterial(DrawHelper.WhitePixel), mesh, RectangleF.Empty, LightningColor);
                    }
                }
                if (type == typeof(AimedLightning).FullName)
                {
                    positions.Clear();
                    basePositions.Clear();
                    positions.Add(entPrev.GetInterpPosition(entCurr));
                    basePositions.Add(entPrev.GetInterpPosition(entCurr));

                    int advanceNum = entPrev.GetInterpCounter(entCurr, 0);
                    int seed = entCurr.counters[1];

                    float advanceLength = entPrev.GetInterpTimer(entCurr, 1);
                    float maxLength = entPrev.GetInterpTimer(entCurr, 2);
                    float advanceVariance = entPrev.GetInterpTimer(entCurr, 3);

                    Vector3 advanceDirection = entPrev.GetInterpVelocity(entCurr);

                    float totalLength = 0;

                    for (int j = 1; j < advanceNum; j++)
                    {
                        PCG32 pcg = new PCG32((ulong)(seed + i));

                        Vector3 o = new Vector3(pcg.NextFloat(-advanceVariance, advanceVariance), 0, 0);
                        o = Vector3.Transform(o, Matrix.CreateRotationZ(pcg.NextFloat(0, MathF.PI * 2)));
                        // TODO this shouldn't use camera
                        o = Vector3.Transform(o, Matrix.CreateRotationX(-Main.camera.Rotation.X) * Matrix.CreateRotationY(-Main.camera.Rotation.Y));

                        Vector3 previousPosition = basePositions[j - 1];

                        float realAdvanceLength = advanceLength;

                        totalLength += advanceLength;
                        if (totalLength > maxLength)
                            realAdvanceLength = totalLength - maxLength;

                        Vector3 advance = advanceDirection * realAdvanceLength;
                        Vector3 nextPosition = previousPosition + advance;
                        positions.Add(nextPosition + o);
                        basePositions.Add(nextPosition);
                    }

                    for (int j = 1; j < positions.Length; j++)
                    {
                        Vector3 prev = positions[j - 1];
                        Vector3 current = positions[j];

                        DrawHelper3D.DrawLine(prev, current, Cube.CUBE_SCALE / 4f, new RendererDeferred.DrawMaterial(DrawHelper.WhitePixel),
                            mesh, RectangleF.Empty, LightningColor);
                    }
                }
            }
        }
    }
}
