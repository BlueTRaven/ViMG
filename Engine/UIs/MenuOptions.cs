using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.GameStates;

namespace ViMG.UIs
{
    public class MenuOptions : Menu
    {
        private UI.LabelConstructionParameters[] optionsAA;
        private int currentAAOption;
        private bool dropdownAAOpen;
        private UI.Button[] outputsAA;

        private UI.LabelConstructionParameters[] optionsFXAA;
        private int currentFXAAOption;
        private bool dropdownFXAAOpen;
        private UI.Button[] outputsFXAA;

        private UI.LabelConstructionParameters[] optionsSMAA;
        private int currentSMAAOption;
        private bool dropdownSMAAOpen;
        private UI.Button[] outputsSMAA;

        private UI.LabelConstructionParameters[] optionsHDR;
        private int currentHDROption;
        private bool dropdownHDROpen;
        private UI.Button[] outputsHDR;

        private TextHelper.FontInfo fi;

        public MenuOptions(GameStateManager gsManager) : base(gsManager)
        {
            fi = new TextHelper.FontInfo(Main.assetsManager.GetAsset<SpriteFont>("fira_mono_sml"), 1, true);
            Main.MouseControl = true;
            Main.DrawCursor = true;

            currentAAOption = (int)Options.CurrentAntiAliasing;
            optionsAA = new UI.LabelConstructionParameters[3]
            {
                new UI.LabelConstructionParameters("None", fi, 128, Vector2.Zero),
                new UI.LabelConstructionParameters("FXAA", fi, 128, Vector2.Zero),
                new UI.LabelConstructionParameters("SMAA", fi, 128, Vector2.Zero),
            };

            outputsAA = new UI.Button[3];

            currentFXAAOption = (int)Options.CurrentFXAAQuality;
            optionsFXAA = new UI.LabelConstructionParameters[3]
            {
                new UI.LabelConstructionParameters("Low", fi, 128, Vector2.Zero),
                new UI.LabelConstructionParameters("Medium", fi, 128, Vector2.Zero),
                new UI.LabelConstructionParameters("High", fi, 128, Vector2.Zero),
            };

            outputsFXAA = new UI.Button[4];

            currentSMAAOption = (int)Options.CurrentSMAAQuality;
            optionsSMAA = new UI.LabelConstructionParameters[4]
            {
                new UI.LabelConstructionParameters("Low", fi, 128, Vector2.Zero),
                new UI.LabelConstructionParameters("Medium", fi, 128, Vector2.Zero),
                new UI.LabelConstructionParameters("High", fi, 128, Vector2.Zero),
                new UI.LabelConstructionParameters("Ultra", fi, 128, Vector2.Zero),
            };
            outputsSMAA = new UI.Button[4];

            currentHDROption = (int)Options.CurrentHDRType;
            optionsHDR = new UI.LabelConstructionParameters[2]
            {
                new UI.LabelConstructionParameters("EXP", fi, 128, Vector2.Zero),
                new UI.LabelConstructionParameters("Aces", fi, 128, Vector2.Zero)
            };
            outputsHDR = new UI.Button[2];
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

            Main.MouseControl = false;
            Main.DrawCursor = false;
        }

        public override void Update(double deltaTime)
        {
            UI.Start();

            UI.StartParent(new Vector2(Options.CurrentWindowResolution.X / 2 - 64, Options.CurrentWindowResolution.Y / 2 - 128));

            if (UI.MakeButton(new UI.ButtonConstructionParameters(new RectangleF(-32, 256, 32, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                new UI.LabelConstructionParameters("<", fi, 32, Vector2.Zero),
                new RectangleF(0, 64, 32, 32), new RectangleF(32, 64, 32, 32), new RectangleF(32, 64, 32, 32))).clickLeft)
            {
                Main.SessionIO.Save();
                gsManager.GetCurrentGameState().PopMenu();
            }

            Vector2 pos = Vector2.Zero;

            UI.MakeLabel(new UI.LabelConstructionParameters("Anti-Aliasing", fi, 128 + 16, pos - new Vector2(128 + 16, 0)));

            if (UIWidgets.MakeDropdown(new UI.ButtonConstructionParameters(new RectangleF(pos, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                new UI.LabelConstructionParameters("Anti-Aliasing", fi, 128 + 16, Vector2.Zero),
                new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32)),
                new UI.ButtonConstructionParameters(new RectangleF(128, -32, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32)),
                optionsAA, outputsAA, ref dropdownAAOpen, ref currentAAOption))
            {
                if (currentAAOption == 0)
                {
                    Options.CurrentAntiAliasing = Options.AntiAliasing.None;
                    Options.CurrentSMAAQuality = Options.SMAA_INVALID;
                    Options.CurrentFXAAQuality = Options.FXAA_INVALID;
                }
                else if (currentAAOption == 1)
                {
                    Options.CurrentAntiAliasing = Options.AntiAliasing.FXAA;
                    Options.CurrentSMAAQuality = Options.SMAA_INVALID;

                    if (Options.CurrentFXAAQuality == Options.FXAA_INVALID)
                        Options.CurrentFXAAQuality = Options.FXAAQuality.FXAA_LOW;

                    currentFXAAOption = (int)Options.CurrentFXAAQuality;
                }
                else if (currentAAOption == 2)
                {
                    Options.CurrentAntiAliasing = Options.AntiAliasing.SMAA;
                    Options.CurrentFXAAQuality = Options.FXAA_INVALID;

                    if (Options.CurrentSMAAQuality == Options.SMAA_INVALID)
                        Options.CurrentSMAAQuality = Options.SMAAQuality.SMAA_LOW;

                    currentSMAAOption = (int)Options.CurrentSMAAQuality;
                }
            }

            pos.Y += 32 + MARGIN;

            if (currentAAOption == 1)
            {
                UI.MakeLabel(new UI.LabelConstructionParameters("FXAA Quality", fi, 128 + 16, pos - new Vector2(128 + 16, 0)));

                if (UIWidgets.MakeDropdown(new UI.ButtonConstructionParameters(new RectangleF(pos, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                    new UI.LabelConstructionParameters("FXAA Quality", fi, 128 + 16, Vector2.Zero),
                    new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32)),
                    new UI.ButtonConstructionParameters(new RectangleF(128, -32, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                    new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32)),
                    optionsFXAA, outputsFXAA, ref dropdownFXAAOpen, ref currentFXAAOption))
                {
                    Options.CurrentFXAAQuality = (Options.FXAAQuality)currentFXAAOption;
                }

                pos.Y += 32 + MARGIN;
            }
            else if (currentAAOption == 2)
            {
                UI.MakeLabel(new UI.LabelConstructionParameters("SMAA Quality", fi, 128 + 16, pos - new Vector2(128 + 16, 0)));

                if (UIWidgets.MakeDropdown(new UI.ButtonConstructionParameters(new RectangleF(pos, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                    new UI.LabelConstructionParameters("SMAA Quality", fi, 128 + 16, Vector2.Zero),
                    new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32)),
                    new UI.ButtonConstructionParameters(new RectangleF(128, -32, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                    new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32)),
                    optionsSMAA, outputsSMAA, ref dropdownSMAAOpen, ref currentSMAAOption))
                {
                    Options.CurrentSMAAQuality = (Options.SMAAQuality)currentSMAAOption;
                }

                pos.Y += 32 + MARGIN;
            }

            UI.MakeLabel(new UI.LabelConstructionParameters("HDR Type", fi, 128 + 16, pos - new Vector2(128 + 16, 0)));

            if (UIWidgets.MakeDropdown(new UI.ButtonConstructionParameters(new RectangleF(pos, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                new UI.LabelConstructionParameters("HDR", fi, 128 + 16, Vector2.Zero),
                new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32)),
                new UI.ButtonConstructionParameters(new RectangleF(128, -32, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32)),
                optionsHDR, outputsHDR, ref dropdownHDROpen, ref currentHDROption))
            {
                if (currentHDROption == 0)
                    Options.CurrentHDRType = Options.HDRType.HDR_EXP;
                else Options.CurrentHDRType = Options.HDRType.HDR_ACES;
            }

            pos.Y += 32 + MARGIN;

            UI.MakeLabel(new UI.LabelConstructionParameters("Instanced Light Volumes", fi, 256 - 8, pos - new Vector2(256 - 8, 0)));

            UIWidgets.MakeCheckbox(new UI.ButtonConstructionParameters(new RectangleF(pos, 32, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                new RectangleF(0, 64, 32, 32), new RectangleF(32, 64, 32, 32), new RectangleF(32, 64, 32, 32)),
                new UI.TextureConstructionParameters(new RectangleF(8, 8, 16, 16), Main.assetsManager.GetAsset<Texture2D>("ui_inventory"), new RectangleF(16, 16, 16, 16)),
                new UI.TextureConstructionParameters(new RectangleF(8, 8, 16, 16), Main.assetsManager.GetAsset<Texture2D>("ui_inventory"), new RectangleF(0, 16, 16, 16)),
                ref Options.UseInstancedLightVolumes);
            
            pos.Y += 32 + MARGIN;

            UI.MakeLabel(new UI.LabelConstructionParameters("Bloom", fi, 128, pos - new Vector2(128, 0)));

            UIWidgets.MakeCheckbox(new UI.ButtonConstructionParameters(new RectangleF(pos, 32, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                new RectangleF(0, 64, 32, 32), new RectangleF(32, 64, 32, 32), new RectangleF(32, 64, 32, 32)),
                new UI.TextureConstructionParameters(new RectangleF(8, 8, 16, 16), Main.assetsManager.GetAsset<Texture2D>("ui_inventory"), new RectangleF(16, 16, 16, 16)),
                new UI.TextureConstructionParameters(new RectangleF(8, 8, 16, 16), Main.assetsManager.GetAsset<Texture2D>("ui_inventory"), new RectangleF(0, 16, 16, 16)),
                ref Options.BloomEnabled);

            pos.Y += 32 + MARGIN;

            UI.MakeLabel(new UI.LabelConstructionParameters("Render Distance", fi, 128, pos - new Vector2(128, 0)));

            float range = Options.RENDER_DISTANCE_MAX - Options.RENDER_DISTANCE_MIN;
            float rdScalar = ((float)Options.RenderDistance - Options.RENDER_DISTANCE_MIN) / range;

            UIWidgets.MakeSlider(new UI.ButtonConstructionParameters(new RectangleF(pos, 32, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                new RectangleF(64, 64, 32, 32), new RectangleF(96, 64, 32, 32), new RectangleF(96, 64, 32, 32)),
                new UI.TextureConstructionParameters(new RectangleF(0, 0, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"), new RectangleF(0, 96, 128, 32)),
                128, ref rdScalar);

            Options.RenderDistance = (int)(rdScalar * range + Options.RENDER_DISTANCE_MIN);

            if (Options.RenderDistance <= Options.RENDER_DISTANCE_MIN)
                Options.RenderDistance = Options.RENDER_DISTANCE_MIN;

            if (Options.RenderDistance >= Options.RENDER_DISTANCE_MAX)
                Options.RenderDistance = Options.RENDER_DISTANCE_MAX;

            UI.MakeLabel(new UI.LabelConstructionParameters(Options.RenderDistance.ToString(), fi, 128, pos + new Vector2(128, 0)));

            /*Options.SMAAThresholdChanged = UIWidgets.MakeSlider(new UI.ButtonConstructionParameters(new RectangleF(pos, 32, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                new RectangleF(64, 64, 32, 32), new RectangleF(96, 64, 32, 32), new RectangleF(96, 64, 32, 32)),
                new UI.TextureConstructionParameters(new RectangleF(0, 0, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"), new RectangleF(0, 96, 128, 32)),
                128, ref smaaThreshold);

            if (UI.MakeButton(new UI.ButtonConstructionParameters(new RectangleF(pos + new Vector2(128, 0), 32, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                new UI.LabelConstructionParameters("R", fi, 32, Vector2.Zero),
                new RectangleF(0, 64, 32, 32), new RectangleF(32, 64, 32, 32), new RectangleF(32, 64, 32, 32))).clickLeft)
            {
                smaaThreshold = 0f;
                Options.SMAAThresholdChanged = true;
            }
            Options.SMAAThreshold = MathHelper.Lerp(0.05f, 0.5f, smaaThreshold);

            pos.Y += 32 + MARGIN;*/


            UI.EndParent();
        }

        public override void Draw(SpriteBatch batch)
        {
            UI.Draw(batch, 1);
        }
    }
}
