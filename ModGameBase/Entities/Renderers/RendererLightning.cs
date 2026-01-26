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

        private static int[]? types = null;
        public override int[] GetRenderedTypes()
        {
            if (types == null)
            {
                types =
                [
                    GlobalState.Registry.EntityRegistry.Get<Lightning>().Id,
                    GlobalState.Registry.EntityRegistry.Get<AimedLightning>().Id,
                ];
            }
            return types;
        }

        private static FastList<Vector3> positions = new FastList<Vector3>();
        private static FastList<Vector3> basePositions = new FastList<Vector3>();

        public override void RenderClientEnt(GraphicsDevice device, double deltaTime, ClientStates client, int type)
        {
            for (int i = 0; i < client.Current().entities.MaxEnts; i++)
            {
                var reference = client.Current().entities.GetReference(i);
                if (client.Current().entities.GetTypeById(reference.id) != type) continue;

                var entCurr = client.Current().entities.GetById(reference.id);
                var entPrev = client.Previous(1).entities.GetById(reference.id);

                // TODO cache this id
                if (type == GlobalState.Registry.EntityRegistry.Get<Lightning>().Id)
                {
                    positions.Clear();

                    Vector3 position = entPrev.GetInterpPosition(entCurr, client.TimeC);
                    Vector3 bottomPosition = entPrev.GetInterpVelocity(entCurr, client.TimeC);

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

                        DrawHelper3D.DrawLine(client.Renderer, client.currInterpState.camera, prev, current, Cube.CUBE_SCALE / 4f, new RendererDeferred.DrawMaterial(DrawHelper.WhitePixel), mesh, RectangleF.Empty, LightningColor);
                    }
                }
                if (type == GlobalState.Registry.EntityRegistry.Get<AimedLightning>().Id)
                {
                    positions.Clear();
                    basePositions.Clear();
                    positions.Add(entPrev.GetInterpPosition(entCurr, client.TimeC));
                    basePositions.Add(entPrev.GetInterpPosition(entCurr, client.TimeC));

                    int advanceNum = entPrev.GetInterpCounter(entCurr, 0, client.TimeC);
                    int seed = entCurr.counters[1];

                    float advanceLength = entPrev.GetInterpTimer(entCurr, 1, client.TimeC);
                    float maxLength = entPrev.GetInterpTimer(entCurr, 2, client.TimeC);
                    float advanceVariance = entPrev.GetInterpTimer(entCurr, 3, client.TimeC);

                    Vector3 advanceDirection = entPrev.GetInterpVelocity(entCurr, client.TimeC);

                    float totalLength = 0;

                    for (int j = 1; j < advanceNum; j++)
                    {
                        PCG32 pcg = new PCG32((ulong)(seed + i));

                        Vector3 o = new Vector3(pcg.NextFloat(-advanceVariance, advanceVariance), 0, 0);
                        o = Vector3.Transform(o, Matrix.CreateRotationZ(pcg.NextFloat(0, MathF.PI * 2)));
                        // TODO this shouldn't use camera
                        //o = Vector3.Transform(o, Matrix.CreateRotationX(-Main.camera.RotationEuler.X) * Matrix.CreateRotationY(-Main.camera.RotationEuler.Y));

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

                        DrawHelper3D.DrawLine(client.Renderer, client.currInterpState.camera, prev, current, Cube.CUBE_SCALE / 4f, new RendererDeferred.DrawMaterial(DrawHelper.WhitePixel),
                            mesh, RectangleF.Empty, LightningColor);
                    }
                }
            }
        }
    }
}
