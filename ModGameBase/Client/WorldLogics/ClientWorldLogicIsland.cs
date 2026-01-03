using Engine.Clients.WorldLogics;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;

namespace ModGameBase.Client.WorldLogics
{
    public class ClientWorldLogicIsland : ClientWorldLogic
    {
        public ClientWorldLogicIsland()
        {
            this.skybox = new Skybox
            {
                Day = Main.assetsManager.GetAsset<Texture2D>("skybox_day"),
                Weather = Main.assetsManager.GetAsset<Texture2D>("skybox_stormy"),
                Night = Main.assetsManager.GetAsset<Texture2D>("skybox_night"),
            };
        }
    }
}
