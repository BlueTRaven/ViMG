using Engine;
using SharpDX.Direct3D9;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using ViMG.IMGUIImpl;

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
            GlobalState.Args.ParseArgs(args);

            // NOTE: This overrides debugger exception behavior, so don't do it if a debugger is attached.
            if (!Debugger.IsAttached)
                AppDomain.CurrentDomain.UnhandledException += UnhandledException;

            try
            {
                HeadlessRunner runner = new HeadlessRunner();
                runner.Run();
                //using (var game = new Main(args))
                //    game.Run();
            }
            catch (Exception e)
            {
                OnException(e);
            }
        }

        private static void UnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            OnException(args.ExceptionObject as Exception);
        }
        
        private static void OnException(Exception e)
        {
            string dir = string.Format("crash_logs/{0}", DateTime.Now.ToString("yyyy-MM-dd"));
            Directory.CreateDirectory(dir);
            var history = IMGUIConsole.GetHistory();
            StringBuilder sb = new StringBuilder();
            foreach (string line in history.Slice())
                sb.AppendLine(line);
            sb.AppendLine(e.ToString());
            File.WriteAllText(string.Format("{0}/crash_log-{1}.txt", dir, DateTime.Now.ToString("hh-mm-ss")), sb.ToString());
        }
    }
}
