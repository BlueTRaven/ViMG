using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.UIs
{
    public static class UIWidgets
    {
        private static int trackingID = -1;
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

            if (trackingID == dummyButton.id.id)
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
                    trackingID = -1;
            }
            else if (trackingID == -1)
            {
                //we're not currently dragging. Check too see if we want to start dragging.
                if (dummyButton.clickLeft)
                {
                    trackingID = dummyButton.id.id;

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
