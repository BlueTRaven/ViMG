using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.IMGUIImpl;

namespace Engine
{
    public static class GlobalState
    {
        public static ArgParser Args = new ArgParser();
        public static ViMGAssetsManager assetsManager;
        public static RegistryService Registry;

        public const int SEED = 1338;
        public static Random random = new Random(SEED);

#if DEBUG
		public static bool Debug = true;
#else
        public static bool Debug = false;
#endif

        public static SessionInformation SessionInformation;
        public static SessionIO SessionIO;

        public static Thread MainThread;

        public const bool GEN_BROAD = true;
        public const bool GEN_DETAIL = true;
        public const bool GEN_CAVES = false;
        public const bool GEN_CUBE_POST_DETAIL = false;

        [ConsoleCommandVar("random_spawns", "enable random entity spawning")]
        public static bool ENABLE_ENT_SPAWNING = false;
        public const float RANDOM_UPDATES_TIME = 8f / 60f;
        [ConsoleCommandVar("random_cube_updates", "enable random cube updates (grass spreading, etc)")]
        public static bool ENABLE_RANDOM_UPDATES = false;
        public const int RANDOM_UPDATES_PER_CHUNK = 1;

        public const bool MULTITHREADING = true;
        public const bool MULTITHREAD_BROAD_PHASE = MULTITHREADING && true;
        public const bool MULTITHREAD_LOADING = MULTITHREADING && true;
        public const bool MULTITHREAD_MESHING = MULTITHREADING && true;
        public const bool MULTITHREAD_UPLOADMESH = MULTITHREADING && true;

        public static double Time;

        public static bool Exit = false;
    }
}
