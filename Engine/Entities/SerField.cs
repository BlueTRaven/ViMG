using BepuUtilities.Memory;
using BrUtility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Entities
{
    public abstract class ASerField
    {
        public int offset;
        public int size;

        public ASerField()
        {
        }
    }

    public class SerField<T> : ASerField where T : unmanaged
    {
        public unsafe SerField(ref int accumulator)
        {
            offset = accumulator;
            size = sizeof(T);
            accumulator += size;
        }

        //public struct Instance
        //{
        //    private SerField<T> field;
        //    private Buffer<byte> buffer;

        //    public Instance(SerField<T> field, Buffer<byte> buffer)
        //    {
        //        this.field = field;
        //        this.buffer = buffer;
        //    }

        //    public T Get()
        //    {
        //        return MemoryMarshal.Cast<byte, T>(buffer.Slice(field.offset, field.size))[0];
        //    }

        //    public unsafe void Set(T value)
        //    {
        //        Span<byte> bytes = new Span<byte>(buffer.GetPointer(field.offset), field.size);
        //        if (value is int i)
        //            BitConverter.TryWriteBytes(bytes, i);
        //        else if (value is float f)
        //            BitConverter.TryWriteBytes(bytes, f);
        //        else
        //        {
        //            buffer.CopyFrom(bytes, 0, field.offset, field.size);
        //        }
        //    }

        //    public static implicit operator T(Instance value)
        //    {
        //        return value.Get();
        //    }
        //}
    }

    public class SerFieldInt : SerField<int>
    {
        public SerFieldInt(ref int accumulator) : base(ref accumulator)
        {

        }

        public struct Instance
        {
            private SerFieldInt field;
            private Buffer<byte> buffer;

            public Instance(SerFieldInt field, Buffer<byte> buffer)
            {
                this.field = field;
                this.buffer = buffer;
            }

            public int Get()
            {
                return MemoryMarshal.Cast<byte, int>(buffer.Slice(field.offset, field.size))[0];
            }

            public unsafe void Set(int value)
            {
                Span<byte> bytes = new Span<byte>(buffer.GetPointer(field.offset), field.size);
                BitConverter.TryWriteBytes(bytes, value);
            }

            public static implicit operator int(Instance value)
            {
                return value.Get();
            }
        }
    }

    public class SerFieldFloat : SerField<float>
    {
        public SerFieldFloat(ref int accumulator) : base(ref accumulator)
        {

        }

        public struct Instance
        {
            private SerFieldFloat field;
            private Buffer<byte> buffer;

            public Instance(SerFieldFloat field, Buffer<byte> buffer)
            {
                this.field = field;
                this.buffer = buffer;
            }

            public float Get()
            {
                return MemoryMarshal.Cast<byte, float>(buffer.Slice(field.offset, field.size))[0];
            }

            public unsafe void Set(float value)
            {
                Span<byte> bytes = new Span<byte>(buffer.GetPointer(field.offset), field.size);
                BitConverter.TryWriteBytes(bytes, value);
            }

            public static implicit operator float(Instance value)
            {
                return value.Get();
            }
        }
    }

    public struct SlimeDef
    {
        private int size = 0;
        private ASerField[] fields;

        public SerFieldFloat jumpTimer;
        public SerFieldInt health;

        public SlimeDef()
        {
            FastList<ASerField> fields = new();
            int accumulator = 0;
            jumpTimer = new(ref accumulator);
            health = new(ref accumulator);
            fields.Add(jumpTimer);
            fields.Add(health);

            size = accumulator;

            this.fields = fields.Buffer[0..fields.Length];
        }

        public Buffer<byte> New(BufferPool pool)
        {
            pool.Take<byte>(size, out var buffer);
            return buffer;
        }
    }

    public struct Slime1
    {
        private static SlimeDef def = new();

        private Buffer<byte> buffer;

        public SerFieldFloat.Instance jumpTimer;
        public SerFieldInt.Instance health;

        public Slime1(BufferPool pool)
        {
            buffer = def.New(pool);
            jumpTimer = new(def.jumpTimer, buffer);
            health = new(def.health, buffer);
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Slime2
    {
        public float jumpTimer;
        public int health;
    }

    public static class FieldTest
    {
        private static unsafe void SetIntAtOffset(ref Slime2 s2, int offset, int value)
        {
            ref byte baseRef = ref Unsafe.As<Slime2, byte>(ref s2);

            // Add the offset to the base reference to get a reference to the target field location
            ref byte targetRef = ref Unsafe.Add(ref baseRef, offset);

            ref int dest = ref Unsafe.As<byte, int>(ref targetRef);
            
            dest = value;
        }

        private static unsafe void SetFloatAtOffset(ref Slime2 s2, int offset, float value)
        {
            ref byte baseRef = ref Unsafe.As<Slime2, byte>(ref s2);

            // Add the offset to the base reference to get a reference to the target field location
            ref byte targetRef = ref Unsafe.Add(ref baseRef, offset);

            ref float dest = ref Unsafe.As<byte, float>(ref targetRef);

            dest = value;
        }

        public static void DoTest()
        {
            int num = 16_000_000;

            BufferPool p = new BufferPool();

            Slime1[] s1s = new Slime1[num];
            Slime2[] s2s = new Slime2[num];

            var watch = Stopwatch.StartNew();
            for (int i = 0; i < num; i++)
            {
                s1s[i] = new Slime1(p);
            }
            watch.Stop();
            Console.WriteLine("Create 1: {0}", watch.Elapsed.TotalSeconds);

            watch.Restart();
            for (int i = 0; i < num; i++)
            {
                s2s[i] = new Slime2();
            }
            watch.Stop();
            Console.WriteLine("Create 2: {0}", watch.Elapsed.TotalSeconds);

            Random rnd = new Random(16);
            //watch.Restart();
            //for (int i = 0; i < num; i++)
            //{
            //    s1s[i].jumpTimer.Set(rnd.NextFloat());
            //    s1s[i].health.Set(rnd.Next());
            //}
            //watch.Stop();
            //Console.WriteLine("Set 1: {0}", watch.Elapsed.TotalSeconds);

            //rnd = new Random(16);
            watch.Restart();
            for (int i = 0; i < num; i++)
            {
                float jumpTimer = rnd.NextFloat();
                int health = rnd.Next();
                s2s[i].jumpTimer = jumpTimer;
                s2s[i].health = health;

                Debug.Assert(s2s[i].jumpTimer == jumpTimer);
                Debug.Assert(s2s[i].health == health);
            }
            watch.Stop();
            Console.WriteLine("Set 2: {0}", watch.Elapsed.TotalSeconds);

            int offsetOfJumpTimer = Marshal.OffsetOf<Slime2>("jumpTimer").ToInt32();
            int offsetOfHealth = Marshal.OffsetOf<Slime2>("health").ToInt32();
            rnd = new Random(16);
            watch.Restart();
            for (int i = 0; i < num; i++)
            {
                float jumpTimer = rnd.NextFloat();
                int health = rnd.Next();
                SetFloatAtOffset(ref s2s[i], offsetOfJumpTimer, jumpTimer);
                SetIntAtOffset(ref s2s[i], offsetOfHealth, health);

                Debug.Assert(s2s[i].jumpTimer == jumpTimer);
                Debug.Assert(s2s[i].health == health);
            }
            watch.Stop();
            Console.WriteLine("Set 3: {0}", watch.Elapsed.TotalSeconds);
        }
    }
}
