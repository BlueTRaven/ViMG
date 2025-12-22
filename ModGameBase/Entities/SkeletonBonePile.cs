using BrUtility;
using Engine.Networking;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Buffs;
using ViMG.Cubes;
using ViMG.Entities;

namespace ModGameBase.Entities
{
    public class SkeletonBonePile : Entity, IHasStats
    {
        private CubePosition? trackBoneBlockPosition;
        private float resurrectTimer;
        private float checkTimer;
        private BuffManager buffManager;
        private AiImmobile ai;

        public SkeletonBonePile()
        {
        }

        public SkeletonBonePile(Vector3 position)
        {
            Position = position;
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

            resurrectTimer = this.random.NextFloat(15, 25);
            buffManager = new BuffManager(this);

            ai = new AiImmobile(new Rectangle3D(-new Vector3(Cube.CUBE_SCALE * 0.35f), new Vector3(Cube.CUBE_SCALE * 0.70f)), buffManager, 2);
            SearchForNearbyBoneBlocks();
        }

        public override void OnKill()
        {
            base.OnKill();

            EntityItem ent = new EntityItem(Position,
            new Vector3(Main.random.NextFloat(-Cube.CUBE_SCALE * 5, Cube.CUBE_SCALE * 5), Cube.CUBE_SCALE * 6.4f,
                    Main.random.NextFloat(-Cube.CUBE_SCALE * 5, Cube.CUBE_SCALE * 5)),
            new ViMG.Items.ItemInstance(Main.Registry.ItemRegistry.Get("brittle_bone"), 1, 1));
            world.EntityManager.Add(ent);
        }

        public override void OnUnload()
        {
            base.OnUnload();

            var funcs = new AiImmobile.Funcs<SkeletonBonePile> { ai = ai, entity = this };
            funcs.OnUnload();
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            var funcs = new AiImmobile.Funcs<SkeletonBonePile> { ai = ai, entity = this };
            funcs.Update(deltaTime);

            if (trackBoneBlockPosition.HasValue)
            {
                //ai.InvulnTimer = 0.25f;
            }

            if (resurrectTimer <= 0 && !Dead) 
            {
                world.EntityManager.Add(new Skeleton2(Position));
                world.EntityManager.Unload(this);
                return;
            }
            if (checkTimer <= 0)
            {
                SearchForNearbyBoneBlocks();
                checkTimer = 1;
            }

            resurrectTimer -= (float)deltaTime;
            checkTimer -= (float)deltaTime;
        }

        public Stats GetStats()
        {
            return new Stats()
            {
                HP = ai.Health,
                MaximumHP = ai.MaxHealth,
            };
        }

        public void SetStats(Stats stats)
        {
            ai.Health = stats.HP;
            ai.MaxHealth = stats.MaximumHP;
        }

        public override void OnSave(List<byte> saveBytes)
        {
            base.OnSave(saveBytes);

            Get(out var state);
            state.OnSave(saveBytes);

            ai?.OnSave(saveBytes);
        }

        public override void OnLoad(World world, byte[] loadBytes, in int version)
        {
            base.OnLoad(world, loadBytes, version);

            int index = 0;
            var bs = new BasicState();
            bs.OnLoad(loadBytes, ref index);
            Set(ref bs);

            ai?.OnLoad(loadBytes, ref index);
        }

        public void Get(out BasicState state)
        {
            BasicState aiState = new BasicState();
            ai?.Get(out aiState);
            aiState.position = Position;
            aiState.rotation = Quaternion.Identity;
            state = aiState;
        }

        public void Set(ref readonly BasicState state)
        {
            Position = state.position;

            ai?.Set(in state);
        }

        private bool SearchForNearbyBoneBlocks()
        {
            Cube boneBlock = Main.Registry.CubeRegistry.Get("brittle_bone_block");

            const int searchRadius = 4;

            trackBoneBlockPosition = null;
            for (int x = -searchRadius; x <= searchRadius; x++)
            {
                for (int y = -searchRadius; y <= searchRadius; y++)
                {
                    for (int z = -searchRadius; z <= searchRadius; z++)
                    {
                        CubePosition checkPos = CubePosition.FromWorldSpace(Position) + new CubePosition(x, y, z, CubePosition.CoordinateSpace.CubeSpace);

                        if (world.ChunkManager.CubeView.GetCube(checkPos).GetOrDefault(Main.Registry.CubeRegistry.Air) == boneBlock)
                        {
                            trackBoneBlockPosition = checkPos;

                            return true;
                        }
                    }
                }
            }

            return false;
        }

    }
}
