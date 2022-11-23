using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Entities
{
    //A cube tracker that has a set timer. Once the timer expires, it deletes itself and the tracked cube.
    [EntitySerializable(EntitySerializableAttribute.SerializationType.All)]
    [EntityMeta(0)]
    public class CubeTimer : Entity, ICubeTracker
    {
        private ushort setTo;
        private float time;
        private float timer;
        public CubePosition TrackedPosition { get; private set; }

        public CubeTimer()
        {
        }

        public CubeTimer(CubePosition position, ushort setTo, float timer)
        {
            this.Position = position.InWorldSpace(null);
            TrackedPosition = position;

            this.setTo = setTo;

            this.time = timer;
            this.timer = timer;
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            timer -= (float)deltaTime;

            if (timer <= 0)
            {
                world.EntityManager.Remove(this);

                world.ChunkManager2.SetCube(TrackedPosition, setTo);
            }
        }

        public bool OnInteract(Player player)
        {
            return false;
        }

        public void TrackingCubeDestroyed(World world, ChunkManager cm)
        {
            world.EntityManager.Remove(this);
        }

        public override void OnSave(List<byte> saveBytes)
        {
            base.OnSave(saveBytes);

            SaveHelper.SaveCubePosition(saveBytes, TrackedPosition);
            SaveHelper.SaveFloat32(saveBytes, timer);
            SaveHelper.SaveFloat32(saveBytes, time);

            SaveHelper.SaveUInt16(saveBytes, setTo);
        }

        public override void OnLoad(byte[] loadBytes, in int version)
        {
            base.OnLoad(loadBytes, version);

            int index = 0;
            TrackedPosition = SaveHelper.LoadCubePosition(loadBytes, ref index);
            timer = SaveHelper.LoadFloat32(loadBytes, ref index);
            time = SaveHelper.LoadFloat32(loadBytes, ref index);

            setTo = SaveHelper.LoadUInt16(loadBytes, ref index);
        }
    }
}
