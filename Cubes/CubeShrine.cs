using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Buffs;
using ViMG.ChunkStuff;
using ViMG.Entities;
using ViMG.GameStates;
using ViMG.Items;

namespace ViMG.Cubes
{
    public class CubeShrine : Cube
    {
        private readonly string buff;

        public CubeShrine(string identifier, RectangleF sourceRect, string buff, string name = "", string description = "") : base(identifier, sourceRect, Color.White, 999)
        {
            this.buff = buff;

            this.Name = name;
            this.Description = description;
        }

        public override void PostChunkGen(WorldPrototype world, CubePosition position)
        {
            base.PostChunkGen(world, position);

            EntityShrine shrine = new EntityShrine(position, Main.Registry.BuffRegistry.Get(buff));
            world.AddEntity(shrine);
        }

        public override void OnPlayerPlaced(Player player, CubePosition position)
        {
            base.OnPlayerPlaced(player, position);

            EntityShrine shrine = new EntityShrine(position, Main.Registry.BuffRegistry.Get(buff));
            player.GetWorld().EntityManager.Add(shrine);
        }

        public override RectangleF GetSourceRect(RenderPass pass, CopiedChunkData data, ChunkMesher.CubeMeshingParameters parameters)
        {
            if (data.GetValid())
            {
                var meshingData = data.GetEntityMeshingData<EntityShrine.MeshingData>(parameters.position);
             
                if (meshingData.cooldownTimer > 0)
                {
                    RectangleF sourceRect = base.GetSourceRect(pass, data, parameters);
                    sourceRect.y += 16;
                    return sourceRect;
                }
            }

            return base.GetSourceRect(pass, data, parameters);
        }

        public override void GetDrops(List<ItemInstance> itemsToDrop)
        {
            base.GetDrops(itemsToDrop);

            DropSelf(itemsToDrop);
        }
    }
}
