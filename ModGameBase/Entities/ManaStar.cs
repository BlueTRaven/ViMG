using BrUtility;
using Engine;
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
    //Spawns on world creation - always active.
    public class ManaStar : Entity, ISyncBasicState
    {
        public enum State
        {
            InSky,
            DivingInSky,
            DivingInWorld,
            Finished,
        }
        private static VerySimpleMesh mesh;
        private static RendererDeferred.DrawMaterial material = new RendererDeferred.DrawMaterial("mana_star");

        public Vector2 yawPitch;

        public State state;
        public const float DIVINGINSKY_TIME = 8f;
        public const float DIVINGINWORLD_TIME = 4f;
        public float timer = DIVINGINSKY_TIME;

        private EntityHelper.DirectionalSourceRect directionalSourceRect = new EntityHelper.DirectionalSourceRect()
        {
            front = new RectangleF(0, 0, 4, 4),
            back = new RectangleF(0, 0, 4, 4),
            sideLeft = new RectangleF(4, 0, 8, 4),
        };

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
            if (state == State.DivingInSky)
            {
                if (timer <= 0)
                {
                    timer = DIVINGINWORLD_TIME;
                    state = State.DivingInWorld;

                    //Set position to be the point where we end up being eventually.
                    
                    Vector2 startXZ = world.player[world.localPlayerIndex].Position.XZ() + new Vector2(GlobalState.random.Next(-32, 32) * Cube.CUBE_SCALE, GlobalState.random.Next(-32, 32) * Cube.CUBE_SCALE);
                    CubePosition endPos = world.ChunkManager.CubeView.GetFirstSolidDown(
                        CubePosition.FromWorldSpace(new Vector3(startXZ.X, world.sizeInCubes * Cube.CUBE_SCALE, startXZ.Y))).Get() +
                        new CubePosition(0, 1, 0);
                    Position = endPos.InWorldSpace();
                }
            }
            else if (state == State.DivingInWorld)
            {
                if (timer <= 0)
                {
                    state = State.Finished;
                    timer = 8f;
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
