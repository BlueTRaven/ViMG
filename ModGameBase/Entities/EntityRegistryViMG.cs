using Engine.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Entities;

namespace ModGameBase.Entities
{
    public class EntityRegistryViMG : EntityRegistry
    {
        public EntityRegistryViMG()
        {
        }

        protected override void DoRegistration()
        {
            Register(EntityType.New<AncientAltar>());
            Register(EntityType.New<CaveSalamander>());
            Register(EntityType.New<CubeLight>());
            Register(EntityType.New<CubeTimer>());
            Register(EntityType.New<Cultist>());
            Register(EntityType.New<Door>());
            Register(EntityType.New<Ducken>());
            Register(EntityType.New<EntityAnvilIron>());
            Register(EntityType.New<EntityCaveCompass>());
            Register(EntityType.New<EntityCaveRoot>());
            Register(EntityType.New<EntityChest>());
            Register(EntityType.New<EntityCubeBonfire>());
            Register(EntityType.New<EntityCubeFlame>());
            Register(EntityType.New<EntityFurnace>());
            Register(EntityType.New<EntityLeviathan>());
            Register(EntityType.New<EntityShrine>());
            Register(EntityType.New<EntitySonarTracker>());
            Register(EntityType.New<Ghost>());
            Register(EntityType.New<Ghoul>());
            Register(EntityType.New<GlowNode>());
            Register(EntityType.New<Heart>());
            Register(EntityType.New<Imp>());
            Register(EntityType.New<Lightning>());
            Register(EntityType.New<LightStressTest>());
            Register(EntityType.New<ManaStar>());
            Register(EntityType.New<PhysicsTestBall>());
            Register(EntityType.New<PlayerBubble>());
            Register(EntityType.New<Sapling>());
            Register(EntityType.New<Skeleton>());
            Register(EntityType.New<Skeleton2>());
            Register(EntityType.New<SkeletonBonePile>());
            Register(EntityType.New<Skullhead>());
            Register(EntityType.New<SkullheadEye>());
            Register(EntityType.New<Slime>());
            Register(EntityType.New<SlimeBig>());
            Register(EntityType.New<Snake>());
            Register(EntityType.New<SnakeFlying>());
            Register(EntityType.New<TestNPC>());
            Register(EntityType.New<Tree>());
            Register(EntityType.New<Worm>());
        }
    }
}
