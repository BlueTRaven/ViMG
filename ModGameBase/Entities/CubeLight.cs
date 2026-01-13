using Engine.Networking;
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
    public class CubeLight : Entity, ICubeTracker, ISyncBasicState
    {
        private CubePosition trackedPosition;
        public CubePosition TrackedPosition => trackedPosition;

        private Vector4 lightColor;
        private Vector2 lightExtents;

        public CubeLight()
        {
            DoesSync = false;
            DoesMajorSync = false;
        }

        public CubeLight(CubePosition position, Vector4 lightColor, Vector2 lightExtents)
        {
            DoesSync = false;
            DoesMajorSync = false;

            this.trackedPosition = position;
            Position = position.InWorldSpace() + new Vector3(Cube.CUBE_SCALE / 2);

            this.lightColor = lightColor;
            this.lightExtents = lightExtents;
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            world.LightManager2.AddShadowmapped(new Engine.Common.LightManager2.LightConfig
            {
                position = Position,
                min = lightExtents.X,
                max = lightExtents.Y,
                color = lightColor,
            });
        }

        public bool OnInteract(Player player)
        {
            return false;
        }

        public void TrackingCubeUpdated(World world, ChunkManager manager, Player? player, ushort updatedId)
        {
            world.EntityManager.Kill(this);
        }

        public override void OnSave(List<byte> saveBytes)
        {
            base.OnSave(saveBytes);

            SaveHelper.SaveCubePosition(saveBytes, TrackedPosition);

            SaveHelper.SaveVector4(saveBytes, lightColor);
            SaveHelper.SaveVector2(saveBytes, lightExtents);
        }

        public override void OnLoad(World world, byte[] loadBytes, in int version)
        {
            base.OnLoad(world, loadBytes, version);

            int index = 0;
            trackedPosition = SaveHelper.LoadCubePosition(loadBytes, ref index);
            Position = TrackedPosition.InWorldSpace() + new Vector3(Cube.CUBE_SCALE / 2);

            lightColor = SaveHelper.LoadVector4(loadBytes, ref index);
            lightExtents = SaveHelper.LoadVector2(loadBytes, ref index);
        }

        public void Get(out BasicState state)
        {
            state = new BasicState
            {
                position = Position,
            };
        }

        public void Set(ref readonly BasicState state)
        {
            Position = state.position;
        }
    }
}
