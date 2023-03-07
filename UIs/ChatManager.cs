using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.UIs
{
    public class ChatManager
    {
        private const int MAX_CHAT_MESSAGES_TO_DISPLAY = 8;
        private struct ChatMessage
        {
            public string message;
            public float timer;
        }

        private FastList<ChatMessage> messages = new FastList<ChatMessage>();
        private int latestChatMessage;

        private TextHelper.FontInfo fi;

        private Vector2 position;

        public ChatManager(Vector2 position)
        {
            this.position = position;

            fi = new TextHelper.FontInfo(Main.assetsManager.GetAsset<SpriteFont>("fira_mono_sml"), 1, true);
        }

        public void Update(double deltaTime)
        {
            for (int i = Math.Max(0, latestChatMessage - MAX_CHAT_MESSAGES_TO_DISPLAY); i < latestChatMessage; i++)
            {
                float timer = messages.Buffer[i].timer;
                timer += (float)deltaTime;
                messages.Buffer[i].timer = timer;
            }
        }

        public void Draw(SpriteBatch batch)
        {
            float yPos = 0;
            for (int i = latestChatMessage - 1; i >= Math.Max(0, latestChatMessage - MAX_CHAT_MESSAGES_TO_DISPLAY); i--)
            {
                float timer = messages.Buffer[i].timer;

                Color color = Color.White;

                if (timer > 8f)
                {
                    if (timer > 10f)
                        color = Color.Transparent;
                    else
                    {
                        float p = (timer - 8f) / 2f;
                        color *= 1 - p;
                    }
                }

                Rectangle bounds = new Rectangle((int)position.X, (int)(Options.CurrentWindowResolution.Y - yPos), 512, 512);
                TextHelper.WrappedText wrappedText = TextHelper.GetWrappedText(fi, messages[i].message, 512);
                Vector2 alignmentOffset = TextHelper.GetAlignmentOffset(fi, messages[i].message, bounds, Enums.Alignment.TopLeft);
                yPos += fi.StringHeight(wrappedText.text);
                bounds.Y = (int)(Options.CurrentWindowResolution.Y - yPos);

                TextHelper.DrawText(batch, fi, wrappedText, alignmentOffset, color, bounds, 1, TextHelper.OverFlowAction.None);
            }
        }

        public void AddChatMessage(string message)
        {
            messages.Add(new ChatMessage() { message = message });
            latestChatMessage++;
        }
    }
}
