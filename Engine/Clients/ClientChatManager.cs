using BrUtility;
using Engine.Networking;
using Engine.Networking.Messages;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.GameStates;
using ViMG.IMGUIImpl;

namespace Engine.Clients
{
    public class ClientChatManager
    {
        private const int MAX_CHAT_MESSAGES_TO_DISPLAY = 8;
        private struct ChatMessage
        {
            public string message;
            public Color color;
            public float timer;

            public ChatMessage(string message, Color color)
            {
                this.message = message;
                this.color = color;

                timer = 0;
            }
        }

        private FastList<ChatMessage> messages = new FastList<ChatMessage>();
        private int latestChatMessage;

        private readonly TextHelper.FontInfo fi;
        private readonly Vector2 position;
        private readonly NetworkManager netManager;

        public ClientChatManager(GraphicsDevice device, Vector2 position, NetworkManager netManager)
        {
            fi = new TextHelper.FontInfo(GlobalState.AssetsManager.GetAsset<SpriteFont>("fira_mono_sml"), 1, true);
            this.position = position;
            this.netManager = netManager;
        }

        public void AddChatMessage(DateTime time, Color color, string message)
        {
            messages.Add(new ChatMessage(message, color));
            latestChatMessage++;
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

                Color color = messages.Buffer[i].color;

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
                string realMessage = "";

                TextHelper.WrappedText wrappedText = TextHelper.GetWrappedText(fi, messages[i].message, 512);
                Vector2 alignmentOffset = TextHelper.GetAlignmentOffset(fi, messages[i].message, 0, messages[i].message.Length, bounds, Enums.Alignment.TopLeft);
                yPos += fi.StringHeight(wrappedText.text);
                bounds.Y = (int)(Options.CurrentWindowResolution.Y - yPos);

                TextHelper.DrawText(batch, fi, wrappedText, alignmentOffset, color, bounds, 1, TextHelper.OverFlowAction.None);
            }
        }

        [ConsoleCommand("say", "say a message in chat", ConsoleCommandRunSide.Client)]
        public static void Say(string[] parameters)
        {
            if (IMGUIConsole.RequireParam(parameters, 0, "message") && GlobalState.GameStateManager.GetCurrentGameState() is GameStateTheIsland theIsland)
            {
                SyncChatMessageClient.Instance.Send(new SyncChatMessageClient.ChatToSend
                {
                    str = string.Join(' ', parameters),
                });
            }
        }
    }
}
