using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;

namespace Engine.Networking
{
    public struct BasicState : INetSerializable
    {
        [Flags]
        private enum Fields : uint
        {
            None = 0,
            PosX = 1 << 0,
            PosY = 1 << 1,
            PosZ = 1 << 2,
            
            RotX = 1 << 3,
            RotY = 1 << 4,
            RotZ = 1 << 5,
            RotW = 1 << 6,
            
            VelX = 1 << 7,
            VelY = 1 << 8,
            VelZ = 1 << 9,

            Health = 1 << 10,
            State = 1 << 11,

            Timer0 = 1 << 12,
            Timer1 = 1 << 13,
            Timer2 = 1 << 14,
            Timer3 = 1 << 15,

            Counter0 = 1 << 16,
            Counter1 = 1 << 17,
            Counter2 = 1 << 18,
            Counter3 = 1 << 19,
        }

        private const int VERSION = 1;
        [System.Runtime.CompilerServices.InlineArray(4)]
        public struct Arr4F
        {
            private float _element0;

            public float this[int i]
            {
                get => this[i];
                set => this[i] = value;
            }
        }
        [System.Runtime.CompilerServices.InlineArray(4)]
        public struct Arr4I
        {
            private int _element0;

            public int this[int i]
            {
                get => this[i];
                set => this[i] = value;
            }
        }

        private int version;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public int health;
        public int state;
        public Arr4F timers;
        public Arr4I counters;

        public void Deserialize(NetDataReader reader)
        {
            version = reader.GetInt();
            position.X = reader.GetFloat();
            position.Y = reader.GetFloat();
            position.Z = reader.GetFloat();
            velocity.X = reader.GetFloat();
            velocity.Y = reader.GetFloat();
            velocity.Z = reader.GetFloat();
            rotation.X = reader.GetFloat();
            rotation.Y = reader.GetFloat();
            rotation.Z = reader.GetFloat();
            rotation.W = reader.GetFloat();
            health = reader.GetInt();
            state = reader.GetInt();

            var timersA = reader.GetArray<float>(sizeof(byte));
            for (int i = 0; i < 4; i++) timers[i] = timersA[i];
            var countersA = reader.GetArray<int>(sizeof(byte));
            for (int i = 0; i < 4; i++) counters[i] = countersA[i];
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(VERSION);
            writer.Put(position.X);
            writer.Put(position.Y);
            writer.Put(position.Z);
            writer.Put(velocity.X);
            writer.Put(velocity.Y);
            writer.Put(velocity.Z);
            writer.Put(rotation.X);
            writer.Put(rotation.Y);
            writer.Put(rotation.Z);
            writer.Put(rotation.W);
            writer.Put(health);
            writer.Put(state);

            Span<float> t = timers;
            writer.PutSpan(t);
            Span<int> i = counters;
            writer.PutSpan(i);
        }

        public void DeserializeDelta(NetDataReader reader)
        {
            version = reader.GetInt();
            Fields bits = (Fields)reader.GetUInt();

            if ((bits & Fields.PosX) == Fields.PosX)
                position.X = reader.GetFloat();
            if ((bits & Fields.PosY) == Fields.PosY)
                position.Y = reader.GetFloat();
            if ((bits & Fields.PosZ) == Fields.PosZ)
                position.Z = reader.GetFloat();

            if ((bits & Fields.VelX) == Fields.VelX)
                velocity.X = reader.GetFloat();
            if ((bits & Fields.VelY) == Fields.VelY)
                velocity.Y = reader.GetFloat();
            if ((bits & Fields.VelZ) == Fields.VelZ)
                velocity.Z = reader.GetFloat();

            if ((bits & Fields.RotX) == Fields.RotX)
                rotation.X = reader.GetFloat();
            if ((bits & Fields.RotY) == Fields.RotY)
                rotation.Y = reader.GetFloat();
            if ((bits & Fields.RotZ) == Fields.RotZ)
                rotation.Z = reader.GetFloat();
            if ((bits & Fields.RotW) == Fields.RotW)
                rotation.W = reader.GetFloat();

            if ((bits & Fields.Health) == Fields.Health)
                health = reader.GetInt();
            if ((bits & Fields.State) == Fields.State)
                state = reader.GetInt();
        }

        public void SerializeDelta(NetDataWriter writer, ref readonly BasicState prevState)
        {
            writer.Put(VERSION);
            int bitsPos = writer.Length;
            writer.Put((uint)0);

            Fields bits = Fields.None;
            if (position.X != prevState.position.X)
            {
                bits |= Fields.PosX;
                writer.Put(position.X);
            }
            if (position.Y != prevState.position.Y)
            {
                bits |= Fields.PosY;
                writer.Put(position.Y);
            }
            if (position.Z != prevState.position.Z)
            {
                bits |= Fields.PosZ;
                writer.Put(position.Z);
            }

            if (velocity.X != prevState.velocity.X)
            {
                bits |= Fields.VelX;
                writer.Put(velocity.X);
            }
            if (velocity.Y != prevState.velocity.Y)
            {
                bits |= Fields.VelY;
                writer.Put(velocity.Y);
            }
            if (velocity.Z != prevState.velocity.Z)
            {
                bits |= Fields.VelZ;
                writer.Put(velocity.Z);
            }

            if (rotation.X != prevState.rotation.X)
            {
                bits |= Fields.RotX;
                writer.Put(rotation.X);
            }
            if (rotation.Y != prevState.rotation.Y)
            {
                bits |= Fields.RotY;
                writer.Put(rotation.Y);
            }
            if (rotation.Z != prevState.rotation.Z)
            {
                bits |= Fields.RotZ;
                writer.Put(rotation.Z);
            }
            if (rotation.W != prevState.rotation.W)
            {
                bits |= Fields.RotW;
                writer.Put(rotation.W);
            }

            if (health != prevState.health)
            {
                bits |= Fields.Health;
                writer.Put(health);
            }
            if (state != prevState.state)
            {
                bits |= Fields.State;
                writer.Put(state);
            }

            for (int i = 0; i < 4; i++)
            {
                if (timers[i] != prevState.timers[i])
                {
                    bits |= (Fields)((int)Fields.Timer0 + i);
                    writer.Put(timers[i]);
                }
            }

            for (int i = 0; i < 4; i++)
            {
                if (counters[i] != prevState.counters[i])
                {
                    bits |= (Fields)((int)Fields.Counter0 + i);
                    writer.Put(counters[i]);
                }
            }

            int end = writer.Length;
            writer.SetPosition(bitsPos);
            writer.Put((uint)bits);
            writer.SetPosition(end);
        }

        public void OnSave(List<byte> saveBytes)
        {
            SaveHelper.SaveVector3(saveBytes, position);
            SaveHelper.SaveVector3(saveBytes, velocity);
            SaveHelper.SaveVector4(saveBytes, rotation.ToVector4());
            SaveHelper.SaveInt32(saveBytes, health);
            SaveHelper.SaveInt32(saveBytes, state);
            for (int i = 0; i < 4; i++)
                SaveHelper.SaveFloat32(saveBytes, timers[i]);
            for (int i = 0; i < 4; i++)
                SaveHelper.SaveInt32(saveBytes, counters[i]);
        }

        public void OnLoad(byte[] loadBytes, ref int index)
        {
            position = SaveHelper.LoadVector3(loadBytes, ref index);
            velocity = SaveHelper.LoadVector3(loadBytes, ref index);
            rotation = new Quaternion(SaveHelper.LoadVector4(loadBytes, ref index));
            health = SaveHelper.LoadInt32(loadBytes, ref index);
            state = SaveHelper.LoadInt32(loadBytes, ref index);

            for (int i = 0; i < 4; i++)
                timers[i] = SaveHelper.LoadFloat32(loadBytes, ref index);
            for (int i = 0; i < 4; i++)
                counters[i] = SaveHelper.LoadInt32(loadBytes, ref index);
        }
    }
}
