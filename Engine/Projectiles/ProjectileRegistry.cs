using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Entities;

namespace Engine.Projectiles
{
    public abstract class ProjectilePreset : IRegisterable
    {
        public string Identifier { get; private set; }
        public int Id;

        public ProjectilePreset(string identifier)
        {
            Identifier = identifier;
        }

        public abstract ProjectileManager.ProjectileVisStats VisStats();

        public abstract ProjectileManager.ProjectileStats DefaultStats();
    }

    public class ProjectilePresetGeneric : ProjectilePreset
    {
        private readonly ProjectileManager.ProjectileVisStats visStats;
        private readonly ProjectileManager.ProjectileStats defaultStats;

        public ProjectilePresetGeneric(string identifier, ProjectileManager.ProjectileVisStats visStats, ProjectileManager.ProjectileStats defaultStats) : base(identifier)
        {
            this.visStats = visStats;
            this.defaultStats = defaultStats;
        }

        public override ProjectileManager.ProjectileVisStats VisStats()
        {
            return visStats;
        }

        public override ProjectileManager.ProjectileStats DefaultStats()
        {
            return defaultStats;
        }
    }

    public class ProjectileRegistry : ObjRegistry<ProjectilePreset>
    {
        public override void Register(ProjectilePreset obj)
        {
            obj.Id = Count + 1;
            base.Register(obj);
        }
    }
}
