using BrNineSlice;
using BrUtility;
using Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SharpDX.Direct2D1.Effects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Items;
using static ViMG.UIs.UI;

namespace ViMG.UIs
{
    public static class UIWidgets
    {
        static UIWidgets()
        {
            Main.WindowTextInputEvent += Input;
        }

        private static char character;
        private static Keys key;

        private static void Input(object? sender, TextInputEventArgs args)
        {
            character = args.Character;
            key = args.Key;
        }

        public static void MakeTextbox(UI.ButtonConstructionParameters button, ref string str, TextHelper.FontInfo fontInfo)
        {
            UI.StartParent(button.bounds.Position);
            button.bounds.Position = Vector2.Zero;

            UI.MakeButton(button);
            UI.MakeLabel(new UI.LabelConstructionParameters(str, fontInfo, 1000, Vector2.Zero));

            UI.EndParent();
        }

        private static NineSlice tooltipPanelNS = new NineSlice(GlobalState.AssetsManager.GetAsset<Texture2D>("ui_inventory"),
            new RectangleF(256, 64, 64, 64), 16);
        private static TextHelper.FontInfo tooltipLabelTitleFI = new TextHelper.FontInfo(
            GlobalState.AssetsManager.GetAsset<SpriteFont>("fira_mono_sml"), 1, true);
        private static TextHelper.FontInfo tooltipLabelDescFI = new TextHelper.FontInfo(
            GlobalState.AssetsManager.GetAsset<SpriteFont>("fira_mono_tny"), 1, true);
        public static void MakeTooltip(Vector2 position, string title, string description)
        {
            const float minWidth = 256;
            const float maxWidth = 512;

            const float minHeight = 48;

            var nameWrapped = TextHelper.GetWrappedText(tooltipLabelTitleFI, title, maxWidth);
            var descWrapped = TextHelper.GetWrappedText(tooltipLabelDescFI, description, maxWidth);

            Size nameSize = tooltipLabelTitleFI.StringSize(nameWrapped.text);
            Size descSize = tooltipLabelDescFI.StringSize(descWrapped.text);

            float width = float.Max(minWidth, float.Max(nameSize.Width, descSize.Width));
            float height = float.Max(minHeight, nameSize.Height + descSize.Height);

            Vector2 realPos = UI.GetParentPosition() + position;

            if (realPos.X + width > Options.CurrentWindowResolution.X)
                realPos.X = Options.CurrentWindowResolution.X - width;
            if (realPos.Y + height > Options.CurrentWindowResolution.Y)
                realPos.Y = Options.CurrentWindowResolution.Y - height;

            position = realPos - UI.GetParentPosition();

            RectangleF bounds = new RectangleF(position, width, height);

            MakePanel(new PanelConstructionParameters(bounds.Expand(8), Color.White, tooltipPanelNS, true));
            MakeLabel(new LabelConstructionParameters(nameWrapped, tooltipLabelTitleFI, width, bounds.Position));
            MakeLabel(new LabelConstructionParameters(descWrapped, tooltipLabelDescFI, width, bounds.Position + new Vector2(0, nameSize.Height)));
        }

        public static void MakeCoinCounter(Vector2 position, int currency, float scale, TextHelper.FontInfo fi)
        {
            ItemHelper.GetCoins(currency, out ItemInstance coinsCopper, out ItemInstance coinsBronze, out ItemInstance coinsSilver, out ItemInstance coinsGold, out _);
            Vector2 labelOffset = new Vector2(0, 12 * scale);

            RectangleF rect = new RectangleF(position, 16 * scale, 16 * scale);
            Button button = UI.MakeButton(new ButtonConstructionParameters(rect, coinsCopper.item.Client.GetMaterial().Diffuse, coinsCopper.item.Client.SourceRect));
            UI.MakeLabel(new UI.LabelConstructionParameters(coinsCopper.num.ToString(), fi, 200, rect.Position + labelOffset));
            if (button.hovered)
                MakeTooltip(rect.Position, coinsCopper.item.GetName(coinsCopper) + " x" + coinsCopper.num, coinsCopper.item.GetDescription(coinsCopper));

            rect = new RectangleF(position.X + 16 * scale, position.Y, 16 * scale, 16 * scale);
            button = UI.MakeButton(new ButtonConstructionParameters(rect, coinsBronze.item.Client.GetMaterial().Diffuse, coinsBronze.item.Client.SourceRect));
            UI.MakeLabel(new UI.LabelConstructionParameters(coinsBronze.num.ToString(), fi, 200, rect.Position + labelOffset));
            if (button.hovered)
                MakeTooltip(rect.Position, coinsBronze.item.GetName(coinsBronze) + " x" + coinsBronze.num, coinsBronze.item.GetDescription(coinsBronze));

            rect = new RectangleF(position.X + 32 * scale, position.Y, 16 * scale, 16 * scale);
            button = UI.MakeButton(new ButtonConstructionParameters(rect, coinsSilver.item.Client.GetMaterial().Diffuse, coinsSilver.item.Client.SourceRect));
            UI.MakeLabel(new UI.LabelConstructionParameters(coinsSilver.num.ToString(), fi, 200, rect.Position + labelOffset));
            if (button.hovered)
                MakeTooltip(rect.Position, coinsSilver.item.GetName(coinsSilver) + " x" + coinsSilver.num, coinsSilver.item.GetDescription(coinsSilver));

            rect = new RectangleF(position.X + 48 * scale, position.Y, 16 * scale, 16 * scale);
            button = UI.MakeButton(new ButtonConstructionParameters(rect, coinsGold.item.Client.GetMaterial().Diffuse, coinsGold.item.Client.SourceRect));
            UI.MakeLabel(new UI.LabelConstructionParameters(coinsGold.num.ToString(), fi, 200, rect.Position + labelOffset));
            if (button.hovered)
                MakeTooltip(rect.Position, coinsGold.item.GetName(coinsGold) + " x" + coinsGold.num, coinsGold.item.GetDescription(coinsGold));
        }

        public static void MakeCheckbox(UI.ButtonConstructionParameters checkboxButton, 
            UI.TextureConstructionParameters offTexture, UI.TextureConstructionParameters onTexture, ref bool checkedValue)
        {
            UI.StartParent(checkboxButton.bounds.Position);
            checkboxButton.bounds.Position = Vector2.Zero;

            if (!checkedValue)
                UI.MakeTexture(offTexture);
            else UI.MakeTexture(onTexture);

            if (UI.MakeButton(checkboxButton).clickLeft)
                checkedValue = !checkedValue;

            UI.EndParent();
        }

        private static int sliderTrackingId = -1;
        public static bool MakeSlider(UI.ButtonConstructionParameters sliderButton, UI.TextureConstructionParameters texture, 
            float width, ref float currentValue) 
        {
            bool hasChanged = false;

            var parentId = UI.StartParent(sliderButton.bounds.Position);
            sliderButton.bounds.Position = Vector2.Zero;

            UI.MakeTexture(texture);

            float end = width - sliderButton.bounds.Size.Width;

            sliderButton.bounds.Position = new Vector2(MathHelper.Lerp(0, end, currentValue), 0);
            UI.Button dummyButton = UI.MakeButton(new UI.ButtonConstructionParameters(sliderButton.bounds, DrawHelper.TransparentPixel, null));

            if (sliderTrackingId == dummyButton.id.id)
            {
                //we're currently dragging.
                float mouseX = Main.inputManager.GetMousePosition().X;

                float realStart = parentId.position.X;
                float realEnd = realStart + end;

                float realSliderPosition = Math.Clamp(mouseX, realStart, realEnd);

                currentValue = (realSliderPosition - realStart) / (realEnd - realStart);

                UI.MakeTexture(sliderButton.bounds, sliderButton.texture, sliderButton.clickedSourceRect);

                hasChanged = true;

                if (!Main.inputManager.IsPressed(A1r.Input.MouseInput.LeftButton))
                    sliderTrackingId = -1;
            }
            else if (sliderTrackingId == -1)
            {
                //we're not currently dragging. Check too see if we want to start dragging.
                if (dummyButton.clickLeft)
                {
                    sliderTrackingId = dummyButton.id.id;

                    UI.MakeTexture(sliderButton.bounds, sliderButton.texture, sliderButton.clickedSourceRect);

                    hasChanged = true;
                }
                else if (dummyButton.hovered)
                    UI.MakeTexture(sliderButton.bounds, sliderButton.texture, sliderButton.hoveredSourceRect);
                else UI.MakeTexture(sliderButton.bounds, sliderButton.texture, sliderButton.sourceRect);
            }

            UI.EndParent();

            return hasChanged;
        }

        public static bool MakeDropdown(UI.ButtonConstructionParameters baseButton, UI.ButtonConstructionParameters dropdownButtons,
            UI.LabelConstructionParameters[] options, UI.Button[] outputState, ref bool open, ref int currentState)
        {
            UI.StartParent(baseButton.bounds.Position);
            baseButton.bounds.Position = Vector2.Zero;  //since we're using parenting, keeping this as it is would result in double offset, so reset to 0
            dropdownButtons.bounds.y += baseButton.bounds.height;

            if (currentState != -1)
                baseButton.label = options[currentState];

            UI.Button clickButton = UI.MakeButton(baseButton);

            if (clickButton.clickLeft)
                open = !open;

            bool changed = false;
            
            if (open)
            {
                for (int i = 0; i < options.Length; i++)
                {
                    dropdownButtons.label = options[i];
                    outputState[i] = UI.MakeButton(dropdownButtons);

                    dropdownButtons.bounds.y += dropdownButtons.bounds.height;

                    if (outputState[i].clickLeft)
                    {
                        open = false;
                        currentState = i;

                        changed = true;
                        break;
                    }
                }
            }

            UI.EndParent();

            return changed;
        }
    }
}
