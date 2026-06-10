using BepuPhysics.Constraints;
using Engine.Networking;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.GameStates;

namespace Engine.Server
{
    // We should eventually move all server state to here instead of using GameStateManager.
    // There are a couple of reasons for this. First of all, the server won't really have different game states. It'll be in the game or choosing a save to load.
    // Additionally, we need to add support for multiple worlds at a time. 
    // Some things to think about: can we run multiple of the same layer at the same time? Or do we only allow one?
    // Only allowing one would allow me to slot each layer into an index, and you wouldn't have to manage or look up which layer belongs in which slot.
    public class ServerState
    {
        private static Logger Logger = Logger.InitLogger("ServerState", true, Logger.LogLevel.Info);

        public static string LoadMessage;
        public static int LoadMin;
        public static int LoadMax;

        private WorldTask?[] worldTasks = new WorldTask?[byte.MaxValue];
        private World?[] worlds = new World?[byte.MaxValue];

        public NetworkManager? NetworkManager;

        public bool Paused;

        // Loads or creates a world.
        public WorldTask? BeginLoadWorld(string worldName, int layer)
        {
            using var zone = ViMG.TracyImpl.Tracy.BeginZone();

            var invalidChars = System.IO.Path.GetInvalidFileNameChars();
            if (string.IsNullOrWhiteSpace(worldName) || worldName.Any(c => invalidChars.Contains(c)))
            {
                Logger.Log(Engine.Logger.LogLevel.Error, "'{0}' is an invalid world file name.", worldName);
                return null;
            }

            Debug.Assert(worldTasks[layer] == null && worlds[layer] == null);
            WorldTask task = new WorldTask(worldName, layer);
            worldTasks[layer] = task;

            return task;
        }

        public void NetPoll()
        {
            for (int i = 0; i < worldTasks.Length; i++)
            {
                if (worldTasks[i] != null && worlds[i] == null && worldTasks[i].task.IsCompleted)
                {
                    worlds[i] = worldTasks[i].task.Result;
                    worldTasks[i] = null;

                    if (GameStateTheIsland.PauseWhenWorldLoaded)
                    {
                        Paused = true;
                        // Shoud we continue to execute here or just return?
                    }
                }
            }
        }

        public void Update()
        {
            for (int i = 0; i < worldTasks.Length; i++)
            {
                if (worldTasks[i] != null)
                {

                }
            }
        }
    }
}
