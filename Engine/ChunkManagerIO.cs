using Engine.ChunkStuff;
using Engine.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;

namespace ViMG
{
    public class ChunkManagerIO : WorldIO
    {
		public ref struct CapturedChunk : IDisposable
		{
			private ref LoadedChunk ourChunk;
			public ReadOnlySpan<ushort> data;

			private bool valid;

			public static CapturedChunk Invalid => new(0);
			public CapturedChunk(ref LoadedChunk chunk)
			{
				Debug.Assert(chunk.LoadedState == LoadedState.Loaded);
				Interlocked.Increment(ref chunk.refCount);
				
				ourChunk = ref chunk;
				data = chunk.cubes;

				valid = true;
			}

			private CapturedChunk(int a)
			{
				data = ReadOnlySpan<ushort>.Empty;

				valid = false;
			}

            public void Dispose()
            {
				if (valid)
					Interlocked.Decrement(ref ourChunk.refCount);
            }
        }

        public enum LoadedState : byte
        {
            Unloaded,   // On disk
            Palettized, // Present in memory, but palettized
			Depalettizing,
            Loaded,     // Fully decompressed in memory
        }

        public struct LoadedChunk
        {
			public LoadedState LoadedState => (LoadedState)loadedState;
            public int loadedState;

            public PalettizedChunk palettizedChunk;

            // null if storedPalettized
            public ushort[]? cubes;

			public object l;

			public int refCount = 0;

			public LoadedChunk()
			{
				palettizedChunk = new();
				loadedState = (int)LoadedState.Unloaded;
				cubes = null;
				l = new();
			}

            public LoadedChunk(PalettizedChunk palettizedChunk)
            {
                this.palettizedChunk = palettizedChunk;
                loadedState = (int)LoadedState.Palettized;
                cubes = null;
            }
        }

        public const string FILE_NAME_CHUNK_OLD = "chunks_";
		public const string FILE_NAME_CHUNK = "chunks";
		public const string EXT_CHUNK = ".vis";

		private const long SIZEOF_HEADER = sizeof(int) * 4;
		private const long SIZEOF_CHUNK = (sizeof(ushort) * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE);

		private const int VERSION = 4;
		private const int MIN_VERSION = 1;

		public int Version;

		//private readonly ChunkManager manager;
		private readonly string managerName;
        private readonly int sizeInChunks;
        private int layer;
		private readonly long numChunks;

		private bool loaded;
		//private byte[] allBytes;

		private long[] chunkOffsets;

		private LoadedChunk[] loadedChunks;

		private FileStream regionFile;

		//private const int NUM_CHUNKS_TO_KEEP_IN_MEMORY = 1726;
		//private const long SIZEOF_CHUNKS_IN_MEMORY = NUM_CHUNKS_TO_KEEP_IN_MEMORY * SIZEOF_CHUNK;
		//private int offset;
		//private FileStream fs;

		public ChunkManagerIO(int sizeInChunks, string chunkManagerName, int layer)
        {
			this.managerName = chunkManagerName;
            this.layer = layer;
            this.sizeInChunks = sizeInChunks;
			numChunks = sizeInChunks * sizeInChunks * sizeInChunks;
			//allBytes = new byte[numChunks * SIZEOF_CHUNK];

			chunkOffsets = new long[numChunks];
			loadedChunks = new LoadedChunk[numChunks];
			for (int i = 0; i < numChunks; i++)
			{
				loadedChunks[i] = new();
			}
        }

		public void Initialize()
        {

        }

		public void CreateAll()
		{
			for (int i = 0; i < numChunks; i++)
			{
				Util.OneDToThreeD(i, new ValuePoint3D(sizeInChunks), out var p);
                loadedChunks[i] = new()
                {
                    cubes = new ushort[Chunk.NUM_CUBES_IN_CHUNK],
                    loadedState = (int)LoadedState.Loaded,
                };
            }
		}

		public void CreateChunk(ChunkPosition position)
		{
            Util.ThreeDToOneD(new ValuePoint3D(position.X, position.Y, position.Z), new ValuePoint3D(sizeInChunks), out int i);
			loadedChunks[i] = new()
			{
                cubes = new ushort[Chunk.NUM_CUBES_IN_CHUNK],
				loadedState = (int)LoadedState.Loaded,
            };
        }

		public void UnloadAll()
		{
			for (int i = 0; i < numChunks; i++)
			{
				loadedChunks[i] = new();
			}
		}

		public bool IsLoaded(ChunkPosition position)
		{
            Util.ThreeDToOneD(new ValuePoint3D(position.X, position.Y, position.Z), new ValuePoint3D(sizeInChunks), out int i);
			return loadedChunks[i].LoadedState != LoadedState.Unloaded;
        }

        [Flags]
		public enum GetMode
		{
			None = 0,
			Read = 1,
			Write = 2,
			ReadWrite = Read | Write,
		}

		private void DepalettizeOrWait(ref LoadedChunk chunk)
		{
            if (Interlocked.CompareExchange(ref chunk.loadedState, (int)LoadedState.Depalettizing, (int)LoadedState.Palettized) == (int)LoadedState.Palettized)
            {
                lock (chunk.l)
                {
                    chunk.cubes = PalettizedChunk.Depaletteize(chunk.palettizedChunk);
                    chunk.loadedState = (int)LoadedState.Loaded;
                    chunk.palettizedChunk = new PalettizedChunk();
                }
            }
            else if (chunk.LoadedState == LoadedState.Depalettizing)
            {
                // Waits for above lock to be released on other thread
                lock (chunk.l) { }
                Debug.Assert(chunk.LoadedState == LoadedState.Loaded);
            }
        }

		public Span<ushort> GetChunk(ChunkPosition position, GetMode mode)
        {
            Util.ThreeDToOneD(new ValuePoint3D(position.X, position.Y, position.Z), new ValuePoint3D(sizeInChunks), out int i);
			if (loadedChunks[i].LoadedState == LoadedState.Unloaded) return null;

			DepalettizeOrWait(ref loadedChunks[i]);

			Debug.Assert(loadedChunks[i].cubes != null);

			return loadedChunks[i].cubes!;
		}

		public CapturedChunk GetChunk(ChunkPosition position)
        {
            Util.ThreeDToOneD(new ValuePoint3D(position.X, position.Y, position.Z), new ValuePoint3D(sizeInChunks), out int i);
            if (loadedChunks[i].LoadedState == LoadedState.Unloaded) throw new Exception();
            DepalettizeOrWait(ref loadedChunks[i]);

            return new CapturedChunk(ref loadedChunks[i]);
		}

		public PalettizedChunk? GetPalettizedChunk(ChunkPosition position)
		{
			Util.ThreeDToOneD(new ValuePoint3D(position.X, position.Y, position.Z), new ValuePoint3D(sizeInChunks), out int i);
			if (loadedChunks[i].LoadedState != LoadedState.Palettized) return null;

			return loadedChunks[i].palettizedChunk;
		}

		//public ushort GetId(CubePosition position)
		//{
		//	ChunkPosition chunkPos = ChunkPosition.CubeChunk(position);
  //          Util.ThreeDToOneD(new ValuePoint3D(chunkPos), new ValuePoint3D(sizeInChunks), out int i);

		//	if (loadedChunks[i].LoadedState == LoadedState.Unloaded) return 0;

		//	if (loadedChunks[i].LoadedState == LoadedState.Palettized)
		//	{
		//		return loadedChunks[i].palettizedChunk.GetId(position.InChunkSpace());
		//	}
		//	else 
		//	{
  //              Util.ThreeDToOneD(new ValuePoint3D(position.InChunkSpace()), new ValuePoint3D(Chunk.CHUNK_SIZE), out int j);
  //              return loadedChunks[i].cubes[j];
		//	}

		//	return 0;
  //      }

		public void SetChunk(ref readonly PalettizedChunk palettized)
		{
            Util.ThreeDToOneD(new ValuePoint3D(palettized.position.X, palettized.position.Y, palettized.position.Z), new ValuePoint3D(sizeInChunks), out int i);

			loadedChunks[i] = new LoadedChunk()
			{
				cubes = null,
				palettizedChunk = palettized,
				loadedState = (int)LoadedState.Palettized,
			};
        }

        public void ReleaseChunk(ChunkPosition position, GetMode mode)
		{
        }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetCubeOffset(CubePosition position, int sizeInChunks = 32)
        {
			//NOTE: we can't just index directly into bytes (as a ushort)
			//This is because we store cube ids weirdly. We do not store them flat, one after another; instead, we store them as a chunk, then as another chunk, etc.
			//This may introduce problems here, but I don't think I want to change that behavior
			//as it may help later down the line of we want to, say, introduce streaming. Streaming individual cubes?
			//Pretty useless. Chunks, however, are a much more useful streamable object.

			//Get chunk position...
			int chx = position.X / Chunk.CHUNK_SIZE;
			int chy = position.Y / Chunk.CHUNK_SIZE;
			int chz = position.Z / Chunk.CHUNK_SIZE;
			//use it to find offset in byte array
			int chunkOffset = chx + sizeInChunks * (chy + sizeInChunks * chz);
			chunkOffset *= Chunk.NUM_CUBES_IN_CHUNK;

			//Get chunk relative cube position...
			//https://stackoverflow.com/questions/11040646/faster-modulus-in-c-c
			//Faster mod when denominator is a power of 2.
			//NOTE: if Chunk.CHUNK_SIZE changes and no longer is a power of two, THIS WILL BREAK EVERYTHING!
			int csx = position.X & (Chunk.CHUNK_SIZE - 1);
			int csy = position.Y & (Chunk.CHUNK_SIZE - 1);
			int csz = position.Z & (Chunk.CHUNK_SIZE - 1);

			int cubeOffset = csx + Chunk.CHUNK_SIZE * (csy + Chunk.CHUNK_SIZE * csz);
			cubeOffset += chunkOffset;

			return cubeOffset;
		}

		//Saves the contents of allBytes to disk.
		public void Save(string folderName)
        {
			if (!Directory.Exists(SAVE_FOLDER + folderName))
				Directory.CreateDirectory(SAVE_FOLDER + folderName);

			// NOTE: we write everything to memory, then we write it to disk. This allows us to use regionFile stream
			using (MemoryStream stream = new MemoryStream())
			{
				SaveToStream(stream);

				regionFile?.Close();
                using (FileStream fs = new FileStream(GetSaveName(folderName), FileMode.Create, FileAccess.Write, FileShare.None))
                {
					fs.Write(stream.GetBuffer());
                }
            }

			regionFile = new FileStream(GetSaveName(folderName), FileMode.Open, FileAccess.Read);
		}

		public void SaveToStream(Stream stream)
		{
			Stopwatch watch = Stopwatch.StartNew();

            stream.Write(BitConverter.GetBytes(VERSION));
            stream.Write(BitConverter.GetBytes(layer));

            //Write unused remaining header bytes
            long remainingBytes = SIZEOF_HEADER - stream.Position;
            stream.Write(new byte[remainingBytes]);

			long chunkOffsetsBegin = stream.Position;
			for (int i = 0; i < numChunks * sizeof(long); i++) 
				stream.WriteByte(0);

			for (int i = 0; i < loadedChunks.Length; i++)
			{
				PalettizedChunk pal;
				if (loadedChunks[i].LoadedState == LoadedState.Unloaded)
				{
					LoadChunk(i);
				}
				if (loadedChunks[i].LoadedState == LoadedState.Palettized)
					pal = loadedChunks[i].palettizedChunk;
				else
				{
					Util.OneDToThreeD(i, new ValuePoint3D(sizeInChunks), out var p);
					pal = PalettizedChunk.Palettize(new ChunkPosition(p.x, p.y, p.z), loadedChunks[i].cubes);
				}

				var prePal = stream.Position;
				using (MemoryStream chunkSaveStream = new MemoryStream())
				{
					pal.Save(chunkSaveStream);
					stream.Write(BitConverter.GetBytes(chunkSaveStream.Length));
					stream.Write(chunkSaveStream.GetBuffer());
				}
				var postPal = stream.Position;

				// Write the chunk offset
				stream.Position = chunkOffsetsBegin + (i * sizeof(long));
				stream.Write(BitConverter.GetBytes(prePal));
				stream.Position = postPal;
			}
			//stream.Write(allBytes);

			watch.Stop();
			Console.WriteLine("Wrote {0} bytes {1:.02}s", stream.Length, watch.Elapsed.TotalSeconds);
        }

		private void LoadChunk(int index)
		{
            regionFile.Position = chunkOffsets[index];

			Span<byte> sizeBytes = stackalloc byte[sizeof(long)];
			regionFile.Read(sizeBytes);
			long size = BitConverter.ToInt64(sizeBytes);
			Span<byte> bytes = stackalloc byte[(int)size];
			regionFile.Read(bytes);

			PalettizedChunk c = new();
			c.Load(bytes);

			loadedChunks[index].loadedState = (int)LoadedState.Palettized;
			loadedChunks[index].palettizedChunk = c;
        }

        public LoadError Load(string folderName)
		{
			if (!File.Exists(GetLoadName(folderName)))
				return LoadError.FileDoesntExist;

			if (regionFile != null) regionFile.Close();
			regionFile = new FileStream(GetLoadName(folderName), FileMode.Open, FileAccess.Read);
			var result = LoadFromStream(regionFile);

			if (result != LoadError.Success)
				return result;

			loaded = true;

			return LoadError.Success;
		}

		public LoadError LoadFromStream(Stream stream)
        {
            Stopwatch watch = Stopwatch.StartNew();

            Console.WriteLine("Reading {0} bytes...", stream.Length);

			var allBytes = new byte[numChunks * SIZEOF_CHUNK];

			using (BinaryReader br = new BinaryReader(stream, Encoding.ASCII, true))
			{
				Version = br.ReadInt32();

				if (Version >= 2)
				{
					var loadedLayer = br.ReadInt32();
					if (loadedLayer != layer)
					{
						OtherError = string.Format("Tried to load a chunk file as layer {0}, but it actually belongs to layer {1}!", layer, loadedLayer);
						return LoadError.Other;
					}
				}

				if (Version < MIN_VERSION)
					return LoadError.InvalidVersion;
				else
				{
					//Discard the rest of the buffer.
					int remainingBytes = (int)(SIZEOF_HEADER - stream.Position);
					br.Read(new byte[remainingBytes], 0, remainingBytes);
				}
			}

			if (Version <= 3)
			{
				stream.Read(allBytes, 0, (int)(numChunks * SIZEOF_CHUNK));

				LinearToChunkMemory(allBytes);
			}
			else
			{
				Span<byte> offsets = stackalloc byte[(int)(sizeof(long) * numChunks)];
				stream.Read(offsets);

				Span<long> longs = MemoryMarshal.Cast<byte, long>(offsets);
				chunkOffsets = longs.ToArray();

				for (int i = 0; i < numChunks; i++)
				{
					LoadChunk(i);
				}
			}

			watch.Stop();
            Console.WriteLine("Done. {0:.02}s", watch.Elapsed.TotalSeconds);

            return LoadError.Success;
		}

        // TODO: this is really slow
        // We store the entire bytes array linearly right now, so we need to convert it into chunks.
        // Eventually we will get rid of allBytes entirely (at least for id accesses), and we will save/load
        // directly into loadedChunks. So this won't be a problem.
		private void LinearToChunkMemory(byte[] allBytes)
		{
            //Console.WriteLine("Transform 1:");

            Stopwatch watch = Stopwatch.StartNew();
            //for (int cz = 0; cz < Chunk.CHUNK_SIZE * sizeInChunks; cz++)
            //{
            //    for (int cy = 0; cy < Chunk.CHUNK_SIZE * sizeInChunks; cy++)
            //    {
            //        for (int cx = 0; cx < Chunk.CHUNK_SIZE * sizeInChunks; cx++)
            //        {
            //            CubePosition pos = new CubePosition(cx, cy, cz, CubePosition.CoordinateSpace.CubeSpace);
            //            Util.ThreeDToOneD(new ValuePoint3D(pos), new ValuePoint3D(Chunk.CHUNK_SIZE * sizeInChunks), out int j);

            //            var chunkPos = ChunkPosition.CubeChunk(pos);

            //            Util.ThreeDToOneD(new ValuePoint3D(chunkPos), new ValuePoint3D(sizeInChunks), out int i);

            //            if (loadedChunks[i].cubes == null)
            //                loadedChunks[i].cubes = new ushort[Chunk.NUM_CUBES_IN_CHUNK];

            //            ushort id = Unsafe.ReadUnaligned<ushort>(ref allBytes[j * sizeof(ushort)]);

            //            Util.ThreeDToOneD(new ValuePoint3D(pos.InChunkSpace()), new ValuePoint3D(Chunk.CHUNK_SIZE), out j);
            //            loadedChunks[i].cubes[j] = id;
            //        }
            //    }
            //}

            //Console.WriteLine("Done. {0}", watch.Elapsed.TotalSeconds);
            Console.WriteLine("Transform 2:");
            watch.Restart();
            for (int chz = 0; chz < sizeInChunks; chz++)
            {
                for (int chy = 0; chy < sizeInChunks; chy++)
                {
                    for (int chx = 0; chx < sizeInChunks; chx++)
                    {
                        Util.ThreeDToOneD(new ValuePoint3D(chx, chy, chz), new ValuePoint3D(sizeInChunks), out int i);
                        loadedChunks[i].cubes = new ushort[Chunk.NUM_CUBES_IN_CHUNK];

                        ChunkPosition chunkPos = new ChunkPosition(chx, chy, chz);

                        for (int cz = 0; cz < Chunk.CHUNK_SIZE; cz++)
                        {
                            for (int cy = 0; cy < Chunk.CHUNK_SIZE; cy++)
                            {
                                for (int cx = 0; cx < Chunk.CHUNK_SIZE; cx++)
                                {
                                    Util.ThreeDToOneD(new ValuePoint3D(cx, cy, cz), new ValuePoint3D(Chunk.CHUNK_SIZE), out int j);
                                    CubePosition pos = new CubePosition(cx, cy, cz, CubePosition.CoordinateSpace.ChunkSpace);
                                    pos = pos.InCubeSpace(chunkPos);
                                    int offset = GetCubeOffset(pos);

                                    ushort id = Unsafe.ReadUnaligned<ushort>(ref allBytes[offset * sizeof(ushort)]);
                                    loadedChunks[i].cubes[j] = id;

									loadedChunks[i].loadedState = (int)LoadedState.Loaded;
                                }
                            }
                        }
                    }
                }
            }
            Console.WriteLine("Done. {0}", watch.Elapsed.TotalSeconds);
        }

		private string GetSaveName(string folderName)
        {
			return SAVE_FOLDER + folderName + "/" + FILE_NAME_CHUNK + layer + EXT_CHUNK;
		}

		private string GetLoadName(string folderName)
        {
			if (!File.Exists(SAVE_FOLDER + folderName + "/" + FILE_NAME_CHUNK + layer + EXT_CHUNK))
				return SAVE_FOLDER + folderName + "/" + FILE_NAME_CHUNK_OLD + managerName + EXT_CHUNK;
			else return SAVE_FOLDER + folderName + "/" + FILE_NAME_CHUNK + layer + EXT_CHUNK;
		}

		public override bool HandleError(LoadError error, string folderName)
        {
            switch (error)
            {
                case LoadError.InvalidVersion:
                    Console.WriteLine("Chunk file could not be loaded. The current file version ({0}) is not supported.", Version);
                    return true;
                case LoadError.FileDoesntExist:
                    Console.WriteLine("Chunk file does not exist.", GetLoadName(folderName));
                    return true;
                case LoadError.Other:
                    Console.WriteLine(OtherError);
                    return true;
                case LoadError.Success:
                    return false;
                default:
                    return true;
            }
        }
	}
}
