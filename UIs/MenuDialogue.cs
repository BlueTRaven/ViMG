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
    public class MenuDialogue : Menu
    {
        public enum PlayingType
        {
            Text,
            Options
        }

        public struct OptionsInput
        {
            public string optionText;
            public string text;
        }

        private struct Option
        {
            public Text text;
        }

        private struct Text
        {
            //time it takes for each character to resolve
            //overwritten by text effects.
            public float overallCharacterTime;
            public string[] lines;
        }

        private struct DialogueInstance
        {
            public float characterTime;
            public int currentLineCharacter;
            public int currentLine;
        }

        //private float margin;
        //private float width;
        private TextHelper.FontInfo fi;

        private Text currentText;
        private OptionsInput[] selectableOptions;
        private DialogueInstance instance;

        private PlayingType playingType;
        private bool textFinished;
        private bool playing;

        public MenuDialogue(GameStateManager gsManager) : base(gsManager)
        {
            fi = new TextHelper.FontInfo(Main.assetsManager.GetAsset<SpriteFont>("fira_mono_sml"), 1, true);
        }

        public override void OnOpen()
        {
            base.OnOpen();

            Main.DrawCursor = true;
            Main.MouseControl = true;
        }

        public override void OnClose()
        {
            base.OnClose();
            
            currentText = new Text();
            instance = new DialogueInstance();

            playing = false;
            textFinished = true;

            Main.DrawCursor = false;
            Main.MouseControl = false;
        }

        public void StartText(string dialogue)
        {
            //parse text,
            //wrap text,
            //set as next display

            float margin = Options.CurrentWindowResolution.X / 16f;
            float width = Options.CurrentWindowResolution.X - margin * 2;

            currentText = new Text()
            {
                lines = TextHelper.WrapTextAsArray(fi, dialogue, width),
                overallCharacterTime = 4f / 60f,
            };

            instance = new DialogueInstance();

            playing = true;
            textFinished = false;

            playingType = PlayingType.Text;
        }

        public void StartOptions(OptionsInput[] options)
        {
            selectableOptions = options;

            playing = true;
            textFinished = true;

            playingType = PlayingType.Options;
        }

        public override void Update(GraphicsDevice device, double deltaTime)
        {
            base.Update(device, deltaTime);

            if (!playing)
            {
                gsManager.TheIsland.PopMenu();  //close the menu
                return;
            }

            UI.Start();

            if (playingType == PlayingType.Text)
            {
                instance.characterTime -= (float)deltaTime;

                while (instance.characterTime <= 0 && !textFinished)
                {
                    instance.characterTime += currentText.overallCharacterTime;
                    instance.currentLineCharacter++;
                }

                //clamp to bounds
                if (instance.currentLineCharacter >= currentText.lines[instance.currentLine].Length)
                {
                    instance.currentLineCharacter = currentText.lines[instance.currentLine].Length - 1;
                    textFinished = true;
                }

                if (Main.inputManager.JustPressed(A1r.Input.MouseInput.LeftButton) ||
                    Main.inputManager.JustPressed(A1r.Input.MouseInput.RightButton) ||
                    Main.inputManager.JustPressed(Microsoft.Xna.Framework.Input.Keys.E))
                {
                    if (textFinished)
                    {
                        if (instance.currentLine + 1 >= currentText.lines.Length)
                        {
                            playing = false;
                            gsManager.TheIsland.PopMenu();  //close the menu

                            return;
                        }
                        else
                        {
                            textFinished = false;
                            instance.currentLine++;
                            instance.currentLineCharacter = 0;
                            instance.characterTime = 0;
                        }
                    }
                    else
                    {
                        instance.currentLineCharacter = currentText.lines[instance.currentLine].Length - 1;
                    }
                }

                float margin = Options.CurrentWindowResolution.X / 16f;
                float width = Options.CurrentWindowResolution.X - margin * 2;

                UI.StartParent(new Vector2(margin, Options.CurrentWindowResolution.Y - fi.LineSpacing * 4f));

                RectangleF bounds = new RectangleF(0, 0, width, fi.LineSpacing * 3f);

                UI.MakePanel(Color.Black * 0.5f, bounds);

                if (instance.currentLine > 0)
                    UI.MakeLabel(new UI.LabelConstructionParameters(currentText.lines[instance.currentLine - 1], fi, bounds.width, Vector2.Zero));
                UI.MakeLabel(new UI.LabelConstructionParameters(currentText.lines[instance.currentLine], fi, bounds.width, new Vector2(0, fi.LineSpacing)));

                UI.EndParent();
            }
            else if (playingType == PlayingType.Options)
            {
                float margin = Options.CurrentWindowResolution.X / 16f;
                float width = Options.CurrentWindowResolution.X - margin * 2;

                UI.StartParent(new Vector2(margin, Options.CurrentWindowResolution.Y - fi.LineSpacing * 4f));

                RectangleF bounds = new RectangleF(0, 0, width, fi.LineSpacing * 3f);

                UI.MakePanel(Color.Black * 0.5f, bounds);

                float cw = 0;
                for (int i = 0; i < selectableOptions.Length; i++)
                {
                    OptionsInput input = selectableOptions[i];

                    float w = fi.StringWidth(input.optionText);
                    var button = UI.MakeButton(new UI.ButtonConstructionParameters(new RectangleF(cw - 2, -2, w + 4f, fi.LineSpacing + 4f), Color.White, Color.Gray, Color.Gray));
                    UI.MakeLabel(new UI.LabelConstructionParameters(input.optionText, fi, 2000, new Vector2(cw, 0)));

                    if (button.clickLeft)
                    {
                        StartText(input.text);
                    }

                    //+ some padding
                    cw += w + 16f;

                    //for now assume all options can fit on one line (they probably won't eventually)
                }

                UI.EndParent();
            }

            if (Main.inputManager.JustPressed(Microsoft.Xna.Framework.Input.Keys.Escape))
            {
                gsManager.TheIsland.PopMenu();
            }
        }

        public override void Draw(SpriteBatch batch)
        {
            if (!playing)
                return;

            UI.Draw(batch, 1);
        }
    }
}
