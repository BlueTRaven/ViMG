using BrUtility;
using Engine;
using Engine.Entities;
using Engine.Networking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Rendering;

namespace ViMG.Entities
{
    [EntityMeta(0)]
    [EntitySerializable(EntitySerializableAttribute.SerializationType.All)]
    public class ManaStar : Entity, ISyncBasicState
    {
        public enum State
        {
            InSky,
            FallingInSky,
            FallingInWorld,
            Finished,
        }

        public Vector2 yawPitch;

        public State state;
        public const float LOOKAT_TIME = 2;
        public const float FALLINGINSKY_TIME = 8f;
        public const float FALLINGINWORLD_TIME = 4f;
        public float timer = FALLINGINSKY_TIME;

        private int playerThatLookedAtStar = -1;

        public ManaStar() : this(Vector2.Zero)
        {
        }

        public ManaStar(Vector2 yawPitch)
        {
            this.yawPitch = yawPitch;
            DisableDistance = float.MaxValue;
        }

        public override void OnUnload()
        {
            base.OnUnload();
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            timer -= (float)deltaTime;
            if (state == State.InSky)
            {
                if (world.IsNight())
                {
                    bool anyLookedAt = false;
                    // check to see if we should fall
                    foreach (Player? player in world.player)
                    {
                        if (player != null)
                        {
                            Vector3 worldDir = Vector3.Transform(Vector3.Zero,
                                Matrix.CreateRotationX(MathHelper.ToRadians(-90)) *
                                Matrix.CreateTranslation(Vector3.Up) *
                                Matrix.CreateRotationX(MathHelper.ToRadians(yawPitch.Y)) *
                                Matrix.CreateRotationY(MathHelper.ToRadians(yawPitch.X)));
                            float dot = Vector3.Dot(-worldDir, (player as IRotatable).Forward);

                            //Console.WriteLine("{0:0.00}", dot);
                            if (dot > 0.8f) anyLookedAt = true;

                            if (dot > 0.8f && timer <= 0)
                            {
                                Console.WriteLine("Started falling to {0}:{1}!", GlobalState.GameStateManager.TheIsland.netManagerServer.GetNetPlayer(player.playerIndex).playerName, player.playerIndex);
                                playerThatLookedAtStar = player.playerIndex;
                                timer = FALLINGINSKY_TIME;
                                state = State.FallingInSky;
                            }
                        }
                    }

                    // no player looked at us
                    if (!anyLookedAt)
                    {
                        timer = LOOKAT_TIME;
                        playerThatLookedAtStar = -1;
                    }
                }
            } 
            else if (state == State.FallingInSky)
            {
                if (timer <= 0)
                {
                    timer = FALLINGINWORLD_TIME;
                    state = State.FallingInWorld;

                    //Set position to be the point where we end up being eventually.

                    Vector2 startXZ = world.player[playerThatLookedAtStar]!.Position.XZ() + new Vector2(GlobalState.random.Next(-32, 32) * Cube.CUBE_SCALE, GlobalState.random.Next(-32, 32) * Cube.CUBE_SCALE);
                    CubePosition endPos = world.ChunkManager.CubeView.GetFirstSolidDown(
                        CubePosition.FromWorldSpace(new Vector3(startXZ.X, world.sizeInCubes * Cube.CUBE_SCALE, startXZ.Y))).Get() +
                        new CubePosition(0, 1, 0);
                    Position = endPos.InWorldSpace();
                }
            }
            else if (state == State.FallingInWorld)
            {
                if (timer <= 0)
                {
                    Console.WriteLine("Finished");
                    state = State.Finished;
                }
            }
            else if (state == State.Finished)
            {
                world.ChunkManager.CubeView.SetCube(CubePosition.FromWorldSpace(Position),
                    GlobalState.Registry.CubeRegistry.Get("mana_star").Id, true);
                //if (timer <= 0)
                world.EntityManager.Kill(this);
            }

            AlwaysRender = world.IsNight();

            // TODO debug
            //if (Main.inputManager.JustPressed(A1r.Input.MouseInput.LeftButton))
            //{
            //    timer = DIVINGINSKY_TIME;
            //    state = State.DivingInSky;
            //}
        }

        public void Get(out BasicState state)
        {
            state = new BasicState
            {
                position = Position,
                rotation = new Quaternion(yawPitch.X, yawPitch.Y, 0, 1),
                state = (int)this.state,
                timers = { [0] = timer, },
            };
        }

        public void Set(ref readonly BasicState state)
        {
            Position = state.position;
            yawPitch = new(state.rotation.X, state.rotation.Y);
            timer = state.timers[0];
            this.state = (State)state.state;
        }
    }
}
