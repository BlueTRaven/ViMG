using BepuPhysics.Constraints;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Entities;
using ViMG.UIs;

namespace ViMG.Client
{
    [RegisterClientEntity(typeof(Player))]
    public class CEPlayer : ClientEntity
    {
        private Player Player { get { return entity as Player;  } }

        public MenuPlayer menuPlayer;

        public CEPlayer(Entity baseEntity) : base(baseEntity)
        {
            menuPlayer = new MenuPlayer(baseEntity.world.GameStateManager, Player, Player.inventory, Player.craftInventory, Player.accessoryInventory, Player.gearInventory);
            menuPlayer.Close();
            baseEntity.world.GameStateManager.TheIsland.SetMenu(menuPlayer);
        }

        public override void Update()
        {
            base.Update();
            
            if (Main.inputManager.JustPressed(Keys.E))
            {
                if (Player.world.GameStateManager.GetCurrentGameState().GetCurrentMenu() != menuPlayer)
                    Player.world.GameStateManager.GetCurrentGameState().PopMenu();
                else menuPlayer.Toggle();
            }

            if (Player.useTimer <= 0)
            {
                int oldHighlight = menuPlayer.HighlightIndex;
                if (Main.inputManager.JustPressed(Keys.D1))
                {
                    menuPlayer.HighlightIndex = 0;
                }

                if (Main.inputManager.JustPressed(Keys.D2))
                {
                    menuPlayer.HighlightIndex = 1;
                }

                if (Main.inputManager.JustPressed(Keys.D3))
                {
                    menuPlayer.HighlightIndex = 2;
                }

                if (Main.inputManager.JustPressed(Keys.D4))
                {
                    menuPlayer.HighlightIndex = 3;
                }

                if (Main.inputManager.JustPressed(Keys.D5))
                {
                    menuPlayer.HighlightIndex = 4;
                }

                if (Main.inputManager.JustPressed(Keys.D6))
                {
                    menuPlayer.HighlightIndex = 5;
                }

                if (Main.inputManager.JustPressed(Keys.D7))
                {
                    menuPlayer.HighlightIndex = 6;
                }

                if (Main.inputManager.JustPressed(Keys.D8))
                {
                    menuPlayer.HighlightIndex = 7;
                }

                if (oldHighlight != menuPlayer.HighlightIndex)
                {
                    if (Player.inventory.Get(oldHighlight).valid)
                    {
                        Player.inventory.Get(oldHighlight).item.EndHold(Player, Player.inventory, menuPlayer.HighlightIndex);

                        if (Player.inventory.Get(menuPlayer.HighlightIndex).valid)
                            Player.inventory.Get(oldHighlight).item.StartHold(Player, Player.inventory, menuPlayer.HighlightIndex);
                    }
                }
            }

            if (Player.inventory.Get(menuPlayer.HighlightIndex).valid)
                Player.inventory.Get(menuPlayer.HighlightIndex).item.Hold(Player, Player.inventory, menuPlayer.HighlightIndex);
        }
    }
}
