using BrUtility;
using Engine.ChunkStuff;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.ChunkStuff;
using ViMG.GameStates;
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

        public override RectangleF GetSourceRect(RenderPass pass, CopiedChunkManager.CopiedChunkData data, ChunkRenderMesher.CubeMeshingParameters parameters, MeshHelper.CubeFace face)
        {
            //we're meshing one of the sides.
            if ((face & MeshHelper.CubeFace.SIDES) > 0)
            {
                //if the cube above is the same
                if (data.GetCube(parameters.position + new CubePosition(0, 1, 0, CubePosition.CoordinateSpace.ChunkSpace)).GetOrDefault(Main.Registry.CubeRegistry.Air) == this)
                {
                    //use the stone texture for the sides
                    return new RectangleF(16, 0, 16, 16);
                }
            }

            return base.GetSourceRect(pass, data, parameters, face);
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

        public override void PostChunkGen(WorldPrototype world, CubePosition position)
        {
            base.PostChunkGen(world, position);

            if (mushroomStem == null)
            {
                mushroomStem = Main.Registry.CubeRegistry.Get("mushroom_stem");
                mushroomTop = Main.Registry.CubeRegistry.Get("mushroom_orange_top");
                mushroomSmall = Main.Registry.CubeRegistry.Get("mushroom_orange_small");
            }

            if (Main.random.NextFloat() < 1f / 30f)
                SpawnMushrooms(world.ChunkManager, position, false);
        }

        public override void OnRandomUpdate(World world, ChunkManager manager, CubePosition position)
        {
            base.OnRandomUpdate(world, manager, position);

            if (mushroomStem == null)
            {
                mushroomStem = Main.Registry.CubeRegistry.Get("mushroom_stem");
                mushroomTop = Main.Registry.CubeRegistry.Get("mushroom_orange_top");
                mushroomSmall = Main.Registry.CubeRegistry.Get("mushroom_orange_small");
            }

            //Don't try to spawn a mushroom most of the time
            if (Main.random.NextFloat() < 0.05f)
                SpawnMushrooms(world.ChunkManager, position, true);
        }

        private void SpawnMushrooms(ChunkManager manager, CubePosition position, bool restrictBase)
        {
            //check the block above to see if we can place a mushroom or other block there
            CubePosition abovePosition = position + new CubePosition(0, 1, 0, CubePosition.CoordinateSpace.CubeSpace);
            if (ChunkHelper.CanPlaceIfNonSolid(manager, abovePosition, out Cube offsetCube))
            {
                bool smallMushroom = Main.random.NextCoinFlip();

                if (smallMushroom)
                {
                    manager.CubeView.SetCube(abovePosition, mushroomSmall.Id);
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
                            if (!ChunkHelper.CanPlaceIfNonSolid(manager, offsetPosition, out Cube b))
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
                            if (!ChunkHelper.CanPlaceIfNonSolid(manager, offsetPosition, out Cube b))
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
                            if (!ChunkHelper.CanPlaceIfNonSolid(manager, offsetPosition, out Cube b))
                            {
                                canPlaceBigMushroom = false;
                                break;
                            }
                        }
                    }

                    if (canPlaceBigMushroom)
                    {
                        int len = size + placeOffsets.Length;
                        Span<CubePosition> positions = stackalloc CubePosition[len];
                        Span<ushort> ids = stackalloc ushort[len];
                        int mi = 0;

                        for (int i = 1; i < size + 1; i++)
                        {
                            CubePosition offsetPosition = position + new CubePosition(0, i, 0);

                            if (i < size)
                            {
                                positions[mi] = offsetPosition;
                                ids[mi] = mushroomStem.Id;
                                mi++;
                            }
                            //manager.SetCube(offsetPosition, mushroomStem.Id);
                            else
                            {
                                positions[mi] = offsetPosition;
                                ids[mi] = mushroomTop.Id;
                                mi++;
                            } 
                                //manager.SetCube(offsetPosition, mushroomTop.Id);
                        }

                        for (int i = 0; i < placeOffsets.Length; i++)
                        {
                            CubePosition offsetPosition = position + placeOffsets[i] + new CubePosition(0, size - 1, 0);

                            positions[mi] = offsetPosition;
                            ids[mi] = mushroomTop.Id;
                            mi++;
                            //manager.SetCube(offsetPosition, mushroomTop.Id);
                        }

                        manager.CubeView.SetCubes(positions, ids);
                    }
                }
            }
        }
    }
}
