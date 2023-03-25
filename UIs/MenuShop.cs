using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Entities;
using ViMG.GameStates;
using ViMG.Items;

namespace ViMG.UIs
{
    public class MenuShop : Menu
    {
        public struct ShopStockedItem
        {
            public ItemInstance item;
            public int value;
        }

        private Player player;
        private ItemInstance held;
        private ShopStockedItem[] stock;

        private TextHelper.FontInfo fi = new TextHelper.FontInfo(Main.assetsManager.GetAsset<SpriteFont>("fira_mono_sml"), 1, true);
        private UI.ItemSlot[] inventoryItemSlots = new UI.ItemSlot[Player.INVENTORY_ROWS * Player.INVENTORY_COLUMNS];

        public MenuShop(GameStateManager gsManager, Player player, ShopStockedItem[] stock) : base(gsManager)
        {
            this.player = player;
            this.stock = stock;
        }

        public override void OnOpen()
        {
            Main.MouseControl = true;
            Main.DrawCursor = true;
        }

        public override void OnClose()
        {
            Main.MouseControl = false;
            Main.DrawCursor = false;
        }

        public override void Update(GraphicsDevice device, double deltaTime)
        {
            base.Update(device, deltaTime);

            UI.Start();

            UI.StartParent(new Vector2(MARGIN, MARGIN + 32));
            
            MenuHelper.DoPlayerInventory(player, player.GetInventory(), ref held, Player.INVENTORY_ROWS, Player.INVENTORY_COLUMNS, 18 * 2f, 2f, inventoryItemSlots);
            
            UI.EndParent();
            
            float width = Options.CurrentWindowResolution.X / 2f;
            float height = Options.CurrentWindowResolution.X / 2f * (1f /Options.WindowAspectRatio);

            UI.StartParent(new Vector2(Options.CurrentWindowResolution.X / 2f - width / 2f, Options.CurrentWindowResolution.Y / 2f - height / 2f));

            UI.MakePanel(new UI.PanelConstructionParameters(new RectangleF(0, 0, width, height), Color.White, MenuHelper.MainPanelNS));

            UI.StartParent(new Vector2(16));

            var buttonParameters = MenuHelper.ButtonParameters;
            buttonParameters.bounds.Size = new Size(buttonParameters.bounds.Size.Width * SCALE, 
                buttonParameters.bounds.Size.Height * SCALE);

            for (int i = 0; i < stock.Length; i++)
            {
                UI.StartParent(new Vector2(0, i * 18 * SCALE + i * 2f));

                ShopStockedItem stocked = stock[i];
                UI.MakeItemSlot(UI.MakeButton(buttonParameters), stocked.item);
                UIWidgets.MakeCoinCounter(new Vector2(18 * SCALE + 2f, 0), stocked.value, SCALE, fi);

                UI.EndParent();
            }

            UI.EndParent();

            UI.EndParent();

            if (Main.inputManager.JustPressed(Microsoft.Xna.Framework.Input.Keys.Escape))
            {
                gsManager.TheIsland.PopMenu();

                if (held.valid)
                {
                    EntityItem ent = new EntityItem(player.Position, -Main.camera.Forward * Cube.CUBE_SCALE * 5, held);
                    player.world.EntityManager.Add(ent);

                    held = new ItemInstance();
                }
            }
        }

        public override void Draw(SpriteBatch batch)
        {
            base.Draw(batch);

            UI.Draw(batch, SCALE);

            MenuHelper.DrawHeldItem(batch, held, SIZE, SCALE);
        }
    }
}
