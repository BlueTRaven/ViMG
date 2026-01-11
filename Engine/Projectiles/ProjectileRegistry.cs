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
    }

    public class ProjectilePresetGeneric : ProjectilePreset
    {
        private readonly ProjectileManager.ProjectileVisStats visStats;

        public ProjectilePresetGeneric(string identifier, ProjectileManager.ProjectileVisStats visStats) : base(identifier)
        {
            this.visStats = visStats;
        }

        public override ProjectileManager.ProjectileVisStats VisStats()
        {
            return visStats;
        }
    }

    public class ProjectileRegistry : ObjRegistry<ProjectilePreset>
    {
        public override void Register(ProjectilePreset obj)
        {
            obj.Id = this.Count;
            base.Register(obj);
        }
    }
}
