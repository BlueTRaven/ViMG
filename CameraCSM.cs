using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;

namespace ViMG
{
    public class CameraCSM : Camera
    {
        private Camera mainCamera;
        private float ourNear;
        private float ourFar;

        private Matrix ourView;
        private Matrix ourProj;

        public CameraCSM(Camera mainCamera, float near, float far) 
            : base(Vector3.Zero, Vector3.Zero, Vector3.One, near, far, false)
        {
            this.mainCamera = mainCamera;
        }

        public void Update(Vector3 direction)
        {
            CalculateFrustumCorners();

            Vector3 center = Vector3.Zero;
            foreach (Vector3 corner in corners)
            {
                center += corner;
            }
            center /= corners.Length;

            ourView = Matrix.CreateLookAt(center, center - direction, new Vector3(0, 1, 0));

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            float minZ = float.MaxValue;
            float maxZ = float.MinValue;

            foreach (Vector3 corner in corners)
            {
                Vector3 transformed = Vector3.Transform(corner, ourView);
                minX = MathHelper.Min(minX, transformed.X);
                maxX = MathHelper.Max(maxX, transformed.X);
                minY = MathHelper.Min(minY, transformed.Y);
                maxY = MathHelper.Max(maxY, transformed.Y);
                minZ = MathHelper.Min(minZ, transformed.Z);
                maxZ = MathHelper.Max(maxZ, transformed.Z);
            }

            float zRange = 2.5f;

            if (minZ < 0)
                minZ *= zRange;
            else minZ /= zRange;

            if (maxZ < 0)
                maxZ /= zRange;
            else maxZ *= zRange;

            ourProj = Matrix.CreateOrthographicOffCenter(minX, maxX, minY, maxY, minZ, maxZ);
        }

        private Vector3[] corners = new Vector3[8];
        //we can't use the BoundingFrustum class provided by Monogame (that's an alloction!) so we cache an array and calculate it ourselves.
        private void CalculateFrustumCorners()
        {
            //dumbass shit to get mainCamera.GetProjectionMatrix to produce a new value (it's cached and only marked dirty under certain circumstances)
            float near = mainCamera.Near;
            float far = mainCamera.Far;
            mainCamera.Near = Near;
            mainCamera.Far = Far;
            int i = 0;
            Matrix inv = Matrix.Invert(mainCamera.GetViewMatrix() * mainCamera.GetProjectionMatrix());

            //corners = mainCamera.GetFrustum().GetCorners();

            for (int x = 0; x < 2; ++x)
            {
                for (int y = 0; y < 2; ++y)
                {
                    for (int z = 0; z < 2; ++z)
                    {
                        Vector4 pt = Vector4.Transform(new Vector4(2.0f * x - 1.0f, 2.0f * y - 1.0f, 2.0f * z - 1.0f, 1.0f), inv);
                        corners[i] = new Vector3(pt.X / pt.W, pt.Y / pt.W, pt.Z / pt.W);
                        i++;
                    }
                }
            }

            mainCamera.Near = near;
            mainCamera.Far = far;
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
