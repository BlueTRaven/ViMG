using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;

namespace ViMG
{
    public static class LightHelper
    {
        //Distance at which lights attempt to transition to/from shadowmapped lights.
        public static float LODToNonShadowmappedDistance = Cube.CUBE_SCALE * 16;

        public readonly struct LightInfo
        {
            public readonly Vector3 Position;
            public readonly float Start;
            public readonly float End;
            public readonly Vector4 Color;

            public LightInfo(Vector3 position, float start, float end, Vector4 color)
            {
                Position = position;
                Start = start;
                End = end;
                Color = color;
            }
        }

        public static void UpdateLight(LightManager lightManager, LightInfo lightInfo, BoundingSphere? sphere, ref int lightIndex, ref bool lightIsShadowmapped, bool allowShadowmapped)
        {
            bool intersectsCamera = sphere.HasValue ? Main.camera.GetFrustum().Intersects(sphere.Value) : 
                Main.camera.GetFrustum().Contains(lightInfo.Position) == ContainmentType.Contains;

            if (!intersectsCamera)
            {
                if (lightIndex != -1)
                {
                    if (lightIsShadowmapped)
                        lightManager.RemoveShadowmapped(lightIndex);
                    else lightManager.Remove(lightIndex);
                    lightIndex = -1;
                }
            }
            else
            {
                if (lightIndex == -1)
                {
                    if (allowShadowmapped)
                        lightManager.AddShadowmapped(lightInfo.Position,
                            lightInfo.Start, lightInfo.End, new Color(lightInfo.Color), out lightIndex, out lightIsShadowmapped);
                    else
                    {
                        lightIndex = lightManager.Add(lightInfo.Position,
                          lightInfo.Start, lightInfo.End, new Color(lightInfo.Color));
                        lightIsShadowmapped = false;
                    }
                }
                else
                {
                    if (allowShadowmapped)
                    {
                        //Try to transition to a shadowmapped light or from one
                        Vector3 dir = lightInfo.Position - Main.camera.Position;

                        if (lightIsShadowmapped)
                        {
                            //if we're outside the shadowmapping LOD distance,
                            //Transition into a non-shadowmapped light.
                            if (dir.Length() > LODToNonShadowmappedDistance)
                            {
                                lightManager.RemoveShadowmapped(lightIndex);

                                lightIndex = lightManager.Add(lightInfo.Position,
                                    lightInfo.Start, lightInfo.End, new Color(lightInfo.Color));
                                lightIsShadowmapped = false;
                            }
                        }
                        else
                        {
                            //if we're inside the shadowmapping LOD distance,
                            //attempt to transition into a shadowmapped light.
                            if (dir.Length() <= LODToNonShadowmappedDistance)
                            {
                                lightManager.Remove(lightIndex);

                                lightManager.AddShadowmapped(lightInfo.Position,
                                    lightInfo.Start, lightInfo.End, new Color(lightInfo.Color), out lightIndex, out lightIsShadowmapped);
                            }
                        }
                    }
                }
            }
        }
    }
}
