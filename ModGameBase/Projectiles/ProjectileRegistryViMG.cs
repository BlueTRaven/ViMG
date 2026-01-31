using BrUtility;
using Engine;
using Engine.Projectiles;
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
using ViMG.Items;
using static ViMG.Items.Item;

namespace ModGameBase.Projectiles
{
    public class ProjectileRegistryViMG : ProjectileRegistry
    {
        protected override void DoRegistration()
        {
            base.DoRegistration();

            Register(new ProjectilePresetGeneric("imp_fireball", new ProjectileManager.ProjectileVisStats(
                                new RectangleF(32, 0, 16, 16), Cube.CUBE_SCALE,
                                Color.Red.ToVector4(), new Vector2(Cube.CUBE_SCALE * 2, Cube.CUBE_SCALE * 4)),
                                new()
                                {
                                    group = HitboxManager.Group.ENEMYHOSTILE_BOTH,
                                    damage = 1,
                                    knockback = 1,
                                    collisionRadius = Cube.CUBE_SCALE / 4f,
                                    size = Cube.CUBE_SCALE,
                                    dieOnCollision = true,
                                }));

            Register(new ProjectilePresetGeneric("skullhead_skull", new ProjectileManager.ProjectileVisStats(new RectangleF(0, 48, 32, 32), Cube.CUBE_SCALE),
                new()
                {
                    group = HitboxManager.Group.ENEMYHOSTILE_DEAL,
                    damage = 3,
                    knockback = 1f,
                    collisionRadius = Cube.CUBE_SCALE / 4f,
                    size = Cube.CUBE_SCALE,
                }));
            Register(new ProjectilePresetGeneric("cultist_ball", new ProjectileManager.ProjectileVisStats(new RectangleF(32, 0, 16, 16), Cube.CUBE_SCALE,
                Color.Red.ToVector4(), new Vector2(Cube.CUBE_SCALE * 2, Cube.CUBE_SCALE * 4)),
                new()
                {
                    damage = 1,
                    knockback = 0.25f,
                    pierce = 1,
                    gravityScale = 1,
                    dieOnCollision = true,
                    gravity = true,

                    group = HitboxManager.Group.ENEMYHOSTILE_BOTH,
                    collisionRadius = Cube.CUBE_SCALE / 4f,
                    size = Cube.CUBE_SCALE,
                }));
            Register(new ProjectilePresetGeneric("arrow", new ProjectileManager.ProjectileVisStats(new RectangleF(16, 0, 16, 16), Cube.CUBE_SCALE),
                new()
                {
                    group = HitboxManager.Group.PLAYER_DEAL,
                    damage = 1,
                    knockback = 1f,
                    collisionRadius = Cube.CUBE_SCALE / 8f,
                    size = Cube.CUBE_SCALE,
                    dieOnCollision = true,
                    gravity = true,
                    gravityScale = 1f,
                }));
            Register(new ProjectilePresetGeneric("musketball", new ProjectileManager.ProjectileVisStats(new RectangleF(16, 0, 16, 16), Cube.CUBE_SCALE),
                new()
                {
                    group = HitboxManager.Group.PLAYER_DEAL,
                    damage = 1,
                    knockback = 1f,
                    collisionRadius = Cube.CUBE_SCALE * 0.25f,
                    size = Cube.CUBE_SCALE,
                    dieOnCollision = true,
                }));
            Register(new ProjectilePresetGeneric("shard", new ProjectileManager.ProjectileVisStats(new RectangleF(0, 16, 16, 16), Cube.CUBE_SCALE),
                new()
                {
                    group = HitboxManager.Group.ENEMYHOSTILE_BOTH,
                    damage = 1,
                    knockback = 1f,
                    collisionRadius = Cube.CUBE_SCALE / 8,
                    size = Cube.CUBE_SCALE,
                    gravity = true,
                    gravityScale = 0.5f,
                    dieOnCollision = true,
                }));
            Register(new ProjectilePresetGeneric("bone", new ProjectileManager.ProjectileVisStats(new RectangleF(48, 0, 16, 16), Cube.CUBE_SCALE / 3f),
                new()
                {
                    damage = 1,
                    knockback = 0.25f,
                    pierce = 1,
                    gravityScale = 1,
                    dieOnCollision = true,
                    gravity = true,

                    group = HitboxManager.Group.ENEMYHOSTILE_BOTH,
                    collisionRadius = Cube.CUBE_SCALE / 4f,
                    size = Cube.CUBE_SCALE,
                }));
            Register(new ProjectilePresetGeneric("bone_staff", new ProjectileManager.ProjectileVisStats(new RectangleF(80, 128, 32, 32), Cube.CUBE_SCALE),
                new()
                {
                    group = HitboxManager.Group.PLAYER_DEAL,
                    damage = 1,
                    knockback = 1f,
                    collisionRadius = Cube.CUBE_SCALE / 2f,
                    size = Cube.CUBE_SCALE,
                }));
            Register(new ProjectilePresetGeneric("lava", new ProjectileManager.ProjectileVisStats(new RectangleF(48, 16, 16, 16), Cube.CUBE_SCALE),
                new()
                {
                    group = HitboxManager.Group.PLAYER_DEAL,
                    damage = 1,
                    knockback = 1f,
                    collisionRadius = Cube.CUBE_SCALE / 4f,
                    size = Cube.CUBE_SCALE,

                    gravity = true,
                    dieOnCollision = true,
                }));
            // TODO effects will be null here. ProjectileRegistry will be run before ItemRegistry...
            Register(new ProjectilePresetGeneric("lava_cannon", new ProjectileManager.ProjectileVisStats(new RectangleF(32, 16, 16, 16), Cube.CUBE_SCALE),
                new()
                {
                    group = HitboxManager.Group.PLAYER_DEAL,
                    damage = 2,
                    knockback = 1,
                    collisionRadius = Cube.CUBE_SCALE / 4f,
                    size = Cube.CUBE_SCALE,

                    dieOnCollision = true,

                    effects = GlobalState.Registry.ItemRegistry.Get("cannon_lavacrystal") as ItemLavaCannon
                }));
        }
    }
}
