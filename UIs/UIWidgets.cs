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
