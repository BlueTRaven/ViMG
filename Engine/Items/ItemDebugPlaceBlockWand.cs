using BrUtility;
using Engine.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemDebugPlaceBlockWand : Item, IHasAreaEffect
    {
        private const int MAX_PLACEABLE_BLOCKS = 80;

        public ItemDebugPlaceBlockWand() : base("debug_placeblock_wand", new RectangleF(64, 64, 16, 16))
        {
            name = "DEBUG Place block wand";
            description = "Places blocks. For use in building.";
        }

        private static CubePosition[] allOffsets = new CubePosition[6]
        {
            new CubePosition(-1, 0, 0),
            new CubePosition(1, 0, 0),
            new CubePosition(0, -1, 0),
            new CubePosition(0, 1, 0),
            new CubePosition(0, 0, -1),
            new CubePosition(0, 0, 1)
        };

        private static CubePosition[] useOffsets = new CubePosition[4];
        private static CubePosition[] validPositions = new CubePosition[MAX_PLACEABLE_BLOCKS];

        public override bool RightClick(Player player, Inventory inventory, int index, Vector3 facing, out ActionStats actionStats)
        {
            if (player.IsLooking && player.CanPlace)
            {
                Cube startCube = player.world.ChunkManager.CubeView.GetCube(player.LookAtPos).GetOrDefault(Main.Registry.CubeRegistry.Air);

                //TODO safety
                //This doesn't have the safety checks anymore.
                CubePosition[] positions = GetAffectedPositions(player, inventory.Get(index), player.Position, player.LookAtPos.InWorldSpace(), player.LookAtNormal, out int num);

                player.world.ChunkManager.CubeView.SetCubes(positions[..num], startCube.Id);

                actionStats = new ActionStats(0.25f);
                return true;
            }

            return base.RightClick(player, inventory, index, facing, out actionStats);
        }

        public ref readonly ItemPickaxeHead.PickaxeStats GetStats(ItemInstance item)
        {
            throw new NotImplementedException();
        }

        //TODO performance
        //Batching gets
        public CubePosition[] GetAffectedPositions(Player player, ItemInstance item, Vector3 standingPosition, Vector3 hit, Vector3 normal, out int num)
        {
            Cube startCube = player.world.ChunkManager.CubeView.GetCube(CubePosition.FromWorldSpace(hit)).GetOrDefault(Main.Registry.CubeRegistry.Air);

            if (normal.X != 0 && normal.Y == 0 && normal.Z == 0)
            {
                //looking at left or right
                //Offsets are up, down, fwd, bwd
                useOffsets[0] = allOffsets[2];
                useOffsets[1] = allOffsets[3];
                useOffsets[2] = allOffsets[4];
                useOffsets[3] = allOffsets[5];
            }
            else if (normal.X == 0 && normal.Y != 0 && normal.Z == 0)
            {
                //looking at up or down
                //Offsets are left, right, fwd, bwd
                useOffsets[0] = allOffsets[0];
                useOffsets[1] = allOffsets[1];
                useOffsets[2] = allOffsets[4];
                useOffsets[3] = allOffsets[5];
            }
            else if (normal.X == 0 && normal.Y == 0 && normal.Z != 0)
            {
                //looking at fwd or bwd
                //Offsets are left, right, up, down
                useOffsets[0] = allOffsets[0];
                useOffsets[1] = allOffsets[1];
                useOffsets[2] = allOffsets[2];
                useOffsets[3] = allOffsets[3];
            }

            Array.Fill(validPositions, new CubePosition(-1, -1, -1));

            Queue<CubePosition> positions = new Queue<CubePosition>();
            HashSet<CubePosition> visitedPositions = new HashSet<CubePosition>();

            CubePosition nrmCP = CubePosition.FromWorldSpace(normal * Cube.CUBE_SCALE);
            positions.Enqueue(CubePosition.FromWorldSpace(hit) + nrmCP);

            int numIterated = 0;
            int numPlaced = 0;
            while (positions.Count > 0 && numIterated < MAX_PLACEABLE_BLOCKS * 4 && numPlaced < MAX_PLACEABLE_BLOCKS)
            {
                CubePosition pos = positions.Dequeue();
                CubePosition checkPos = pos - CubePosition.FromWorldSpace(normal * Cube.CUBE_SCALE);

                if (!visitedPositions.Contains(pos))
                {
                    visitedPositions.Add(pos);

                    if (player.world.ChunkManager.IsInWorldBounds(pos) && player.world.ChunkManager.IsInWorldBounds(checkPos))
                    {
                        if (player.world.ChunkManager.CubeView.GetCube(pos).GetOrDefault(Main.Registry.CubeRegistry.Air) == Main.Registry.CubeRegistry.Air && 
                            player.world.ChunkManager.CubeView.GetCube(checkPos).GetOrDefault(Main.Registry.CubeRegistry.Air) == startCube)
                        {
                            validPositions[numPlaced++] = pos;

                            for (int i = 0; i < 4; i++)
                            {
                                positions.Enqueue(pos + useOffsets[i]);
                            }
                        }
                    }
                }

                numIterated++;
            }

            num = numPlaced;

            return validPositions;
        }

        public bool CanPredictAir()
        {
            return true;
        }
    }
}
