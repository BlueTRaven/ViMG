using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Generation
{
    //A pre-made or pre-generated layout of cubes, fit to be overlaid at any given point in the world.
    public class Structure
    {
        public Point3D size;
        public ushort[] data;

        public Structure(Point3D size, ushort[] data)
        {
            this.size = size;
            this.data = data;
        }

        public void Serialize(List<byte> bytes)
        {
            SaveHelper.SaveInt32(bytes, size.X);
            SaveHelper.SaveInt32(bytes, size.Y);
            SaveHelper.SaveInt32(bytes, size.Z);

            SaveHelper.SaveInt32(bytes, data.Length);
            for (int i = 0; i < data.Length; i++)
                SaveHelper.SaveUInt16(bytes, data[i]);
        }

        public static Structure Deserialize(byte[] bytes)
        {
            int index = 0;

            int x = SaveHelper.LoadInt32(bytes, ref index);
            int y = SaveHelper.LoadInt32(bytes, ref index);
            int z = SaveHelper.LoadInt32(bytes, ref index);

            Point3D size = new Point3D(x, y, z);

            int dataSize = SaveHelper.LoadInt32(bytes, ref index);
            ushort[] data = new ushort[dataSize];
            for (int i = 0; i < dataSize; i++)
            {
                data[i] = SaveHelper.LoadUInt16(bytes, ref index);
            }

            return new Structure(size, data);
        }
    }

    //The idea here is we pregenerate a bunch of Structures, for instance, GOL3DSims, using multithreading. These operate on their own arrays
    //and do not write to anything outside themselves, so it's perfect for this task.
    //Later on, generation will pick from a list of these Structures and paste it into the world where it wants it.
    //The only cost we incur here is that of actually copying the Structure.
    //Of course, there are problems presented with this method of structure generation: we can't do any real reading
    //from the ChunkManager as we don't define these structures as being at any specific points.
    //This means that environment-sensitive structures won't work unless that environment sensitivity is taken care of entirely in the
    //pasting, which is plausible to do.

    //Other benefits of this system:
    //Structures can be used in other ways, not just pregenerating variations of generated structures.
    //We might have some pre-made structures as well. Maybe we want a house somewhere on the island. If we ever end up implementing a 
    //method of copying data that the player has built in gameand putting it into a structure (a very useful operation I will definitely look at)
    //then we can paste these freely wherever we want. We could build it in game and then paste it through world generation!

    public abstract class StructureGenerator
    {
        public class StructureGeneratorBatchCollection
        {
            public readonly StructureGeneratorBatch[] batches;

            public readonly int num;
            public readonly int numBatches;

            public StructureGeneratorBatchCollection(StructureGeneratorBatch[] batches, int num, int numBatches)
            {
                this.batches = batches;
                this.num = num;
                this.numBatches = numBatches;
            }

            public Structure Get(int i)
            {
                int numPerBatch = num / numBatches;

                int batch = i / numPerBatch;
                int inBatch = i % numPerBatch;

                return batches[batch].GetOutput()[inBatch];
            }
        }

        public readonly struct StructureGeneratorBatch
        {
            private readonly Task<Structure[]> task;

            public StructureGeneratorBatch(Task<Structure[]> task)
            {
                this.task = task;
            }

            //Waits and returns the result.
            public Structure[] GetOutput()
            {
                task.Wait();

                return task.Result;
            }

            public void Start()
            {
                task.Start();
            }
        }

        protected readonly struct StructureTaskState
        {
            public readonly int sliceStart;
            public readonly int sliceEnd;

            public readonly StructureGeneratorBatch[] outputStructureArray;

            public readonly Random random;
            public readonly ChunkManager chunkManager;

            public StructureTaskState(int sliceStart, int sliceEnd, StructureGeneratorBatch[] outputStructureArray, Random random, ChunkManager chunkManager)
            {
                this.sliceStart = sliceStart;
                this.sliceEnd = sliceEnd;

                this.outputStructureArray = outputStructureArray;
                this.random = random;
                this.chunkManager = chunkManager;
            }
        }

        public readonly string Name;
        private int seed;
        private ChunkManager chunkManager;

        public StructureGenerator(string name, int seed, ChunkManager chunkManager)
        {
            this.Name = name;
            this.seed = seed;
        }

        public StructureGeneratorBatchCollection Generate(int num, int numBatches, bool runSynchronously = false)
        {
            if (num % numBatches != 0)
                throw new Exception(string.Format("Cannot generate {0} num with {1} batches; num must be divisible by batches!", num, numBatches));

            StructureGeneratorBatch[] batches = new StructureGeneratorBatch[numBatches];

            Task initTask = new Task((obj) =>
            {
                StructureGeneratorBatch[] batches = (StructureGeneratorBatch[])obj;

                Task[] tasks = new Task[numBatches];

                int numPerBatch = num / numBatches;

                for (int i = 0; i < num; i += numPerBatch)
                {
                    int sliceStart = i;
                    int sliceEnd = i + numPerBatch;

                    StructureTaskState state = new StructureTaskState(sliceStart, sliceEnd, batches, new Random(seed + i * 128 / 3), chunkManager);

                    Task<Structure[]> task = new Task<Structure[]>(GenerateOneWrapper, state);
                    batches[i / numPerBatch] = new StructureGeneratorBatch(task);
                    tasks[i / numPerBatch] = task;
                }
            }, batches);

            //No real reason for this to be a thread... but whatever.
            //This guarantees that batches are fully initialized.
            initTask.RunSynchronously();

            Task startupTask = new Task((obj) =>
            {
                Stopwatch watch = Stopwatch.StartNew();

                StructureGeneratorBatch[] batches = (StructureGeneratorBatch[])obj;

                //Start all batches
                for (int i = 0; i < batches.Length; i++)
                    batches[i].Start();

                //Wait for them to finish
                for (int i = 0; i < batches.Length; i++)
                    batches[i].GetOutput();

                watch.Stop();

                Console.WriteLine("Finished {0} Structure Generation. Generated {1} structures in {2} batches ({3} each) in {4} seconds.",
                    Name, num, numBatches, num / numBatches, watch.Elapsed.TotalSeconds);
            }, batches);

            if (!runSynchronously)
                startupTask.Start();
            else startupTask.RunSynchronously();

            return new StructureGeneratorBatchCollection(batches, num, numBatches);
        }

        private Structure[] GenerateOneWrapper(object state)
        {
            StructureTaskState casted = (StructureTaskState)state;
            return GenerateOne(ref casted);
        }

        protected abstract Structure[] GenerateOne(ref StructureTaskState state);
    }
}
