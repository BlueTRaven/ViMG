using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ViMG.ChunkStuff;
using ViMG.Items;

namespace ViMG.Cubes
{
    public class CubeObelisk : Cube
    {
        public CubeObelisk() : base("obelisk", new CubeFacingLayout(new RectangleF(64, 32, 16, 16), new RectangleF(64, 16, 16, 16)), Color.White, 0, 4)
        {
            Transparency = TransparencyValue.TransparentOccludesSiblings;
        }

        public override RectangleF GetSourceRect(RenderPass pass, CopiedChunkData data, ChunkRenderMesher.CubeMeshingParameters parameters, MeshHelper.CubeFace face)
        {
            if (data == null)
                return GetSourceRect(pass, data, parameters);

            MeshHelper.CubeFace obscuredFaces = ~parameters.faces;

            OffsetFromFace(face, out CubePosition abovePos, out CubePosition leftPos, out CubePosition rightPos, out CubePosition belowPos);
            bool above = false;     //adjacents
            bool left = false;
            bool right = false;
            bool below = false;
            bool aboveLeft = false; //corners
            bool aboveRight = false;
            bool belowLeft = false;
            bool belowRight = false;
            bool back = false;
            switch (face)
            {
                case MeshHelper.CubeFace.LEFT:
                    above = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.UP);
                    left = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.BACK);
                    right = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.FRONT);
                    below = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.DOWN);
                    back = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.RIGHT);
                    break;
                case MeshHelper.CubeFace.RIGHT:
                    above = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.UP);
                    left = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.FRONT);
                    right = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.BACK);
                    below = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.DOWN);
                    back = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.LEFT);
                    break;
                case MeshHelper.CubeFace.UP:
                    above = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.BACK);
                    below = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.FRONT);
                    left = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.LEFT);
                    right = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.RIGHT);
                    back = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.DOWN);
                    break;
                case MeshHelper.CubeFace.DOWN:
                    above = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.FRONT);
                    below = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.BACK);
                    left = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.LEFT);
                    right = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.RIGHT);
                    back = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.UP);
                    break;
                case MeshHelper.CubeFace.FRONT:
                    above = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.UP);
                    below = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.DOWN);
                    left = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.LEFT);
                    right = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.RIGHT);
                    back = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.BACK);
                    break;
                case MeshHelper.CubeFace.BACK:
                    above = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.UP);
                    below = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.DOWN);
                    left = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.RIGHT);
                    right = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.LEFT);
                    back = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.FRONT);
                    break;
                default:
                    break;
            }

            //have to manually query corners since those can't be included in meshing faces.
            aboveLeft = data.GetId(parameters.position + abovePos + leftPos) == Id;
            aboveRight = data.GetId(parameters.position + abovePos + rightPos) == Id;
            belowLeft = data.GetId(parameters.position + belowPos + leftPos) == Id;
            belowRight = data.GetId(parameters.position + belowPos + rightPos) == Id;

            //two connections, one behind
            if (left && back && !(above || below || right))
                return new RectangleF(176, 192, 16, 16);
            else if (below && back && !(above || left || right))
                return new RectangleF(192, 192, 16, 16);
            else if (right && back && !(above || left || below))
                return new RectangleF(192, 176, 16, 16);
            else if (above && back && !(below || left || right))
                return new RectangleF(176, 176, 16, 16);

            //two connections perpendicular without corner
            else if (below && right && !(above || left) && !belowRight)
                return new RectangleF(208, 176, 16, 16);
            else if (below && left && !(above || right) && !belowLeft)
                return new RectangleF(224, 176, 16, 16);
            else if (above && right && !(below || left) && !aboveRight)
                return new RectangleF(208, 192, 16, 16);
            else if (above && left && !(below || right) && !aboveLeft)
                return new RectangleF(224, 192, 16, 16);

            //two connections perpendicular with corner
            else if (below && right && !(above || left) && belowRight)
                return new RectangleF(208, 208, 16, 16);
            else if (below && left && !(above || right) && belowLeft)
                return new RectangleF(224, 208, 16, 16);
            else if (above && right && !(below || left) && aboveRight)
                return new RectangleF(208, 224, 16, 16);
            else if (above && left && !(below || right) && aboveLeft)
                return new RectangleF(224, 224, 16, 16);

            //three connections without any corners
            else if (left && above && below && !right && !aboveLeft && !belowLeft)
                return new RectangleF(240, 192, 16, 16);
            else if (left && below && right && !above && !belowLeft && !belowRight)
                return new RectangleF(256, 192, 16, 16);
            else if (right && above && below && !left && !belowRight && !aboveRight)
                return new RectangleF(256, 176, 16, 16);
            else if (left && above && right && !below && !aboveLeft && !aboveRight)
                return new RectangleF(240, 176, 16, 16);

            //three connections with one left-hand corner and without one right-hand corner
            else if (left && above && below && !right && !aboveLeft && belowLeft)
                return new RectangleF(240, 224, 16, 16);
            else if (left && below && right && !above && !belowLeft && belowRight)
                return new RectangleF(256, 224, 16, 16);
            else if (right && above && below && !left && !belowRight && aboveRight)
                return new RectangleF(256, 208, 16, 16);
            else if (left && above && right && !below && !aboveRight && aboveLeft)
                return new RectangleF(240, 208, 16, 16);

            //three connections without one left-hand corner and with one right-hand corner
            else if (left && above && below && !right && aboveLeft && !belowLeft)
                return new RectangleF(272, 192, 16, 16);
            else if (left && below && right && !above && belowLeft && !belowRight)
                return new RectangleF(288, 192, 16, 16);
            else if (right && above && below && !left && belowRight && !aboveRight)
                return new RectangleF(288, 176, 16, 16);
            else if (left && above && right && !below && aboveRight && !aboveLeft)
                return new RectangleF(272, 176, 16, 16);

            //three connections with all corners
            else if (left && above && below && !right && aboveLeft && belowLeft)
                return new RectangleF(272, 224, 16, 16);
            else if (left && below && right && !above && belowLeft && belowRight)
                return new RectangleF(288, 224, 16, 16);
            else if (right && above && below && !left && belowRight && aboveRight)
                return new RectangleF(288, 208, 16, 16);
            else if (left && above && right && !below && aboveLeft && aboveRight)
                return new RectangleF(272, 208, 16, 16);

            //four connections without any corners
            else if (above && right && below && left && !aboveLeft && !aboveRight && !belowLeft && !belowRight)
                return new RectangleF(176, 208, 16, 16);

            //four connections and one corner
            else if (above && right && below && left && aboveLeft && !aboveRight && !belowLeft && !belowRight)
                return new RectangleF(288, 240, 16, 16);
            else if (above && right && below && left && !aboveLeft && aboveRight && !belowLeft && !belowRight)
                return new RectangleF(288, 256, 16, 16);
            else if (above && right && below && left && !aboveLeft && !aboveRight && belowLeft && !belowRight)
                return new RectangleF(272, 240, 16, 16);
            else if (above && right && below && left && !aboveLeft && !aboveRight && !belowLeft && belowRight)
                return new RectangleF(272, 256, 16, 16);

            //four connections and two corners
            else if (above && right && below && left && aboveLeft && aboveRight && !belowLeft && !belowRight)
                return new RectangleF(256, 240, 16, 16);
            else if (above && right && below && left && !aboveLeft && aboveRight && !belowLeft && belowRight)
                return new RectangleF(256, 256, 16, 16);
            else if (above && right && below && left && aboveLeft && !aboveRight && belowLeft && !belowRight)
                return new RectangleF(240, 240, 16, 16);
            else if (above && right && below && left && !aboveLeft && !aboveRight && belowLeft && belowRight)
                return new RectangleF(240, 256, 16, 16);
            //opposing corners too
            else if (above && right && below && left && !aboveLeft && aboveRight && belowLeft && !belowRight)
                return new RectangleF(176, 224, 16, 16);
            else if (above && right && below && left && aboveLeft && !aboveRight && !belowLeft && belowRight)
                return new RectangleF(192, 224, 16, 16);

            //four connections and three corners
            else if (above && right && below && left && aboveLeft && aboveRight && !belowLeft && belowRight)
                return new RectangleF(224, 240, 16, 16);
            else if (above && right && below && left && !aboveLeft && aboveRight && belowLeft && belowRight)
                return new RectangleF(224, 256, 16, 16);
            else if (above && right && below && left && aboveLeft && aboveRight && belowLeft && !belowRight)
                return new RectangleF(208, 240, 16, 16);
            else if (above && right && below && left && aboveLeft && !aboveRight && belowLeft && belowRight)
                return new RectangleF(208, 256, 16, 16);

            //four connections, four corners 
            else if (above && right && below && left && aboveLeft && aboveRight && belowLeft && belowRight)
                return new RectangleF(192, 208, 16, 16);

            //no connections
            else if (!(above || right || below || left))
                return new RectangleF(160, 208, 16, 16);

            //two connections, both perpendicular and parallel to each other
            else if (above && below && !(left || right))
                return new RectangleF(160, 176, 16, 16);
            else if (left && right && !(above || below))
                return new RectangleF(160, 192, 16, 16);
            else if (above || below && !(left || right))
                return new RectangleF(160, 176, 16, 16);
            else if (left || right && !(above || below))
                return new RectangleF(160, 192, 16, 16);
            else return new RectangleF();
        }

        //offsets are always ordered:
        //above, left, right, below
        private void OffsetFromFace(MeshHelper.CubeFace face, out CubePosition aboveOut,
            out CubePosition leftOut, out CubePosition rightOut, out CubePosition belowOut)
        {
            CubePosition leftCS = new CubePosition(1, 0, 0, CubePosition.CoordinateSpace.ChunkSpace);
            CubePosition rightCS = new CubePosition(-1, 0, 0, CubePosition.CoordinateSpace.ChunkSpace);
            CubePosition aboveCS = new CubePosition(0, 1, 0, CubePosition.CoordinateSpace.ChunkSpace);
            CubePosition belowCS = new CubePosition(0, -1, 0, CubePosition.CoordinateSpace.ChunkSpace);
            CubePosition frontCS = new CubePosition(0, 0, -1, CubePosition.CoordinateSpace.ChunkSpace);
            CubePosition backCS = new CubePosition(0, 0, 1, CubePosition.CoordinateSpace.ChunkSpace);
            switch (face)
            {
                case MeshHelper.CubeFace.LEFT:
                    aboveOut = aboveCS;
                    leftOut = backCS;
                    rightOut = frontCS;
                    belowOut = belowCS;
                    break;
                case MeshHelper.CubeFace.RIGHT:
                    aboveOut = aboveCS;
                    leftOut = frontCS;
                    rightOut = backCS;
                    belowOut = belowCS;
                    break;
                case MeshHelper.CubeFace.UP:
                    aboveOut = backCS;
                    belowOut = frontCS;
                    leftOut = leftCS;
                    rightOut = rightCS;
                    break;
                case MeshHelper.CubeFace.DOWN:
                    aboveOut = backCS;
                    belowOut = frontCS;
                    leftOut = leftCS;
                    rightOut = rightCS;
                    break;
                case MeshHelper.CubeFace.FRONT:
                    aboveOut = aboveCS;
                    belowOut = belowCS;
                    leftOut = leftCS;
                    rightOut = rightCS;
                    break;
                case MeshHelper.CubeFace.BACK:
                    aboveOut = aboveCS;
                    belowOut = belowCS;
                    leftOut = rightCS;
                    rightOut = leftCS;
                    break;
                default:
                    aboveOut = new CubePosition();
                    leftOut = new CubePosition();
                    rightOut = new CubePosition();
                    belowOut = new CubePosition();
                    break;
            }
        }

        public override void GetDrops(List<ItemInstance> itemsToDrop)
        {
            base.GetDrops(itemsToDrop);

            DropSelf(itemsToDrop);
        }
    }
}
