using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.IMGUIImpl;

namespace Engine
{
    public class ArgParser
    {
        private static Logger Logger = Logger.InitLogger("ArgParser", true, Logger.LogLevel.Warn);

        public string startMode = "MainMenu";
        public string sessionFile = "session.ses";
        public string networkingMode = "Singleplayer";
        public bool startPaused = false;
        public Point? windowPosition = null;

        public bool dedicatedServer = false;
        public string? playerName;

        public void ParseArgs(string[] args)
        {
            int currentArgI = 0;

            while (currentArgI < args.Length)
            {
                string currentArg = args[currentArgI];

                if (currentArg == "--mode" || currentArg == "-m")
                {
                    startMode = NextArg(args, ref currentArgI);
                }
                else if (currentArg == "--session" || currentArg == "-s")
                {
                    sessionFile = NextArg(args, ref currentArgI);
                }
                else if (currentArg == "--netmode" || currentArg == "-n")
                {
                    networkingMode = NextArg(args, ref currentArgI);
                }
                else if (currentArg == "--startpaused" || currentArg == "-p")
                {
                    startPaused = true;
                }
                else if (currentArg == "--showconsole" || currentArg == "-c")
                {
                    IMGUISettings.ShowConsole = true;
                    Options.ShowConsole = true;
                }
                else if (currentArg == "--windowpos")
                {
                    var posX = NextArg(args, ref currentArgI);
                    var posY = NextArg(args, ref currentArgI);
                    windowPosition = new Point(int.Parse(posX), int.Parse(posY));
                }
                else if (currentArg == "--dedicated")
                {
                    dedicatedServer = true;
                }
                else if (currentArg == "--playerName")
                {
                    playerName = NextArg(args, ref currentArgI);
                }
                else
                {
                    Logger.Log(Logger.LogLevel.Warn, "Invalid switch: {0}", currentArg);
                }

                currentArgI += 1;
            }
        }

        private string NextArg(string[] args, ref int currentArgIndex)
        {
            if (args.Length > currentArgIndex + 1)
            {
                currentArgIndex += 1;
                return args[currentArgIndex];
            } 
            else
            {
                throw new Exception("Expected additional argument");
            }
        }
    }
}
