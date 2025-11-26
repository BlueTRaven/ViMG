using System;
using System.IO;

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
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;

            using (var game = new Main(args))
                game.Run();
        }

        static void UnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            File.WriteAllText(string.Format("crash_log-{0}.txt", DateTime.Now.ToShortDateString()), args.ExceptionObject.ToString());
        }
    }
}
