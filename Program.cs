using System;

namespace ViMG
{
	class Program
	{
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            using (var game = new Main(args))
                game.Run();
        }
    }
}
