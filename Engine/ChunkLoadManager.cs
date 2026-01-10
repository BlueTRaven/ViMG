using BepuPhysics.Constraints;
using BepuUtilities.Memory;
using BrUtility.Ported;
using Engine.ChunkStuff;
using Engine.Networking.Messages;
using Microsoft.VisualBasic;
using Microsoft.Xna.Framework;
using SharpDX.Direct3D11;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ViMG.ChunkStuff;
using ViMG.Entities;
using ViMG.GameStates;
using ViMG.IMGUIImpl;

namespace ViMG
{
    // TODO: are copies correctly returned if meshing is interrupted or unused?
    public class ChunkLoadManager : IDisposable
    {
		private enum LoadingState : byte
        {
			Unloaded,
			Enqueued,
			Loading,
			Loaded,
        }

		private readonly ChunkMesher? chunkMesher;
		private readonly ChunkManager chunkManager;
		private readonly EntityManager entityManager;
		private readonly ChunkManagerIO chunkIO;
        private readonly EntityManagerIO entIO;
        // Loaded chunks per player [player][pos]
		private LoadingState[][] loadedChunks;
		//private bool[][] loadedChunksAttribution = new bool[World.MAX_PLAYERS][];
		private List<ChunkPosition> unloadChunks = new List<ChunkPosition>();
		private IEnumerable<ChunkPosition> gettableLoadedChunks;
		private IEnumerable<ChunkPosition>[] gettableLoadedChunksPlayer = new IEnumerable<ChunkPosition>[World.MAX_PLAYERS];

		private bool hasChanged = false;

		private const float DISTANCE_UNLOAD_CHECK_TIME = 4;
		private float distanceUnloadCheckTimer;

		private struct QueuedChunk
		{
            public int player;
			public ChunkPosition position;
            public CopiedChunkManager.CopiedChunkData? copyData;
			//public Task<CopiedChunkData> copyTask;
		}

		private PriorityQueue<QueuedChunk> queue = new(true, (queuedChunk) =>
		{
			return (int)(Main.camera.Position - queuedChunk.position.InWorldSpace()).Length();
		});

		private List<QueuedChunk> copyingChunks = new();
		
		// Double buffers and the currently used buffer.
		private List<QueuedChunk> waitingToFinishMeshingChunks1 = new();
        private List<QueuedChunk> waitingToFinishMeshingChunks2 = new();
		private List<QueuedChunk> waitingToFinishMeshingChunks;

		public ChunkLoadManager(ChunkMesher? chunkMesher, ChunkManager chunkManager, EntityManager entityManager, ChunkManagerIO chunkIO, EntityManagerIO entIO)
		{
			ThreadPool.SetMaxThreads(8, 8);

            loadedChunks = new LoadingState[World.MAX_PLAYERS][];
			for (int i = 0; i < World.MAX_PLAYERS; i++) 
			{
                loadedChunks[i] = new LoadingState[chunkManager.SizeInChunksXZ * chunkManager.SizeInChunksXZ * chunkManager.SizeInChunksXZ];
                //loadedChunksAttribution[i] = new bool[chunkManager.SizeInChunksXZ * chunkManager.SizeInChunksXZ * chunkManager.SizeInChunksXZ];
				//Array.Fill(loadedChunksAttribution[i], false);
			}

			this.chunkMesher = chunkMesher;
			this.chunkManager = chunkManager;
			this.entityManager = entityManager;

			this.chunkIO = chunkIO;
            this.entIO = entIO;

			waitingToFinishMeshingChunks = waitingToFinishMeshingChunks1;
        }

		public void Update(double deltaTime, World world)
		{
            if (Main.inputManager.JustPressed(Microsoft.Xna.Framework.Input.Keys.M))
            {
                FlushLoadQueue(world);
            }
            using var zone = TracyImpl.Tracy.BeginZone();

			distanceUnloadCheckTimer -= (float)deltaTime;

			if (distanceUnloadCheckTimer <= 0)
			{
				distanceUnloadCheckTimer = DISTANCE_UNLOAD_CHECK_TIME;
				LoadAroundTarget(world);
			}
            
            ProcessLoadQueue(world);

            if (hasChanged)
            {
                List<ChunkPosition>[] glcP = new List<ChunkPosition>[World.MAX_PLAYERS];
                List<ChunkPosition> glc = new List<ChunkPosition>();
                for (int j = 0; j < loadedChunks[0].Length; j++)
                {
                    bool any = false;
                    for (int i = 0; i < World.MAX_PLAYERS; i++)
                    {
                        LoadingState item = loadedChunks[i][j];
                        if (item == LoadingState.Loaded)
                        {
                            Util.OneDToThreeD(j, new ValuePoint3D(world.sizeInChunks), out var point);
                            var chunkPos = new ChunkPosition(point.x, point.y, point.z);
                            if (!any)
                            {
                                glc.Add(chunkPos);

                                any = true;
                            }
                            if (glcP[i] == null) glcP[i] = new List<ChunkPosition>();
                            glcP[i].Add(chunkPos);
                        }
                    }
                }
                gettableLoadedChunks = glc;
                for (int i = 0; i < World.MAX_PLAYERS; i++)
                {
                    gettableLoadedChunksPlayer[i] = glcP[i];
                }
            }

			hasChanged = false;
		}

		public IEnumerable<ChunkPosition> GetLoaded()
        {
			return gettableLoadedChunks;
        }

		public IEnumerable<ChunkPosition> GetLoaded(int playerId)
		{
			return gettableLoadedChunksPlayer[playerId];
		}

		public bool IsLoaded(ChunkPosition position)
        {
			if (!chunkManager.IsInWorldBounds(position))
				return false;

			Util.ThreeDToOneD(new ValuePoint3D(position.X, position.Y, position.Z), new ValuePoint3D(chunkManager.SizeInChunksXZ), out int j);
            for (int i = 0; i < World.MAX_PLAYERS; i++)
            {
                if (loadedChunks[i][j] == LoadingState.Loaded)
                    return true;
            }

            return false;
        }

        public void PrintLoadState(ChunkPosition position)
        {
            Console.WriteLine("{0}:", position);
            Util.ThreeDToOneD(new ValuePoint3D(position.X, position.Y, position.Z), new ValuePoint3D(chunkManager.SizeInChunksXZ), out int j);

            for (int i = 0; i < World.MAX_PLAYERS; i++)
            {
                Console.WriteLine(loadedChunks[i][j].ToString());
            }
        }

		//Loads the entirety of the loading queue at once.
		//It's best practice to use this before saving, so as not to miss loading chunks!
		public void FlushLoadQueue(World world)
		{
            using var zone = TracyImpl.Tracy.BeginZone();

			foreach (var queuedChunk in queue.GetEnumerable())
			{
                chunkManager.CopyManager.StartCopyChunk(queuedChunk.position, world.EntityManager);
				//queuedChunk.copyTask.Start();
			}

            chunkManager.CopyManager.FinishCopyChunks();
			foreach (var queuedChunk in queue.GetEnumerable()) 
			{
                //queuedChunk.copyTask.Wait();

                var copy = chunkManager.CopyManager.GetCopy(queuedChunk.position);
				chunkMesher?.RenderMesher?.AddToNextBatch(queuedChunk.position, copy);
                chunkMesher?.CollisionMesher?.AddToNextBatch(world, queuedChunk.position, copy);
            }

            chunkMesher?.RenderMesher?.BeginFlush();
			chunkMesher?.CollisionMesher?.BeginFlush(world);
			chunkMesher?.RenderMesher?.FinishFlush();
            chunkMesher?.CollisionMesher?.FinishFlush();

            int max = queue.Count;
            GameStateTheIsland.ProgressMax = max;

			while (queue.Count > 0)
			{
                //GameStateTheIsland.ProgressMin = max - queue.Count;

				// TODO: sometimes there's stuff in the queue that apparently never gets meshed properly. Why is this?

				QueuedChunk queuedChunk = queue.Dequeue();

                entIO.Deserialize(world, queuedChunk.position);

                Util.ThreeDToOneD(new ValuePoint3D(queuedChunk.position.X, queuedChunk.position.Y, queuedChunk.position.Z), new ValuePoint3D(chunkManager.SizeInChunksXZ), out int i);
				loadedChunks[queuedChunk.player][i] = LoadingState.Loaded;

				hasChanged = true;
			}

			waitingToFinishMeshingChunks.Clear();

            CopiedChunkPool.Verify();

            if (hasChanged)
			{
                List<ChunkPosition>[] glcP = new List<ChunkPosition>[World.MAX_PLAYERS];
                List<ChunkPosition> glc = new List<ChunkPosition>();
                for (int j = 0; j < loadedChunks.Length; j++)
                {
                    bool any = false;
                    for (int i = 0; i < World.MAX_PLAYERS; i++)
                    {
                        LoadingState item = loadedChunks[i][j];
                        if (item == LoadingState.Loaded)
                        {
                            Util.OneDToThreeD(j, new ValuePoint3D(world.sizeInChunks), out var point);
                            var chunkPos = new ChunkPosition(point.x, point.y, point.z);
                            if (!any)
                            {
                                glc.Add(chunkPos);

                                any = true;
                            }
                            glcP[i].Add(chunkPos);
                        }
                    }
                }
                gettableLoadedChunks = glc;
                for (int i = 0; i < World.MAX_PLAYERS; i++)
                {
                    gettableLoadedChunksPlayer[i] = glc;
                }
            }

			hasChanged = false;
		}

		public void ProcessLoadQueue(World world)
        {
            using var zone = TracyImpl.Tracy.BeginZone();

			using (var zoneSort = TracyImpl.Tracy.BeginZone(name: "Sort"))
			{
				if (queue.Count > 30)
				{
					queue.Sort();
				}
			}

			int currentNum = 0;

			var zoneQueue = TracyImpl.Tracy.BeginZone(name: "Queue");
			while (queue.Count > 0 && currentNum < IMGUISettings.CopiesPerFrame)
            {
				QueuedChunk queuedChunk = queue.Dequeue();

				//Chunk has been told to unload before we got to it.
				Util.ThreeDToOneD(new ValuePoint3D(queuedChunk.position.X, queuedChunk.position.Y, queuedChunk.position.Z), new ValuePoint3D(chunkManager.SizeInChunksXZ), out int i);
				if (loadedChunks[queuedChunk.player][i] == LoadingState.Unloaded)
				{
					// TODO: do we need to stop things?
					continue;
				}
				else if (loadedChunks[queuedChunk.player][i] == LoadingState.Enqueued)
				{
                    //if (chunkMesher != null)
                    //{
                    //    bool render = chunkMesher.RenderMesher?.IsMeshed(queuedChunk.position) ?? true;
                    //    bool collision = chunkMesher.CollisionMesher?.IsMeshed(queuedChunk.position) ?? true;

                    //    if (render && collision) continue;
                    //}

                    chunkManager.CopyManager.StartCopyChunk(queuedChunk.position, world.EntityManager);
                    copyingChunks.Add(queuedChunk);

                    currentNum++;
                }
                else
                {
                    IMGUIConsole.Assert(false);
                }
			}
			zoneQueue.End();

			var zoneWait = TracyImpl.Tracy.BeginZone(name: "WaitForCopy");
            chunkManager.CopyManager.FinishCopyChunks();

            for (int j = 0; j < copyingChunks.Count; j++)
			{
                QueuedChunk copyingChunk = copyingChunks[j];
                CopiedChunkManager.CopiedChunkData copy = chunkManager.CopyManager.GetCopy(copyingChunk.position);
                copyingChunk.copyData = copy;

                Util.ThreeDToOneD(new ValuePoint3D(copyingChunk.position), new ValuePoint3D(chunkManager.SizeInChunksXZ), out int i);
                IMGUIConsole.Assert(loadedChunks[copyingChunk.player][i] == LoadingState.Enqueued);
                loadedChunks[copyingChunk.player][i] = LoadingState.Loading;

                // Only enqueue rendering mesh for local player
                if (copyingChunk.player == world.localPlayerIndex)
                    chunkMesher?.RenderMesher?.AddToNextBatch(copyingChunk.position, copy);
                chunkMesher?.CollisionMesher?.AddToNextBatch(world, copyingChunk.position, copy);

				waitingToFinishMeshingChunks.Add(copyingChunk);
            }

			copyingChunks.Clear();
			zoneWait.End();

            zoneWait = TracyImpl.Tracy.BeginZone(name: "WaitForMeshingFinished");
            // Double buffered. If a chunk is not finished, it is moved to the other buffer, and the buffers are swapped each ProcessLoadQueue call.
            var otherBuffer = waitingToFinishMeshingChunks == waitingToFinishMeshingChunks1 ? waitingToFinishMeshingChunks2 : waitingToFinishMeshingChunks1;
			foreach (QueuedChunk queuedChunk in waitingToFinishMeshingChunks)
			{
                // If the chunk mesher is null (we're running headless),
                // if it's finished meshing,
                // or if we're not the local player,
                //  and the collision meshing is done,
                // we're done.
                // Non-local players will only have collision meshed.
                bool isDone = false;
                if (chunkMesher == null)
                {
                    isDone = true;
                }
                else
                {
                    if (queuedChunk.player != world.localPlayerIndex && (chunkMesher?.CollisionMesher?.IsMeshed(queuedChunk.position) ?? true))
                        isDone = true;
                    if ((chunkMesher?.RenderMesher?.IsMeshed(queuedChunk.position) ?? true) && (chunkMesher?.CollisionMesher?.IsMeshed(queuedChunk.position) ?? true))
                        isDone = true;
                }

                if (isDone)
                {
                    Util.ThreeDToOneD(new ValuePoint3D(queuedChunk.position), new ValuePoint3D(chunkManager.SizeInChunksXZ), out int j);

                    loadedChunks[queuedChunk.player][j] = LoadingState.Loaded;

                    entIO.Deserialize(world, queuedChunk.position);

                    // Sync chunk loading to other players
                    // NOTE: this is here, after deserialization, as this sends over chunk meshing data too
                    // (which requires entities to be initialized)
                    var peer = Main.gameStateManager.TheIsland.netManagerServer?.GetPeer(queuedChunk.player);
                    if (peer != null)
                    {
                        var sync = new SyncChunk.ChunkToSync
                        {
                            chunkPosition = queuedChunk.position,
                            ids = queuedChunk.copyData?.GetAllIds(),
                        };
                        Main.gameStateManager.TheIsland.netManagerServer.SendMessageToPeer(SyncChunk.Instance, peer, sync);
                    }

                    hasChanged = true;
                }
                else
                {
                    //not finished loading; re-queue
                    otherBuffer.Add(queuedChunk);
                }

            }

            waitingToFinishMeshingChunks.Clear();
			waitingToFinishMeshingChunks = otherBuffer;

            zoneWait.End();
		}

        // Loads a single chunk, blocking until it is fully loaded.
        // Always attributed to local player. Use for singleplayer and server only.
        public void LoadChunk(World world, ChunkPosition position)
        {
            using var zone = TracyImpl.Tracy.BeginZone();

            Util.ThreeDToOneD(new ValuePoint3D(position.X, position.Y, position.Z), new ValuePoint3D(chunkManager.SizeInChunksXZ), out int i);

            if (chunkManager.IsInWorldBounds(position) && loadedChunks[0][i] == LoadingState.Unloaded)
            {
                loadedChunks[0][i] = LoadingState.Loaded;
                //loadedChunksAttribution[world.localPlayerIndex][i] = true;
                //queue.EnqueueWithoutSorting(position);

                chunkMesher?.RenderMesher.ImmediatelyMesh(position, chunkManager.CopyManager, world.EntityManager);
                chunkMesher?.CollisionMesher.ImmediatelyMesh(world, position, chunkManager.CopyManager, world.EntityManager);

                entIO.Deserialize(world, position);
                //CopiedChunkData copy = CopiedChunkPool.MakeCopy(world, bufferPool, position);
                //chunkManager.RenderMesher.AddToNextBatch(world, position, copy);
                //chunkManager.CollisionMesher.AddToNextBatch(world, position, copy);

                hasChanged = true;
            }
		}

		// Forcibly loads around the target.
		// Always attributed to local player. Use for singleplayer and server only.
		public void LoadAroundTarget(World world, ChunkPosition target, int? tempRenderDistance = null) 
		{
			if (Main.gameStateManager.netMode == GameStateManager.NetworkingMode.Client)
                IMGUIConsole.Assert(false);

            int useRenderDistance = tempRenderDistance.GetValueOrDefault(Options.RenderDistance);

            int minx = -useRenderDistance;
            int maxx = useRenderDistance;
            int minz = -useRenderDistance;
            int maxz = useRenderDistance;

            for (int z = minz; z < maxz; z++)
            {
                for (int y = -useRenderDistance; y <= useRenderDistance; y++)
                {
                    for (int x = minx; x <= maxx; x++)
                    {
                        var pos = target + new ChunkPosition(x, y, z);

                        Vector2 distH = new Vector2(pos.X, pos.Z) - new Vector2(target.X, target.Z);

                        if (distH.Length() < Options.RenderDistance && chunkManager.IsInWorldBounds(pos))
                        {
                            Util.ThreeDToOneD(new ValuePoint3D(pos.X, pos.Y, pos.Z), new ValuePoint3D(chunkManager.SizeInChunksXZ), out int i);
                            if (loadedChunks[0][i] == LoadingState.Unloaded)
                            {
                                loadedChunks[0][i] = LoadingState.Enqueued;
                                //loadedChunksAttribution[world.localPlayerIndex][i] = true;

                                //var context = new CopyChunkTaskContext
                                //{
                                //    cubeView = world.ChunkManager.CubeView,
                                //    entityManager = world.EntityManager,
                                //    sizeInCubes = world.sizeInCubes,
                                //    pool = chunkMesher.bufferPool,
                                //    position = pos,
                                //};
                                //var task = new Task<CopiedChunkData>(CopyChunkTaskFn, context);
                                // NOTE: tasks are not immediately started.
                                queue.EnqueueWithoutSorting(new QueuedChunk
                                {
                                    //copyTask = task,
                                    position = pos,
                                });

                                hasChanged = true;
                            }
                        }
                    }
                }
            }
        }

		// Load around players.
		// Chunks that are within a player's load distance are attributed to them. (Even if they were not the initial loader).
		public unsafe void LoadAroundTarget(World world, int iteration = 4, int? tempRenderDistance = null)
		{
			using var zone = TracyImpl.Tracy.BeginZone();

            int minx = 0;
            int maxx = 0;
            int minz = 0;
            int maxz = 0;

            int useRenderDistance = tempRenderDistance.GetValueOrDefault(Options.RenderDistance);

            if (iteration == 0)
            {
                minx = -useRenderDistance;
                maxx = 0;
                minz = -useRenderDistance;
                maxz = 0;
            }
            else if (iteration == 1)
            {
                minx = 0;
                maxx = useRenderDistance;
                minz = -useRenderDistance;
                maxz = 0;
            }
            else if (iteration == 2)
            {
                minx = 0;
                maxx = useRenderDistance;
                minz = 0;
                maxz = useRenderDistance;
            }
            else if (iteration == 3)
            {
                minx = -useRenderDistance;
                maxx = 0;
                minz = 0;
                maxz = useRenderDistance;
            }
            else if (iteration == 4)
            {
                minx = -useRenderDistance;
                maxx = useRenderDistance;
                minz = -useRenderDistance;
                maxz = useRenderDistance;
            }

            foreach (Player? player in world.player)
			{
				if (player == null || !player.IsInitialized) continue;
                // We don't care about players other than the local one if we're a client

                ChunkPosition target = ChunkPosition.WorldSpaceChunk(player.Position);

                for (int z = minz; z < maxz; z++)
                {
                    for (int y = -useRenderDistance; y <= useRenderDistance; y++)
                    {
                        for (int x = minx; x <= maxx; x++)
                        {
                            var pos = target + new ChunkPosition(x, y, z);

                            Vector2 distH = new Vector2(pos.X, pos.Z) - new Vector2(target.X, target.Z);

                            Util.ThreeDToOneD(new ValuePoint3D(pos.X, pos.Y, pos.Z), new ValuePoint3D(chunkManager.SizeInChunksXZ), out int i);

                            if (distH.Length() < Options.RenderDistance && chunkManager.IsInWorldBounds(pos))
                            {
                                if (loadedChunks[player.playerIndex][i] == LoadingState.Unloaded)
                                {
                                    loadedChunks[player.playerIndex][i] = LoadingState.Enqueued;

                                    queue.EnqueueWithoutSorting(new QueuedChunk
                                    {
                                        player = player.playerIndex,
                                        position = pos,
                                    });

                                    hasChanged = true;
                                }
                            }
                        }
                    }
                }
            }

			var zoneUnload = TracyImpl.Tracy.BeginZone(name: "Unload");
            for (int i = 0; i < loadedChunks.Length; i++)
            {
                Util.OneDToThreeD(i, new ValuePoint3D(chunkManager.SizeInChunksXZ), out var point);
			
                bool remove = true;

                // If in range of any player don't unload
				foreach (Player? player in world.player)
				{
					if (player == null || player.TimeInitialized == 0) continue;
					// We don't care about players other than the local one if we're a client

                    ChunkPosition baseChunkPos = ChunkPosition.WorldSpaceChunk(player.Position);

                    Vector2 dist = new Vector2(point.x, point.z) - new Vector2(baseChunkPos.X, baseChunkPos.Z);

					float len = dist.Length();

					if (len < Options.RenderDistance + 2)
						remove = false;
					else
                    {
                        loadedChunks[player.playerIndex][i] = LoadingState.Unloaded;
                    }
                }

				if (remove)
					unloadChunks.Add(new ChunkPosition(point.x, point.y, point.z));
			}

            // TODO: this only unloads chunks if ALL players are no longer near them. This is not great
			foreach (ChunkPosition pos in unloadChunks)
			{
				Util.ThreeDToOneD(new ValuePoint3D(pos.X, pos.Y, pos.Z), new ValuePoint3D(chunkManager.SizeInChunksXZ), out int j);

                bool allUnloaded = true;
                for (int i = 0; i < World.MAX_PLAYERS; i++)
                {
                    if (loadedChunks[i][j] == LoadingState.Loaded)
                    {
                        allUnloaded = false;
                        break;
                    }
                }

                if (allUnloaded)
                {
                    // Don't bother serializing on client - we never deserialize things
                    if (Main.gameStateManager.netMode != GameStateManager.NetworkingMode.Client)
                        entIO.Serialize(pos);

                    entityManager.UnloadInChunk(pos);
                    chunkMesher?.Unload(pos);
                }

                for (int i = 0; i < World.MAX_PLAYERS; i++)
                {
                    loadedChunks[i][j] = LoadingState.Unloaded;
                }

				hasChanged = true;
			}

			unloadChunks.Clear();
			zoneUnload.End();
		}

        private struct CopyChunkTaskContext
        {
			public CubeView cubeView;
            public EntityManager entityManager;
            public int sizeInCubes;
			public BufferPool pool;
			public ChunkPosition position;
        }

        private CopiedChunkData CopyChunkTaskFn(object context)
		{
			var copyContext = (CopyChunkTaskContext)context;

			var copy = CopiedChunkPool.MakeCopy(copyContext.cubeView, copyContext.entityManager, copyContext.sizeInCubes, copyContext.pool, copyContext.position);
            copy.loadAroundTarget = true;
            return copy;
		}

		public void MarkDirty(ChunkPosition chunkPosition)
		{
			chunkMesher?.MarkChunkDirty(chunkPosition);
        }

		public void Unload(ChunkPosition chunkPosition)
		{
            Util.ThreeDToOneD(new ValuePoint3D(chunkPosition.X, chunkPosition.Y, chunkPosition.Z), new ValuePoint3D(chunkManager.SizeInChunksXZ), out int j);

            for (int i = 0; i < World.MAX_PLAYERS; i++)
            {
                if (loadedChunks[i][j] == LoadingState.Loaded)
                {
                    //chunkIO.SerializeChunk(chunks, pos);
                    //entIO.Serialize(chunkPosition);

                    entityManager.UnloadInChunk(chunkPosition);
                    chunkMesher?.Unload(chunkPosition);
                    break;
                }
            }

            for (int i = 0; i < World.MAX_PLAYERS; i++)
            {
                loadedChunks[i][j] = LoadingState.Unloaded;
            }
			//for (int j = 0; j < World.MAX_PLAYERS; j++)
			//	loadedChunksAttribution[j][i] = false;

            hasChanged = true;

            //chunkMesher?.RenderMesher.FinishFlush();
            //chunkMesher?.CollisionMesher.FinishFlush();

            //chunkMesher?.Unload(chunkPosition);

            //Util.ThreeDToOneD(new ValuePoint3D(chunkPosition.X, chunkPosition.Y, chunkPosition.Z), new ValuePoint3D(chunkManager.SizeInChunksXZ), out int i);

            //loadedChunksFastLookup[i] = LoadingState.Unloaded;
            //loadedChunks[chunkPosition] = LoadingState.Unloaded;
        }

		public void UnloadAll()
		{
            using var zone = TracyImpl.Tracy.BeginZone();

            //TODO: there may still be meshes in the queue.
            chunkMesher?.RenderMesher?.FinishFlush();
            chunkMesher?.CollisionMesher.FinishFlush();
            CopiedChunkPool.Verify();

            chunkMesher?.RenderMesher?.UnloadAll();
            chunkMesher?.CollisionMesher.UnloadAll();
			
			entityManager.UnloadAll();

            for (int i = 0; i < World.MAX_PLAYERS; i++)
                Array.Fill(loadedChunks[i], LoadingState.Unloaded);
		}

        public void UnloadAllFor(int playerIndex)
        {
            if (Main.gameStateManager.TheIsland.netManagerServer.uniqueNetPlayers == 0)
            {
                UnloadAll();
                return;
            }
            
            for (int j = 0; j < chunkManager.SizeInChunksXZ; j++)
            {
                bool anyLoaded = false;
                for (int i = 0; i < World.MAX_PLAYERS; i++)
                {
                    if (i == playerIndex)
                    {
                        loadedChunks[i][j] = LoadingState.Unloaded;
                    }

                    if (loadedChunks[i][j] == LoadingState.Loaded)
                    {
                        anyLoaded = true;
                    }
                }

                if (!anyLoaded)
                {
                    Util.OneDToThreeD(j, new ValuePoint3D(chunkManager.SizeInChunksXZ), out var pos);
                    Unload(new ChunkPosition(pos.x, pos.y, pos.z));
                }
            }
            Array.Fill(loadedChunks[playerIndex], LoadingState.Unloaded);
        }

        public void Dispose()
        {
            chunkMesher?.Dispose();
			entityManager.Dispose();
        }
    }
}
