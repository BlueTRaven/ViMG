using BepuPhysics;
using BrUtility;
using Engine.Entities;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ViMG;

namespace Engine.Networking
{
    public struct BasicState : INetSerializable
    {
        private const int MAX_EXTRA_STATE_BYTES = 256;
        private const int MAX_EXTRA_STATE_INTS = MAX_EXTRA_STATE_BYTES / sizeof(int);

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

            ExtraFields = 1 << 20,
        }

        private const int VERSION = 2;
        [System.Runtime.CompilerServices.InlineArray(4)]
        public struct Arr4F
        {
            private float _element0;

            public float this[int i]
            {
                get => this[i];
                set => this[i] = value;
            }

            public override bool Equals(object? obj)
            {
                if (obj is Arr4F other)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        if (this[i] != other[i])
                            return false;
                    }

                    return true;
                }
                return false;
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

            public override bool Equals(object? obj)
            {
                if (obj is Arr4I other)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        if (this[i] != other[i])
                            return false;
                    }

                    return true;
                }
                return false;   
            }
        }
        [System.Runtime.CompilerServices.InlineArray(MAX_EXTRA_STATE_BYTES)]
        public struct ArrExtraStateBytes
        {
            private byte _element0;

            public byte this[int i]
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
        public float aliveTime;

        public ArrExtraStateBytes extraBytes;

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

            if (version >= 2)
            {
                ReadOnlySpan<byte> remBytes = reader.GetRemainingBytesSpan();
                remBytes[0..MAX_EXTRA_STATE_BYTES].CopyTo(extraBytes);
                reader.SetPosition(reader.Position + MAX_EXTRA_STATE_BYTES);
            }
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

            writer.Put((ReadOnlySpan<byte>)extraBytes);
        }

        public uint GetDeltaBits(ref readonly BasicState prevState)
        {
            Fields bits = Fields.None;
            if (position.X != prevState.position.X)
                bits |= Fields.PosX;
            if (position.Y != prevState.position.Y)
                bits |= Fields.PosY;
            if (position.Z != prevState.position.Z)
                bits |= Fields.PosZ;

            if (velocity.X != prevState.velocity.X)
                bits |= Fields.VelX;
            if (velocity.Y != prevState.velocity.Y)
                bits |= Fields.VelY;
            if (velocity.Z != prevState.velocity.Z)
                bits |= Fields.VelZ;

            if (rotation.X != prevState.rotation.X)
                bits |= Fields.RotX;
            if (rotation.Y != prevState.rotation.Y)
                bits |= Fields.RotY;
            if (rotation.Z != prevState.rotation.Z)
                bits |= Fields.RotZ;
            if (rotation.W != prevState.rotation.W)
                bits |= Fields.RotW;

            if (health != prevState.health)
                bits |= Fields.Health;
            if (state != prevState.state)
                bits |= Fields.State;

            for (int i = 0; i < 4; i++)
            {
                if (timers[i] != prevState.timers[i])
                    bits |= (Fields)((int)Fields.Timer0 + i);
            }

            for (int i = 0; i < 4; i++)
            {
                if (counters[i] != prevState.counters[i])
                    bits |= (Fields)((int)Fields.Counter0 + i);
            }

            for (int i = 0; i < MAX_EXTRA_STATE_BYTES; i++)
            {
                if (prevState.extraBytes[i] != extraBytes[i])
                {
                    bits |= Fields.ExtraFields;
                    break;
                }
            }

            return (uint)bits;
        }

        public ulong GetNumBytesFromBits(uint bits, ulong extraBytesBits)
        {
            ulong sum = 0;

            Fields fields = (Fields)bits;
            if ((fields & Fields.PosX) == Fields.PosX)
                sum += sizeof(float);
            if ((fields & Fields.PosY) == Fields.PosY)
                sum += sizeof(float);
            if ((fields & Fields.PosZ) == Fields.PosZ)
                sum += sizeof(float);

            if ((fields & Fields.VelX) == Fields.VelX)
                sum += sizeof(float);
            if ((fields & Fields.VelY) == Fields.VelY)
                sum += sizeof(float);
            if ((fields & Fields.VelZ) == Fields.VelZ)
                sum += sizeof(float);

            if ((fields & Fields.RotX) == Fields.RotX)
                sum += sizeof(float);
            if ((fields & Fields.RotY) == Fields.RotY)
                sum += sizeof(float);
            if ((fields & Fields.RotZ) == Fields.RotZ)
                sum += sizeof(float);
            if ((fields & Fields.RotW) == Fields.RotW)
                sum += sizeof(float);

            if ((fields & Fields.Health) == Fields.Health)
                sum += sizeof(int);
            if ((fields & Fields.State) == Fields.State)
                sum += sizeof(int);

            for (int i = 0; i < 4; i++)
            {
                Fields bit = (Fields)((int)Fields.Timer0 + i);
                if ((fields & bit) == bit)
                    sum += sizeof(float);
            }

            for (int i = 0; i < 4; i++)
            {
                Fields bit = (Fields)((int)Fields.Counter0 + i);
                if ((fields & bit) == bit)
                    sum += sizeof(int);
            }

            sum += (ulong)(System.Numerics.BitOperations.PopCount(extraBytesBits) * sizeof(int));

            return sum;
        }

        // NOTE: even if MAX_EXTRA_STATE_BYTES != 256 (64 int chunks) right now, we still use a ulong for extra bits.
        public ulong GetExtraBytesBits(ref readonly BasicState prevState)
        {
            ulong bits = 0;
            for (int i = 0; i < MAX_EXTRA_STATE_INTS; i++)
            {
                int min = i * sizeof(uint);
                int max = i * sizeof(uint) + sizeof(uint);
                uint currInt = BitConverter.ToUInt32(extraBytes[min..max]);
                uint prevInt = BitConverter.ToUInt32(prevState.extraBytes[min..max]);
                if (currInt != prevInt)
                {
                    bits |= (1UL << i);
                }
            }

            return bits;
        }

        public uint DeserializeDelta(NetDataReader reader, bool withExtraFields = true)
        {
            version = reader.GetInt();
            Fields bits = (Fields)reader.GetUInt();

            if ((bits & Fields.PosX) == Fields.PosX)
                position.X = reader.GetFloat();
            if ((bits & Fields.PosY) == Fields.PosY)
                position.Y = reader.GetFloat();
            if ((bits & Fields.PosZ) == Fields.PosZ)
                position.Z = reader.GetFloat();

            //if (position == Vector3.Zero)
            //{
            //    Console.WriteLine("!!!");
            //}

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

            for (int i = 0; i < 4; i++)
            {
                Fields bit = (Fields)((int)Fields.Timer0 + i);
                if ((bits & bit) == bit)
                    timers[i] = reader.GetFloat();
            }

            for (int i = 0; i < 4; i++)
            {
                Fields bit = (Fields)((int)Fields.Counter0 + i);
                if ((bits & bit) == bit)
                    counters[i] = reader.GetInt();
            }

            if (withExtraFields)
            {
                ulong extraBytesBits = reader.GetULong();

                if ((bits & Fields.ExtraFields) == Fields.ExtraFields)
                {
                    for (int i = 0; i < MAX_EXTRA_STATE_INTS; i++)
                    {
                        ulong bit = 1UL << i;
                        if ((extraBytesBits & bit) == bit)
                        //if (true)
                        {
                            int min = i * sizeof(uint);
                            int max = i * sizeof(uint) + sizeof(uint);
                            var extraBitBytes = extraBytes[min..max];
                            uint cui = BitConverter.ToUInt32(extraBitBytes);
                            uint ui = reader.GetUInt();
                            BitConverter.TryWriteBytes(extraBitBytes, ui);
                        }
                    }
                }
            }

            return (uint)bits;
        }

        public void DeserializeDeltaExtraFields(NetDataReader reader, uint bits)
        {
            ulong extraBytesBits = reader.GetULong();

            if (((Fields)bits & Fields.ExtraFields) == Fields.ExtraFields)
            {
                for (int i = 0; i < MAX_EXTRA_STATE_INTS; i++)
                {
                    ulong bit = 1UL << i;
                    if ((extraBytesBits & bit) == bit)
                    //if (true)
                    {
                        int min = i * sizeof(uint);
                        int max = i * sizeof(uint) + sizeof(uint);
                        var extraBitBytes = extraBytes[min..max];
                        uint cui = BitConverter.ToUInt32(extraBitBytes);
                        uint ui = reader.GetUInt();
                        BitConverter.TryWriteBytes(extraBitBytes, ui);
                    }
                }
            }
        }

        public uint GetExtraAtBit(int bit)
        {
            int min = bit * sizeof(uint);
            int max = bit * sizeof(uint) + sizeof(uint);
            uint currInt = BitConverter.ToUInt32(extraBytes[min..max]);
            return currInt;
        }

        public void SerializeDelta(NetDataWriter writer, uint _bits)
        {
            writer.Put(VERSION);
            writer.Put(_bits);

            Fields bits = (Fields)_bits;

            if ((bits & Fields.PosX) == Fields.PosX)
                writer.Put(position.X);
            if ((bits & Fields.PosY) == Fields.PosY)
                writer.Put(position.Y);
            if ((bits & Fields.PosZ) == Fields.PosZ)
                writer.Put(position.Z);

            if ((bits & Fields.VelX) == Fields.VelX)
                writer.Put(velocity.X);
            if ((bits & Fields.VelY) == Fields.VelY)
                writer.Put(velocity.Y);
            if ((bits & Fields.VelZ) == Fields.VelZ)
                writer.Put(velocity.Z);

            if ((bits & Fields.RotX) == Fields.RotX)
                writer.Put(rotation.X);
            if ((bits & Fields.RotY) == Fields.RotY)
                writer.Put(rotation.Y);
            if ((bits & Fields.RotZ) == Fields.RotZ)
                writer.Put(rotation.Z);
            if ((bits & Fields.RotW) == Fields.RotW)
                writer.Put(rotation.W);

            if ((bits & Fields.Health) == Fields.Health)
                writer.Put(health);
            if ((bits & Fields.State) == Fields.State)
                writer.Put(state);

            for (int i = 0; i < 4; i++)
            {
                Fields bit = (Fields)((int)Fields.Timer0 + i);
                if ((bits & bit) == bit)
                    writer.Put(timers[i]);
            }

            for (int i = 0; i < 4; i++)
            {
                Fields bit = (Fields)((int)Fields.Counter0 + i);
                if ((bits & bit) == bit)
                    writer.Put(counters[i]);
            }
        }

        public void SerializeDeltaExtraFields(NetDataWriter writer, ulong extraBytesBits)
        {
            writer.Put(extraBytesBits);

            var startLen = writer.Length;

            for (int i = 0; i < MAX_EXTRA_STATE_INTS; i++)
            {
                ulong bit = 1UL << i;
                if ((extraBytesBits & bit) == bit)
                //if (true)
                {
                    int min = i * sizeof(uint);
                    int max = i * sizeof(uint) + sizeof(uint);

                    var extraBitBytes = extraBytes[min..max];
                    uint ui = BitConverter.ToUInt32(extraBitBytes);
                    writer.Put(ui);
                }
            }

            Debug.Assert(writer.Length == startLen + System.Numerics.BitOperations.PopCount(extraBytesBits) * 4);
        }

        public unsafe void SetExtra<T>(ref readonly T val) where T : unmanaged
        {
            Debug.Assert(sizeof(T) <= MAX_EXTRA_STATE_BYTES);
            Span<byte> bytes = extraBytes;
            // TODO is this necessary? Can we just [val]? Does that require a copy?
            ReadOnlySpan<T> valSpan = MemoryMarshal.CreateReadOnlySpan(in val, 1);
            ReadOnlySpan<byte> valBytes = MemoryMarshal.Cast<T, byte>(valSpan);
            valBytes.CopyTo(bytes);
        }

        public unsafe T GetExtra<T>() where T : unmanaged
        {
            Debug.Assert(sizeof(T) <= MAX_EXTRA_STATE_BYTES);
            Span<byte> bytes = extraBytes;
            return MemoryMarshal.Cast<byte, T>(bytes)[0];
        }

        public Vector3 GetInterpPosition(BasicState other, double t)
        {
            return Vector3.Lerp(position, other.position, (float)t);
        }
        
        public Vector3 GetInterpVelocity(BasicState other, double t)
        {
            return Vector3.Lerp(velocity, other.velocity, (float)t);
        }

        public Quaternion GetInterpRotation(BasicState other, double t)
        {
            return Quaternion.Lerp(rotation, other.rotation, (float)t);
        }

        public float GetInterpTimer(BasicState other, int timer, double t)
        {
            return MathHelper.Lerp(timers[timer], other.timers[timer], (float)t);
        }

        public int GetInterpCounter(BasicState other, int counter, double t)
        {
            return (int)MathHelper.Lerp(counters[counter], other.counters[counter], (float)t);
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

        public static Vector3 Forward(ref readonly BasicState state)
        {
            Matrix mat = Matrix.CreateFromQuaternion(state.rotation);

            return Vector3.Transform(new Vector3(0, 0, 1), mat);
        }

        public static Vector3 ForwardYawOnly(ref readonly BasicState state)
        {
            var newQuat = state.rotation;
            newQuat.X = 0;
            newQuat.Z = 0;
            var mag = float.Sqrt(newQuat.W * newQuat.W + newQuat.Y * newQuat.Y);
            newQuat.W /= mag;
            newQuat.Y /= mag;
            Matrix mat = Matrix.CreateFromQuaternion(newQuat);

            return Vector3.Transform(new Vector3(0, 0, 1), mat);
        }

        public static Vector3 Up(ref readonly BasicState state)
        {
            Matrix mat = Matrix.CreateFromQuaternion(state.rotation);

            return Vector3.Transform(new Vector3(0, 1, 0), mat);
        }

        public static Vector3 Right(ref readonly BasicState state)
        {
            Matrix mat = Matrix.CreateFromQuaternion(state.rotation);

            return Vector3.Transform(new Vector3(1, 0, 0), mat);
        }
    }
}
