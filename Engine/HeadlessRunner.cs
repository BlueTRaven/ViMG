using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.GameStates;

namespace Engine
{
    public class HeadlessRunner
    {
        private Runner runner;

        public void Run()
        {
            this.runner = new Runner();
            var _services = new GameServiceContainer();
            var _content = new ContentManager(_services);
            _content.RootDirectory = "Content";

            runner.Initialize(_content);

            runner.LoadContent();
        }
    }
}
