using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
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
        private ItemInstance held = new ItemInstance();
        private ShopStockedItem[] stock;

        private TextHelper.FontInfo fi = new TextHelper.FontInfo(Main.assetsManager.GetAsset<SpriteFont>("fira_mono_sml"), 1, true);
        private UI.ItemSlot[] inventoryItemSlots = new UI.ItemSlot[Player.INVENTORY_ROWS * Player.INVENTORY_COLUMNS];

        private int holdingItemSlot = -1;
        private float holdingTimer;
        private float holdingPickupTimer;

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

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            holdingTimer += (float)deltaTime;
            holdingPickupTimer -= (float)deltaTime;

            UI.Start();

            UI.StartParent(new Vector2(MARGIN, MARGIN + 32));
            
            MenuHelper.DoPlayerInventory(player, player.GetInventory(), player.GetHeldInventory(), Player.INVENTORY_ROWS, Player.INVENTORY_COLUMNS, 18 * 2f, 2f, inventoryItemSlots);

            UI.StartParent(new Vector2(0, MenuHelper.GetInventorySize(Player.INVENTORY_ROWS, Player.INVENTORY_COLUMNS, 18 * 2f, 2f).Height + MARGIN));

            UI.MakePanel(new UI.PanelConstructionParameters(new RectangleF(0, 0, MenuHelper.GetInventorySize(1, 4, 
                18 * 2f, 2f)), Color.White, MenuHelper.MainPanelNS));

            UIWidgets.MakeCoinCounter(new Vector2(16), player.Currency, SCALE, fi);

            UI.EndParent();

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
                UI.Button button = UI.MakeButton(buttonParameters);
                UI.MakeItemSlot(button, stocked.item);
                UIWidgets.MakeCoinCounter(new Vector2(18 * SCALE + 2f, 0), stocked.value, SCALE, fi);

                if (button.clickLeft && (!held.valid || held.item == stocked.item.item))
                {
                    //TODO: maybe change MenuHelper.HandleItemSlot? For right now, handle things manually.
                    //Both buttons should pull one item out and put it into the held slot.
                    if (Main.inputManager.JustPressed(A1r.Input.MouseInput.LeftButton) && player.Currency >= stocked.value)
                    {
                        holdingItemSlot = i;
                        holdingTimer = 0;
                        holdingPickupTimer = 20f / 60f;

                        if (!held.valid)
                            held = new ItemInstance(stocked.item, 1);
                        else held = new ItemInstance(held, held.num + 1);

                        player.Currency -= stocked.value;
                    }
                }
                
                UI.EndParent();
            }

            if (holdingItemSlot != -1)
            {
                if (Main.inputManager.JustReleased(A1r.Input.MouseInput.LeftButton))
                {
                    holdingItemSlot = -1;
                }

                if (Main.inputManager.IsHeld(A1r.Input.MouseInput.LeftButton))
                {
                    if (player.Currency >= stock[holdingItemSlot].value)
                    {
                        if (holdingPickupTimer <= 0)
                        {
                            if (holdingTimer > 4f)
                                holdingPickupTimer = 3f / 60f;
                            else if (holdingTimer > 2f)
                                holdingPickupTimer = 6f / 60f;
                            else if (holdingTimer > 1f)
                                holdingPickupTimer = 12f / 60;
                            else holdingPickupTimer = 24f / 60f;

                            held = new ItemInstance(held, held.num + 1);

                            player.Currency -= stock[holdingItemSlot].value;
                        }
                    }
                    //we can no longer afford the item, so stop buying it.
                    else holdingItemSlot = -1;
                }
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
