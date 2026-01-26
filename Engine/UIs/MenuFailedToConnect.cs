using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.GameStates;
using ViMG.UIs;

namespace Engine.UIs
{
    public class MenuFailedToConnect : Menu
    {
        public enum ConnectionFailureReason
        {
            Refused,
        }

        private readonly TextHelper.FontInfo fi;
        private readonly ConnectionFailureReason failReason;
        private readonly string connectIp;
        private readonly int connectPort;
        private readonly Texture2D uiTex;
        private readonly UI.ButtonConstructionParameters buttonParams;

        public MenuFailedToConnect(GameStateManager gsManager, ConnectionFailureReason failReason, string connectIp, int connectPort) : base(gsManager)
        {
            this.failReason = failReason;
            this.connectIp = connectIp;
            this.connectPort = connectPort;
            fi = new TextHelper.FontInfo(GlobalState.AssetsManager.GetAsset<SpriteFont>("fira_mono_sml"), 1, true);
            uiTex = GlobalState.AssetsManager.GetAsset<Texture2D>("ui_buttons");

            buttonParams = new UI.ButtonConstructionParameters
            {
                bounds = new RectangleF(0, 0, 128, 32),
                label = new UI.LabelConstructionParameters("", fi, 128, Vector2.Zero, alignment: Enums.Alignment.Center, height: 32),
                nsSource = new BrNineSlice.NineSlice(uiTex, new RectangleF(0, 0, 128, 32), 4, 4, 4, 4),
                nsClicked = new BrNineSlice.NineSlice(uiTex, new RectangleF(0, 32, 128, 32), 4, 4, 4, 4),
                nsHovered = new BrNineSlice.NineSlice(uiTex, new RectangleF(0, 32, 128, 32), 4, 4, 4, 4),
                color = Color.White,
                valid = true,
            };
        }

        public override void OnOpen()
        {
            base.OnOpen();

            Main.MouseControl = true;
            Main.DrawCursor = true;
        }

        public override void OnClose()
        {
            base.OnClose();
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            UI.Start();

            UI.StartParent(new Vector2(Options.CurrentWindowResolution.X / 2 - 64, Options.CurrentWindowResolution.Y / 2 - 64));

            int y = 0;
            UI.MakeLabel(new UI.LabelConstructionParameters 
            {
                text = new TextHelper.WrappedText(128, string.Format("Failed to connect to {0}:{1}. Reason:\n{2}", connectIp, connectPort, failReason.ToString())),
                alignment = Enums.Alignment.Center,
                font = fi,
                width = 256,
                height = 32,
                color = Color.White,
                position = new Vector2(-64, y),
                valid = true,
            });
            
            y += 48;

            if (UI.MakeButton(buttonParams with
            {
                label = buttonParams.label.WithNewText("Return to Main Menu") with
                {
                    width = 128 + 96,
                },
                bounds = buttonParams.bounds with { x = buttonParams.bounds.x - 48, y = y, width = 128 + 96 },
            }).clickLeft)
            {
                Main.gameStateManager.GetCurrentGameState().PopMenu();
            }

            UI.EndParent();
        }

        public override void Draw(SpriteBatch batch)
        {
            UI.Draw(batch, 1);
        }
    }
}
