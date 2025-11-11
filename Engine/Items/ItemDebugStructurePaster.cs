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
using ViMG.Rendering;

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

        public ItemDebugStructurePaster() : base("DEBUGStructurePaster", new RectangleF(112, 112, 16, 16))
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

                //TODO check
                //this may or may not work
                int pi = 0;
                Span<CubePosition> positions = stackalloc CubePosition[pasted.structure.size.X * pasted.structure.size.Y * pasted.structure.size.Z];

                for (int x = 0; x < pasted.structure.size.X; x++)
                {
                    for (int y = 0; y < pasted.structure.size.Y; y++)
                    {
                        for (int z = 0; z < pasted.structure.size.Z; z++)
                        {
                            positions[pi] = new CubePosition(pasted.createdAt.X + x, pasted.createdAt.Y + y, pasted.createdAt.Z + z, pasted.createdAt.Coord);
                            pi++;
                        }
                    }
                }

                player.world.ChunkManager.CubeView.SetCubes(positions, pasted.original.AsSpan());
            }

            Main.DEBUGPopupText = "Currently Selected Structure:\n" + assetKeysList[currentStructure] + ".\n" +
                "Press , to cycle LEFT and . to cycle RIGHT.\n\n\n\n" +
                "Press rmb to place the structure. This can then be undone with ctrl+z. Note that undoing doesn't guarantee that tile entities will be re-created if they were destroyed!";
        }

        public override bool RightClick(Player player, Inventory inventory, int index, Vector3 facing, out ActionStats actionStats)
        {
            CubePosition pos = CubePosition.FromWorldSpace(player.Position);
            Structure structure = Main.assetsManager.GetAsset<Structure>(assetKeysList[currentStructure]);
            PastedStructure pasted = new PastedStructure() 
            {
                structure = structure, 
                original = new ushort[structure.size.X * structure.size.Y * structure.size.Z],
                createdAt = pos,
            };

            //TODO check
            //this may or may not work
            //Fill in original with GetIds
            int pi = 0;
            Span<CubePosition> positions = stackalloc CubePosition[pasted.original.Length];
            for (int x = 0; x < structure.size.X; x++)
            {
                for (int y = 0; y < structure.size.Y; y++)
                {
                    for (int z = 0; z < structure.size.Z; z++)
                    {
                        positions[pi] = new CubePosition(pos.X + x, pos.Y + y, pos.Z + z, pos.Coord);
                        pi++;
                    }
                }
            }

            player.world.ChunkManager.CubeView.GetIds(positions, pasted.original.AsSpan());
            player.world.ChunkManager.CubeView.SetCubes(positions, structure.data.AsSpan());

            pastedStructures.Push(pasted);

            actionStats = new ActionStats(3);

            return true;
        }
    }
}
