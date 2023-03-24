using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG
{
    public class DialogueManager
    {
        public struct Dialogue
        {
            //time it takes for each character to resolve
            //overwritten by text effects.
            public float overallCharacterTime; 
            public string[] texts;
        }

        private struct DialogueInstance
        {
            public float characterTime;
            public int currentTextCharacter;
            public int currentText;
        }

        private float margin;
        private float width;
        private TextHelper.FontInfo fi;

        private Dialogue currentDialogue;
        private DialogueInstance instance;

        private bool textFinished;
        private bool playing;

        public DialogueManager()
        {
            fi = new TextHelper.FontInfo(Main.assetsManager.GetAsset<SpriteFont>("fira_mono_sml"), 1, true);

            margin = Options.CurrentWindowResolution.X / 16f;
            width = Options.CurrentWindowResolution.X - margin * 2;
        }

        public void StartDialogue(string dialogue)
        {
            //parse text,
            //wrap text,
            //set as next display

            currentDialogue = new Dialogue()
            {
                texts = TextHelper.WrapTextAsArray(fi, dialogue, width),
                overallCharacterTime = 4f / 60f, 
            };

            instance = new DialogueInstance();

            playing = true;
            textFinished = false;
        }

        public void Update(double deltaTime)
        {
            if (!playing)
                return;

            instance.characterTime -= (float)deltaTime;

            while (instance.characterTime <= 0 && !textFinished)
            {
                instance.characterTime += currentDialogue.overallCharacterTime;
                instance.currentTextCharacter++;
            }

            //clamp to bounds
            if (instance.currentTextCharacter >= currentDialogue.texts[instance.currentText].Length)
            {
                instance.currentTextCharacter = currentDialogue.texts[instance.currentText].Length - 1;
                textFinished = true;
            }

            if (Main.inputManager.JustPressed(A1r.Input.MouseInput.LeftButton))
            {
                if (textFinished)
                {
                    if (instance.currentText + 1 >= currentDialogue.texts.Length)
                    {
                        playing = false;
                    }
                    else
                    {
                        textFinished = false;
                        instance.currentText++;
                        instance.currentTextCharacter = 0;
                    }
                }
                else
                {
                    instance.currentTextCharacter = currentDialogue.texts[instance.currentText].Length - 1;
                }
            }
        }

        public void Draw(SpriteBatch batch)
        {
            if (!playing)
                return;

            RectangleF bounds = new RectangleF(margin, Options.CurrentWindowResolution.Y - fi.LineSpacing * 4f, width, fi.LineSpacing * 3f);
            
            if (instance.currentText > 0)
            {
                var wrappedTextPre = new TextHelper.WrappedText(width, currentDialogue.texts[instance.currentText - 1]);
                TextHelper.DrawText(batch, fi, wrappedTextPre, TextHelper.GetAlignmentOffset(fi, wrappedTextPre.text,
                    wrappedTextPre.offset, wrappedTextPre.length, bounds.ToRectangle(), Enums.Alignment.TopLeft),
                    Color.White, bounds.ToRectangle(), 0.95f);
            }

            var wrappedText = new TextHelper.WrappedText(width, currentDialogue.texts[instance.currentText], 0, instance.currentTextCharacter);

            TextHelper.DrawText(batch, fi, wrappedText, TextHelper.GetAlignmentOffset(fi, wrappedText.text, 
                wrappedText.offset, wrappedText.length, bounds.ToRectangle(), Enums.Alignment.TopLeft), 
                Color.White, bounds.Offset(0, fi.LineSpacing).ToRectangle(), 0.95f);

            batch.DrawRectangle(bounds.Expand(4f), Color.Black * 0.5f, 0.94f);
        }
    }
}
