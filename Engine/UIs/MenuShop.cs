using BrUtility;
using Engine;
using Engine.Items;
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

        private EntityManager.EntityReference player;
        private InventoryManager.InventoryReference playerInventory;
        private InventoryManager.InventoryReference heldInventory;
        private ShopStockedItem[] stock;

        private TextHelper.FontInfo fi = new TextHelper.FontInfo(GlobalState.AssetsManager.GetAsset<SpriteFont>("fira_mono_sml"), 1, true);
        private UI.ItemSlot[] inventoryItemSlots = new UI.ItemSlot[Player.INVENTORY_ROWS * Player.INVENTORY_COLUMNS];

        private int holdingItemSlot = -1;
        private float holdingTimer;
        private float holdingPickupTimer;

        public MenuShop(GameStateManager gsManager, EntityManager.EntityReference player, InventoryManager.InventoryReference playerInventory, InventoryManager.InventoryReference heldInventory, ShopStockedItem[] stock) : base(gsManager)
        {
            this.player = player;
            this.playerInventory = playerInventory;
            this.heldInventory = heldInventory;
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

            var invManager = gsManager.TheIsland.GetClient().inventoryManager;
            var inventory = invManager.Get(this.playerInventory);
            var heldInventory = invManager.Get(this.heldInventory);
            MenuHelper.DoPlayerInventory(player, inventory, heldInventory, Player.INVENTORY_ROWS, Player.INVENTORY_COLUMNS, 18 * 2f, 2f, inventoryItemSlots);

            UI.StartParent(new Vector2(0, MenuHelper.GetInventorySize(Player.INVENTORY_ROWS, Player.INVENTORY_COLUMNS, 18 * 2f, 2f).Height + MARGIN));

            UI.MakePanel(new UI.PanelConstructionParameters(new RectangleF(0, 0, MenuHelper.GetInventorySize(1, 4, 
                18 * 2f, 2f)), Color.White, MenuHelper.MainPanelNS));

            // TODO currency
            UIWidgets.MakeCoinCounter(new Vector2(16), 0, SCALE, fi);

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

                if (button.clickLeft && (!heldInventory.Get(0).valid || heldInventory.Get(0).item == stocked.item.item))
                {
                    //TODO: maybe change MenuHelper.HandleItemSlot? For right now, handle things manually.
                    //Both buttons should pull one item out and put it into the held slot.
                    // TODO currency
                    if (Main.inputManager.JustPressed(A1r.Input.MouseInput.LeftButton) && 0 >= stocked.value)
                    {
                        holdingItemSlot = i;
                        holdingTimer = 0;
                        holdingPickupTimer = 20f / 60f;

                        if (!heldInventory.Get(0).valid)
                            heldInventory.Set(new ItemInstance(stocked.item, 1), 0);
                        else heldInventory.Set(new ItemInstance(heldInventory.Get(0), heldInventory.Get(0).num + 1), 0);

                        // TODO currency
                        //player.Currency -= stocked.value;
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
                    // TODO currency
                    if (0 >= stock[holdingItemSlot].value)
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

                            heldInventory.Set(new ItemInstance(heldInventory.Get(0), heldInventory.Get(0).num + 1), 0);

                            // TODO currency
                            //player.Currency -= stock[holdingItemSlot].value;
                        }
                    }
                    //we can no longer afford the item, so stop buying it.
                    else holdingItemSlot = -1;
                }
            }

            UI.EndParent();

            UI.EndParent();

            // TODO drop item
            //if (Main.inputManager.JustPressed(Microsoft.Xna.Framework.Input.Keys.Escape))
            //{
            //    gsManager.TheIsland.PopMenu();

            //    if (heldInventory.Get(0).valid)
            //    {
            //        EntityItem ent = new EntityItem(player.Position, -Main.camera.Forward * Cube.CUBE_SCALE * 5, heldInventory.Get(0));
            //        player.world.EntityManager.Add(ent);

            //        heldInventory.Set(new ItemInstance(), 0);
            //    }
            //}
        }

        public override void Draw(SpriteBatch batch)
        {
            base.Draw(batch);

            UI.Draw(batch, SCALE);

            // TODO draw held item
            //MenuHelper.DrawHeldItem(batch, held, SIZE, SCALE);
        }
    }
}
