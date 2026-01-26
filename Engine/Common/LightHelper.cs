using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Cubes;

namespace Engine.Common
{
    public static class LightHelper
    {
        public enum LightUpdateType
        {
            DontUpdate,
            UpdateClean,
            UpdateDirty,
        }

        //Distance at which lights attempt to transition to/from shadowmapped lights.
        public static float LODToNonShadowmappedDistance = Cube.CUBE_SCALE * 16;

        public readonly struct LightInfo
        {
            public readonly Vector3 Position;
            public readonly float Start;
            public readonly float End;
            public readonly Vector4 Color;

            public readonly bool UseNDotL;

            public LightInfo(Vector3 position, float start, float end, Vector4 color, bool useNDotL = true)
            {
                Position = position;
                Start = start;
                End = end;
                Color = color;

                UseNDotL = useNDotL;
            }
        }

        /// <summary>
        /// Updates the state of the light.
        /// If the light is shadowmapped, it will attempt to transition into or out of shadowmapped state when entering or leaving <see cref="LODToNonShadowmappedDistance"/>.
        /// </summary>
        /// <param name="lightInfo">The light information used to construct the light, and if update is true, to update it. Otherwise unused.</param>
        /// <param name="update">If true, the light will be updated to match lightInfo.</param>
        /// <param name="sphere">A (nullable) BoundingSphere used to determine if the light is within view of the camera. If null is passed, it will use the light's position instead.</param>
        /// <param name="lightIndex">The light reference.</param>
        /// <param name="lightIsShadowmapped">Whether or not the light is currently shadowmapped.</param>
        /// <param name="allowShadowmapped">Whether or not to allow the light to be shadowmapped. Shadowmapped lights are more expensive. Use as few of them as you can.</param>
        [Obsolete]
        public static void UpdateLight(LightManager lightManager, LightInfo lightInfo, LightUpdateType update, BoundingSphere? sphere, ref int lightIndex, ref bool lightIsShadowmapped, bool allowShadowmapped)
        {
            bool intersectsCamera = false;
            //bool intersectsCamera = sphere.HasValue ? Main.camera.GetFrustum().Intersects(sphere.Value) : 
            //    Main.camera.GetFrustum().Contains(lightInfo.Position) == ContainmentType.Contains;

            //offscreen - delete light
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
                //onscreen
                if (lightIndex == -1)
                {
                    if (allowShadowmapped)
                        lightManager.AddShadowmapped(lightInfo.Position,
                            lightInfo.Start, lightInfo.End, lightInfo.Color, out lightIndex, out lightIsShadowmapped);
                    else
                    {
                        lightIndex = lightManager.Add(lightInfo.Position,
                          lightInfo.Start, lightInfo.End, lightInfo.Color, lightInfo.UseNDotL);
                        lightIsShadowmapped = false;
                    }
                }
                else
                {
                    if (allowShadowmapped)
                    {
                        //Try to transition to a shadowmapped light or from one
                        Vector3 dir = lightInfo.Position;

                        if (lightIsShadowmapped)
                        {
                            if (update != LightUpdateType.DontUpdate)
                            {
                                bool markDirty = false;
                                if (lightInfo.End != lightManager.GetShadowmapped(lightIndex).end || update == LightUpdateType.UpdateDirty)
                                    markDirty = true;
                                lightManager.UpdateShadowmapped(lightIndex, lightInfo.Position, lightInfo.Start, lightInfo.End, lightInfo.Color, markDirty);
                            }

                            //if we're outside the shadowmapping LOD distance,
                            //Transition into a non-shadowmapped light.
                            if (dir.Length() > LODToNonShadowmappedDistance)
                            {
                                lightManager.RemoveShadowmapped(lightIndex);

                                lightIndex = lightManager.Add(lightInfo.Position,
                                    lightInfo.Start, lightInfo.End, lightInfo.Color);
                                lightIsShadowmapped = false;
                            }
                        }
                        else
                        {
                            if (update != LightUpdateType.DontUpdate)
                                lightManager.Update(lightIndex, lightInfo.Position, lightInfo.Start, lightInfo.End, lightInfo.Color);

                            //if we're inside the shadowmapping LOD distance,
                            //attempt to transition into a shadowmapped light.
                            if (dir.Length() <= LODToNonShadowmappedDistance)
                            {
                                lightManager.Remove(lightIndex);

                                lightManager.AddShadowmapped(lightInfo.Position,
                                    lightInfo.Start, lightInfo.End, lightInfo.Color, out lightIndex, out lightIsShadowmapped);
                            }
                        }
                    }
                }
            }
        }
    }
}
