using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;

namespace ViMG.Entities
{
    //For cubes that don't want a fully-fledged cube entity, but want a light.
    [EntitySerializable(EntitySerializableAttribute.SerializationType.All)]
    [EntityMeta(0, 0)]
    public class CubeLight : Entity, ICubeTracker
    {
        private CubePosition trackedPosition;
        public CubePosition TrackedPosition => trackedPosition;

        private Vector4 lightColor;
        private Vector2 lightExtents;
        private int light = -1;

        public CubeLight()
        {
        }

        public CubeLight(CubePosition position, Vector4 lightColor, Vector2 lightExtents)
        {
            this.trackedPosition = position;
            Position = position.InWorldSpace() + new Vector3(Cube.CUBE_SCALE / 2);

            this.lightColor = lightColor;
            this.lightExtents = lightExtents;
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

            light = world.LightManager.Add(Position, lightExtents.X, lightExtents.Y, lightColor);
        }

        public bool OnInteract(Player player)
        {
            return false;
        }

        public void TrackingCubeUpdated(World world, ChunkManager2 manager, ushort updatedId)
        {
            world.LightManager.Remove(light);
            world.EntityManager.Remove(this);
        }

        public override void OnSave(List<byte> saveBytes)
        {
            base.OnSave(saveBytes);

            SaveHelper.SaveCubePosition(saveBytes, TrackedPosition);

            SaveHelper.SaveVector4(saveBytes, lightColor);
            SaveHelper.SaveVector2(saveBytes, lightExtents);
        }

        public override void OnLoad(byte[] loadBytes, in int version)
        {
            base.OnLoad(loadBytes, version);

            int index = 0;
            trackedPosition = SaveHelper.LoadCubePosition(loadBytes, ref index);
            Position = TrackedPosition.InWorldSpace() + new Vector3(Cube.CUBE_SCALE / 2);

            lightColor = SaveHelper.LoadVector4(loadBytes, ref index);
            lightExtents = SaveHelper.LoadVector2(loadBytes, ref index);
        }
    }
}
