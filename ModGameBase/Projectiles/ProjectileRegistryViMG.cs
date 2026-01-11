using BrUtility;
using Engine.Projectiles;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Entities;

namespace ModGameBase.Projectiles
{
    public class ProjectileRegistryViMG : ProjectileRegistry
    {
        protected override void DoRegistration()
        {
            base.DoRegistration();

            Register(new ProjectilePresetGeneric("imp_fireball", new ProjectileManager.ProjectileVisStats(
                                new RectangleF(32, 0, 16, 16), Cube.CUBE_SCALE,
                                Color.Red.ToVector4(), new Vector2(Cube.CUBE_SCALE * 2, Cube.CUBE_SCALE * 4))));
            Register(new ProjectilePresetGeneric("skullhead_skull", new ProjectileManager.ProjectileVisStats(new RectangleF(0, 48, 32, 32), Cube.CUBE_SCALE)));
            Register(new ProjectilePresetGeneric("cultist_ball", new ProjectileManager.ProjectileVisStats(new RectangleF(32, 0, 16, 16), Cube.CUBE_SCALE,
                Color.Red.ToVector4(), new Vector2(Cube.CUBE_SCALE * 2, Cube.CUBE_SCALE * 4))));
            Register(new ProjectilePresetGeneric("arrow", new ProjectileManager.ProjectileVisStats(new RectangleF(16, 0, 16, 16), Cube.CUBE_SCALE)));
            Register(new ProjectilePresetGeneric("musketball", new ProjectileManager.ProjectileVisStats(new RectangleF(16, 0, 16, 16), Cube.CUBE_SCALE)));
            Register(new ProjectilePresetGeneric("shard", new ProjectileManager.ProjectileVisStats(new RectangleF(0, 16, 16, 16), Cube.CUBE_SCALE)));
            Register(new ProjectilePresetGeneric("bone", new ProjectileManager.ProjectileVisStats(new RectangleF(48, 0, 16, 16), Cube.CUBE_SCALE / 3f)));
            Register(new ProjectilePresetGeneric("bone_staff", new ProjectileManager.ProjectileVisStats(new RectangleF(80, 128, 32, 32), Cube.CUBE_SCALE)));
            Register(new ProjectilePresetGeneric("lava", new ProjectileManager.ProjectileVisStats(new RectangleF(48, 16, 16, 16), Cube.CUBE_SCALE)));
            Register(new ProjectilePresetGeneric("lava_cannon", new ProjectileManager.ProjectileVisStats(new RectangleF(32, 16, 16, 16), Cube.CUBE_SCALE)));
            
        }
    }
}
