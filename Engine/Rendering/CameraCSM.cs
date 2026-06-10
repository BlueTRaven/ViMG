using BrUtility;
using Engine.Common;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;

namespace Engine.Rendering
{
    public class CameraCSM : Engine.Common.Camera
    {
        private readonly float prevSplit;
        private readonly float split;
        private float ourNear;
        private float ourFar;

        private Matrix ourView;
        private Matrix ourProj;

        public CameraCSM(float near, float far, float prevSplit, float split) 
            : base(Vector3.Zero, Vector3.Zero, Vector3.One, near, far)
        {
            this.prevSplit = prevSplit;
            this.split = split;
        }

        public void Update(Engine.Common.Camera camera, Vector3 direction, float clampY = -1)
        {
            CalculateFrustumCorners(camera);

            Vector3 center = Vector3.Zero;
            foreach (Vector3 corner in corners)
            {
                center += corner;
            }
            center /= corners.Length;

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            float minZ = float.MaxValue;
            float maxZ = float.MinValue;

            /*foreach (Vector3 corner in corners)
            {
                Vector3 transformed = Vector3.Transform(corner, ourView);
                minX = MathHelper.Min(minX, transformed.X);
                maxX = MathHelper.Max(maxX, transformed.X);
                minY = MathHelper.Min(minY, transformed.Y);
                maxY = MathHelper.Max(maxY, transformed.Y);
                minZ = MathHelper.Min(minZ, transformed.Z);
                maxZ = MathHelper.Max(maxZ, transformed.Z);
            }*/

            // Calculate the radius of a bounding sphere surrounding the frustum corners
            var sphereRadius = 0.0f;
            for (var i = 0; i < 8; ++i)
            {
                var dist = (corners[i] - center).Length();
                sphereRadius = Math.Max(sphereRadius, dist);
            }

            sphereRadius = (float)Math.Ceiling(sphereRadius * 16.0f) / 16.0f;

            maxX = sphereRadius;
            maxY = sphereRadius;
            maxZ = sphereRadius;
            minX = -sphereRadius;
            minY = -sphereRadius;
            minZ = -sphereRadius;

            //maxExtents = new Vector3(sphereRadius);
            //minExtents = -maxExtents;

            /*float zRange = 2.5f;

            if (minZ < 0)
                minZ *= zRange;
            else minZ /= zRange;

            if (maxZ < 0)
                maxZ /= zRange;
            else maxZ *= zRange;*/

            float cascadeExtents = maxZ - minZ;
            Vector3 scp = center + direction * -minZ;

            ourView = Matrix.CreateLookAt(scp, center, new Vector3(0, 1, 0));

            ourProj = Matrix.CreateOrthographicOffCenter(minX, maxX, minY, maxY, 0, cascadeExtents);

            // Create the rounding matrix, by projecting the world-space origin and determining
            // the fractional offset in texel space
            var shadowMatrixTemp = ourView * ourProj;
            var shadowOrigin = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
            shadowOrigin = Vector4.Transform(shadowOrigin, shadowMatrixTemp);
            shadowOrigin = shadowOrigin * ((float)DirectionalLight.RT_SIZE / 2.0f);

            var roundedOrigin = new Vector4((float)Math.Round(shadowOrigin.X), (float)Math.Round(shadowOrigin.Y), (float)Math.Round(shadowOrigin.Z), (float)Math.Round(shadowOrigin.W));
            var roundOffset = roundedOrigin - shadowOrigin;
            roundOffset = roundOffset * (2.0f / (float)DirectionalLight.RT_SIZE);
            roundOffset.Z = 0.0f;
            roundOffset.W = 0.0f;

            var shadowProj = ourProj;
            //shadowProj.r[3] = shadowProj.r[3] + roundOffset;
            shadowProj.M41 += roundOffset.X;
            shadowProj.M42 += roundOffset.Y;
            shadowProj.M43 += roundOffset.Z;
            shadowProj.M44 += roundOffset.W;
            ourProj = shadowProj;
        }

        public Vector3[] GetCorners()
        {
            return corners;
        }

        private Vector3[] corners = new Vector3[8];
        //we can't use the BoundingFrustum class provided by Monogame (that's an alloction!) so we cache an array and calculate it ourselves.
        private void CalculateFrustumCorners(Engine.Common.Camera camera)
        {
            //dumbass shit to get mainCamera.GetProjectionMatrix to produce a new value (it's cached and only marked dirty under certain circumstances)
            //float near = mainCamera.Near;
            //float far = mainCamera.Far;
            //mainCamera.Near = Near;
            //mainCamera.Far = Far;

            ResetViewFrustumCorners();

            Matrix inv = Matrix.Invert(camera.GetViewMatrix() * camera.GetProjectionMatrix());

            for (int i = 0; i < 8; ++i)
                corners[i] = Vector4.Transform(corners[i], inv).ToVector3();

            for (var i = 0; i < 4; ++i)
            {
                var cornerRay = corners[i + 4] - corners[i];
                var nearCornerRay = cornerRay * prevSplit;
                var farCornerRay = cornerRay * split;
                corners[i + 4] = corners[i] + farCornerRay;
                corners[i] = corners[i] + nearCornerRay;
            }

            //corners = mainCamera.GetFrustum().GetCorners();

            /*int ci = 0;
            for (int x = 0; x < 2; ++x)
            {
                for (int y = 0; y < 2; ++y)
                {
                    for (int z = 0; z < 2; ++z)
                    {
                        Vector4 pt = Vector4.Transform(new Vector4(2.0f * x - 1.0f, 2.0f * y - 1.0f, 2.0f * z - 1.0f, 1.0f), inv);
                        corners[ci] = new Vector3(pt.X / pt.W, pt.Y / pt.W, pt.Z / pt.W);
                        ci++;
                    }
                }
            }*/

            //mainCamera.Near = near;
            //mainCamera.Far = far;
        }

        private void ResetViewFrustumCorners()
        {
            corners[0] = new Vector3(-1.0f, 1.0f, 0.0f);
            corners[1] = new Vector3(1.0f, 1.0f, 0.0f);
            corners[2] = new Vector3(1.0f, -1.0f, 0.0f);
            corners[3] = new Vector3(-1.0f, -1.0f, 0.0f);
            corners[4] = new Vector3(-1.0f, 1.0f, 1.0f);
            corners[5] = new Vector3(1.0f, 1.0f, 1.0f);
            corners[6] = new Vector3(1.0f, -1.0f, 1.0f);
            corners[7] = new Vector3(-1.0f, -1.0f, 1.0f);
        }

        protected override Matrix GetProjectionMatrixInternal()
        {
            return ourProj;
        }

        protected override Matrix GetViewMatrixInternal()
        {
            return ourView;
        }
    }
}
