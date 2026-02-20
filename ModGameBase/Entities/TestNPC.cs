using BepuPhysics;
using BepuPhysics.Collidables;
using BrUtility;
using Engine;
using Engine.Networking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Rendering;
using ViMG.UIs;

namespace ViMG.Entities
{
    [EntityMeta(0)]
    [EntitySerializable(EntitySerializableAttribute.SerializationType.All)]
    public class TestNPC : Entity, ISyncBasicState
    {
        private static string firstTimeRightClick = "Well, I'll be. Someone came to save me.\r\n" +
            "I'm soaked to the bone and exhausted. You wouldn't happen to have a place to stay, " +
            "would you? I'll sell you goods, if you're willing.";

        private static MenuDialogue.OptionsInput[] options = new MenuDialogue.OptionsInput[]
        {
            new MenuDialogue.OptionsInput()
            {
                optionText = "Who are you?",
                text = "Me? Name's Wick. I'm a merchant. The lousiest damn merchant in the third sea, I tell you what.\r\n" +
                    "See, I heard of this boat leaving for the island of Meldri a few weeks back. Not many go by that place, " +
                    "but there's good business there, I'll have you know. So I thought, why not ask them to take me along?\r\n" +
                    "Turns out before heading to Meldri, they're taking prisoners to Vi. This island. This damn island.\r\n" +
                    "In my infinite wisdom, I still chose to board that boat. It was cheap, y'see, and heading to where I was going...\r\n" +
                    "Some decision that turned out to be. The boat's a wreck. Didn't last long after we dumped you prisoners here in an " +
                    "attempt to appease the sea. I suspect you and I are the only survivors.\r\n" +
                    "Well, you're one of those dangerous prisoners they was carrying, so I hope you'll let me live. " +
                    "If you do that, I'll sell you my wares, as I said."
            },
            new MenuDialogue.OptionsInput()
            {
                optionText = "Your Wares",
                text = "Lucky me! Or you, I suppose. Most of my wares survived the trip. Might be a bit damp, though."
            }
        };

        private static MenuShop.ShopStockedItem[] stockedItems = new MenuShop.ShopStockedItem[]
        {
            new MenuShop.ShopStockedItem()
            {
                item = new Items.ItemInstance(GlobalState.Registry.ItemRegistry.Get("flask_healthpotion1"), -1, 0),
                value = 50,
            },
            new MenuShop.ShopStockedItem()
            {
                item = new Items.ItemInstance(GlobalState.Registry.ItemRegistry.Get("flask_magicpotion1"), -1, 0),
                value = 50,
            },
            new MenuShop.ShopStockedItem()
            {
                item = new Items.ItemInstance(GlobalState.Registry.ItemRegistry.Get("book_blank"), -1, 0),
                value = 120,
            },
            new MenuShop.ShopStockedItem()
            {
                item = new Items.ItemInstance(GlobalState.Registry.ItemRegistry.Get("rope"), -1, 0),
                value = 25,
            },
            new MenuShop.ShopStockedItem()
            {
                item = new Items.ItemInstance(GlobalState.Registry.ItemRegistry.Get("book_lore_island1"), -1, 0),
                value = 500,
            },
            new MenuShop.ShopStockedItem()
            {
                item = new Items.ItemInstance(GlobalState.Registry.ItemRegistry.Get("food_bread1"), -1, 0),
                value = 235,
            },
        };

        private static VerySimpleMesh mesh;
        private static RendererDeferred.DrawMaterial material = new RendererDeferred.DrawMaterial(DrawHelper.WhitePixel);

        private bool shouldFollowUpMenu;

        private TypedIndex physicsShapeIndex;
        private BodyHandle physicsHandle;

        public TestNPC()
        {
        }

        public TestNPC(Vector3 position)
        {
            this.Position = position + new Vector3(Cube.CUBE_SCALE / 2f, Cube.CUBE_SCALE * 2f, Cube.CUBE_SCALE / 2f);
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

            var physicsShape = new Capsule(Cube.CUBE_SCALE / 2f, Cube.CUBE_SCALE * 0.98f);
            physicsShapeIndex = world.PhysicsInfo.Simulation.Shapes.Add(physicsShape);
            physicsHandle = world.PhysicsInfo.Simulation.Bodies.Add(BodyDescription.CreateDynamic(
                new RigidPose(Position.ToNumerics()), new BodyInertia() { InverseMass = 1f / 20f }, physicsShapeIndex, 0.001f));
        }

        public override void OnUnload()
        {
            base.OnUnload();

            world.PhysicsInfo.Simulation.Shapes.Remove(physicsShapeIndex);
            world.PhysicsInfo.Simulation.Bodies.Remove(physicsHandle);
        }

        public override void Update(double deltaTime)
        {
            // TODO this has some camera and client-side stuff in it, needs a refactor.

            base.Update(deltaTime);

            if (!world.ChunkLoadManager.IsLoaded(ChunkPosition.WorldSpaceChunk(Position)))
            {
                //don't update position - freeze in place
                world.PhysicsInfo.Simulation.Bodies[physicsHandle].Pose.Position = Position.ToNumerics();
                return;
            }
            else world.PhysicsInfo.Simulation.Awakener.AwakenBody(physicsHandle);

            Position = world.PhysicsInfo.Simulation.Bodies[physicsHandle].Pose.Position;

            if (world.ChunkManager.CubeView.GetCube(CubePosition.FromWorldSpace(Position))
                .GetOrDefault(GlobalState.Registry.CubeRegistry.Air) == GlobalState.Registry.CubeRegistry.Get("water"))
            {
                Vector3 velocity = world.PhysicsInfo.Simulation.Bodies[physicsHandle].MotionState.Velocity.Linear;
                velocity -= new Vector3(0, Physics.PhysicsInfo.SIM_GRAVITY * 1.01f, 0);

                if (velocity.Y > -Physics.PhysicsInfo.SIM_GRAVITY * 1.01f)
                    velocity.Y = -Physics.PhysicsInfo.SIM_GRAVITY * 1.01f;

                world.PhysicsInfo.Simulation.Bodies[physicsHandle].MotionState.Velocity.Linear = velocity.ToNumerics();
            }

            //if (GlobalState.gameStateManager.TheIsland.GetCurrentMenu() is MenuPlayer mp && !mp.IsOpened && Main.inputManager.JustPressed(A1r.Input.MouseInput.RightButton))
            //{
            //    Ray ray = new Ray(world.player[world.localPlayerIndex].Position, -Main.camera.Forward * Cube.CUBE_SCALE * 4f);

            //    BoundingBox bb = new BoundingBox(Position - new Vector3(Cube.CUBE_SCALE / 2),
            //        Position + new Vector3(Cube.CUBE_SCALE / 2f, Cube.CUBE_SCALE * 2, Cube.CUBE_SCALE / 2f));

            //    if (bb.Intersects(ray).HasValue)
            //    {
            //        OnRightClick();
            //    }
            //}

            ////When the dialogue stops, check what option we selected. If it's 1 (shop option) then open the shop.
            //if (GlobalState.gameStateManager.TheIsland.GetCurrentMenu() is MenuPlayer && shouldFollowUpMenu) 
            //{
            //    if (world.MenuDialogue.SelectedOption == 1)
            //    {
            //        var player = world.player[world.localPlayerIndex];
            //        GlobalState.gameStateManager.TheIsland.PushMenu(new MenuShop(GlobalState.gameStateManager, world.EntityManager.GetReference(player), player.inventory, player.heldInventory, stockedItems));
            //    }

            //    shouldFollowUpMenu = false;
            //}
        }

        public void OnRightClick()
        {
            if (!world.WorldInfo.flags.HasFlag(WorldLogics.WorldFlags.FlagValues.MERCHANT_SAVED))
            {
                world.MenuDialogue.StartText(firstTimeRightClick);
                world.WorldInfo.flags.Flags |= WorldLogics.WorldFlags.FlagValues.MERCHANT_SAVED;
            }
            else world.MenuDialogue.StartOptions(options);

            GlobalState.GameStateManager.TheIsland.PushMenu(world.MenuDialogue);

            shouldFollowUpMenu = true;
        }

        //public override void Draw(GraphicsDevice device, Effect effect)
        //{
        //    base.Draw(device, effect);

        //    if (mesh.IBO == null)
        //        mesh = MeshHelper.MakeQuad(device, Cube.CUBE_SCALE, Cube.CUBE_SCALE * 2, Enums.Alignment.Bottom);
        //    //mesh = MeshHelper.MakeEnemyQuad(device, Cube.CUBE_SCALE, Cube.CUBE_SCALE * 2);

        //    Main.Renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(material, mesh,
        //        Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
        //        Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
        //        Matrix.CreateTranslation(Position - new Vector3(0, Cube.CUBE_SCALE / 2f, 0))));
        //}

        public override void OnSave(List<byte> saveBytes)
        {
            base.OnSave(saveBytes);

            SaveHelper.SaveVector3(saveBytes, Position + new Vector3(0, Cube.CUBE_SCALE * 4, 0));
        }

        public override void OnLoad(World world, byte[] loadBytes, in int version)
        {
            base.OnLoad(world, loadBytes, version);

            int index = 0;
            Position = SaveHelper.LoadVector3(loadBytes, ref index);
        }

        public void Get(out BasicState state)
        {
            state = new BasicState
            {
                position = Position,
            };
        }

        public void Set(ref readonly BasicState state)
        {
            throw new NotImplementedException();
        }
    }
}
