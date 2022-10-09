using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Generation;

namespace ViMG.Items
{
    public class ItemDebugStructurePaster : Item
    {
        private class PastedStructure
        {
            public Structure structure;
            public ushort[] original;

            public CubePosition createdAt;
        }

        private static Stack<PastedStructure> pastedStructures = new Stack<PastedStructure>();

        private static int currentStructure;
        private static string[] assetKeysList;

        public ItemDebugStructurePaster() : base("DEBUGStructurePaster", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(112, 112, 16, 16))
        {
            name = "DEBUG Structure Paster";
            description = "Allows you to paste structures, as they are defined in their structure files, into the world.";
            assetKeysList = Main.assetsManager.GetAssetKeysList<Structure>().ToArray();
        }

        public override void Hold(Player player, Inventory inventory, int index)
        {
            base.Hold(player, inventory, index);

            if (Main.inputManager.JustPressed(Keys.OemComma))
            {
                currentStructure--;

                if (currentStructure < 0)
                    currentStructure = assetKeysList.Length - 1;
            }

            if (Main.inputManager.JustPressed(Keys.OemPeriod))
            {
                currentStructure++;

                if (currentStructure >= assetKeysList.Length)
                    currentStructure = 0;
            }

            if (Main.inputManager.IsHeld(Keys.LeftControl) && Main.inputManager.JustPressed(Keys.Z) && pastedStructures.Count > 0)
            {
                PastedStructure pasted = pastedStructures.Pop();

                for (int x = 0; x < pasted.structure.size.X; x++)
                {
                    for (int y = 0; y < pasted.structure.size.Y; y++)
                    {
                        for (int z = 0; z < pasted.structure.size.Z; z++)
                        {
                            Util.ThreeDToOneD(new ValuePoint3D(x, y, z), new ValuePoint3D(pasted.structure.size.X, pasted.structure.size.Y, pasted.structure.size.Z), out int i);
                            CubePosition realPos = new CubePosition(pasted.createdAt.X + x, pasted.createdAt.Y + y, pasted.createdAt.Z + z, pasted.createdAt.Coord);

                            Chunk chunk = player.GetWorld().GetChunkManager().GetChunk(realPos);

                            //We still want this to work if we move too far away and unload it
                            //TODO this crashes the game if an entity was saved anywhere in this area
                            if (chunk == null)
                            {
                                player.GetWorld().ChunkLoadManager.LoadChunk(player.GetWorld(), ChunkPosition.CubeChunk(realPos));

                                chunk = player.GetWorld().GetChunkManager().GetChunk(realPos);
                            }

                            if (chunk != null)
                            {
                                Cube cube = chunk.GetData().GetCube(realPos).GetOrDefault(Main.Registry.CubeRegistry.Air);

                                chunk.GetData().SetCube(realPos.InChunkSpace(chunk), pasted.original[i]);
                            }
                        }
                    }
                }
            }

            Main.DEBUGPopupText = "Currently Selected Structure:\n" + assetKeysList[currentStructure] + ".\n" +
                "Press , to cycle LEFT and . to cycle RIGHT.\n\n\n\n" +
                "Press rmb to place the structure. This can then be undone with ctrl+z. Note that undoing doesn't guarantee that tile entities will be re-created if they were destroyed!";
        }

        public override bool RightClick(Player player, Inventory inventory, int index, Vector3 facing, out float itemCooldownTime)
        {
            CubePosition pos = CubePosition.FromWorldSpace(player.Position);
            Structure structure = Main.assetsManager.GetAsset<Structure>(assetKeysList[currentStructure]);
            PastedStructure pasted = new PastedStructure() 
            {
                structure = structure, 
                original = new ushort[structure.size.X * structure.size.Y * structure.size.Z],
                createdAt = pos,
            };

            for (int x = 0; x < structure.size.X; x++)
            {
                for (int y = 0; y < structure.size.Y; y++)
                {
                    for (int z = 0; z < structure.size.Z; z++)
                    {
                        Util.ThreeDToOneD(new ValuePoint3D(x, y, z), new ValuePoint3D(structure.size.X, structure.size.Y, structure.size.Z), out int i);
                        CubePosition realPos = new CubePosition(pos.X + x, pos.Y + y, pos.Z + z, pos.Coord);

                        Chunk chunk = player.GetWorld().GetChunkManager().GetChunk(realPos);

                        if (chunk != null)
                        {
                            Cube cube = chunk.GetData().GetCube(realPos).GetOrDefault(Main.Registry.CubeRegistry.Air);

                            pasted.original[i] = cube.Id;
                            chunk.GetData().SetCube(realPos.InChunkSpace(chunk), structure.data[i]);
                        }
                    }
                }
            }

            pastedStructures.Push(pasted);

            itemCooldownTime = 3f;

            return true;
        }
    }
}
