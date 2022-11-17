using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Items;

namespace ViMG.Cubes
{
    public class CubeOrangeCoveredStone : Cube
    {
        public CubeOrangeCoveredStone() : base("stone_covered_orange", new CubeFacingLayout(new RectangleF(0, 96, 16, 16), new RectangleF(16, 96, 16, 16), new RectangleF(16, 0, 16, 16)), Color.White, 3)
        {
            Name = "Orange Mushroom Covered Stone";
        }

        public override void GetDrops(List<ItemInstance> itemsToDrop)
        {
            base.GetDrops(itemsToDrop);

            //drop stone instead of orange stuff
            itemsToDrop.Add(new ItemInstance(Main.Registry.ItemRegistry.Get("item_stone"), 1, 1));
        }

        public override RectangleF GetSourceRect(RenderPass pass, World world, CubePosition pos, MeshHelper.CubeFace face)
        {
            if (world == null)
                return base.GetSourceRect(pass, world, pos, face);

            //we're meshing one of the sides.
            if ((face & MeshHelper.CubeFace.SIDES) > 0)
            {
                //if the cube above is the same
                if (world.ChunkManager.GetCube(new CubePosition(pos.X, pos.Y + 1, pos.Z)).GetOrDefault(Main.Registry.CubeRegistry.Air) == this)
                {
                    //use the stone texture for the sides
                    return new RectangleF(16, 0, 16, 16);
                }
            }

            return base.GetSourceRect(pass, world, pos, face);
        }

        private Cube mushroomStem;
        private Cube mushroomTop;
        private Cube mushroomSmall;

        private CubePosition[] placeOffsets = new CubePosition[8] 
        {
            new CubePosition(1, 1, 0),
            new CubePosition(1, 1, 1),
            new CubePosition(0, 1, 1),
            new CubePosition(-1, 1, 0),
            new CubePosition(-1, 1, 1),
            new CubePosition(-1, 1, -1),
            new CubePosition(1, 1, -1),
            new CubePosition(0, 1, -1),
        };

        public override void PostChunkGen(World world, ChunkManager manager, ChunkData chunkData, CubePosition position)
        {
            base.PostChunkGen(world, manager, chunkData, position);

            if (mushroomStem == null)
            {
                mushroomStem = Main.Registry.CubeRegistry.Get("mushroom_stem");
                mushroomTop = Main.Registry.CubeRegistry.Get("mushroom_orange_top");
                mushroomSmall = Main.Registry.CubeRegistry.Get("mushroom_orange_small");
            }

            if (Main.random.NextFloat() < 1f / 30f)
                SpawnMushrooms(manager, position.InCubeSpace(chunkData.GetChunk()), false);
        }

        public override void OnRandomUpdate(World world, ChunkManager manager, ChunkData chunkData, CubePosition position)
        {
            base.OnRandomUpdate(world, manager, chunkData, position);

            if (mushroomStem == null)
            {
                mushroomStem = Main.Registry.CubeRegistry.Get("mushroom_stem");
                mushroomTop = Main.Registry.CubeRegistry.Get("mushroom_orange_top");
                mushroomSmall = Main.Registry.CubeRegistry.Get("mushroom_orange_small");
            }

            //Don't try to spawn a mushroom most of the time
            if (Main.random.NextFloat() < 0.05f)
                SpawnMushrooms(manager, position, true);
        }

        private void SpawnMushrooms(ChunkManager manager, CubePosition position, bool restrictBase)
        {
            //check the block above to see if we can place a mushroom or other block there
            CubePosition abovePosition = position + new CubePosition(0, 1, 0, CubePosition.CoordinateSpace.CubeSpace);
            if (ChunkHelper.CanPlaceIfNonSolid(manager, abovePosition, out Chunk offsetChunk, out Cube offsetCube))
            {
                bool smallMushroom = Main.random.NextCoinFlip();

                if (smallMushroom)
                {
                    offsetChunk.GetData().SetCube(abovePosition, mushroomSmall.Id);
                }
                else
                {
                    int size = Main.random.Next(3, 8);
                    bool canPlaceBigMushroom = true;

                    if (restrictBase)
                    {
                        for (int i = 0; i < placeOffsets.Length; i++)
                        {
                            //First, check the surrounding blocks just above to see if we can place a mushroom.
                            CubePosition offsetPosition = position + placeOffsets[i];
                            if (!ChunkHelper.CanPlaceIfNonSolid(manager, offsetPosition, out Chunk a, out Cube b))
                            {
                                canPlaceBigMushroom = false;
                                break;
                            }
                        }
                    }

                    if (canPlaceBigMushroom)
                    {
                        for (int i = 1; i < size + 1; i++)
                        {
                            CubePosition offsetPosition = position + new CubePosition(0, i, 0);
                            if (!ChunkHelper.CanPlaceIfNonSolid(manager, offsetPosition, out Chunk a, out Cube b))
                            {
                                canPlaceBigMushroom = false;
                                break;
                            }
                        }

                    }

                    if (canPlaceBigMushroom)
                    {
                        for (int i = 0; i < placeOffsets.Length; i++)
                        {
                            CubePosition offsetPosition = position + placeOffsets[i] + new CubePosition(0, size, 0);
                            if (!ChunkHelper.CanPlaceIfNonSolid(manager, offsetPosition, out Chunk a, out Cube b))
                            {
                                canPlaceBigMushroom = false;
                                break;
                            }
                        }
                    }

                    if (canPlaceBigMushroom)
                    {
                        for (int i = 1; i < size + 1; i++)
                        {
                            CubePosition offsetPosition = position + new CubePosition(0, i, 0);

                            if (i < size)
                                manager.GetChunk(offsetPosition).GetData().SetCube(offsetPosition, mushroomStem.Id);
                            else manager.GetChunk(offsetPosition).GetData().SetCube(offsetPosition, mushroomTop.Id);
                        }

                        for (int i = 0; i < placeOffsets.Length; i++)
                        {
                            CubePosition offsetPosition = position + placeOffsets[i] + new CubePosition(0, size - 1, 0);

                            manager.GetChunk(offsetPosition).GetData().SetCube(offsetPosition, mushroomTop.Id);
                        }
                    }
                }
            }
        }
    }
}
