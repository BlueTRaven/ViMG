using BrUtility;
using Engine;
using Engine.Networking;
using Engine.Networking.Messages;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.GameStates;
using ViMG.IMGUIImpl;
using static Engine.Networking.NetworkManager;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

namespace ViMG.UIs
{
    public class ChatManager
    {
        private const int SYSTEM_MESSAGE = -1;
        private struct ChatMessage
        {
            public string message;
            public Color color;

            public ChatMessage(string message, Color color)
            {
                this.message = message;
                this.color = color;
            }
        }

        private FastList<ChatMessage> messages = new FastList<ChatMessage>();
        
        private readonly NetworkManager netManager;

        public ChatManager(NetworkManager netManager)
        {
            Debug.Assert(netManager != null);
            this.netManager = netManager;
        }

        private void AddChatMessageInternal(string message, Color? color = null, int playerId = SYSTEM_MESSAGE)
        {
            if (netManager.IsServer)
            {
                string prefixed = GetMessagePrefixedWithPlayerName(message, playerId);

                IMGUIConsole.LogLine(string.Format("<color({0})> {1}", color.GetValueOrDefault(Color.White).PackedValue.ToString("X"), prefixed));
                messages.Add(new ChatMessage(prefixed, color ?? Color.White));
            }
            else
            {
                IMGUIConsole.LogLine(string.Format("<color({0})> {1}", color.GetValueOrDefault(Color.White).PackedValue.ToString("X"), message));
                messages.Add(new ChatMessage(message, color ?? Color.White));
            }
        }

        public void AddPlayerMessage(DateTime time, string message, int playerId)
        {
            string prefixed = GetMessagePrefixedWithPlayerName(message, playerId);

            IMGUIConsole.LogLine(prefixed);
            messages.Add(new ChatMessage(message, Color.White));

            SyncChatMessageServer.Instance.Send(new SyncChatMessageServer.ChatToSend
            {
                str = prefixed,
                time = time,
                color = Color.White,
            });
        }

        public void AddChatMessage(string message, Color? color = null)
        {
            IMGUIConsole.LogLine(string.Format("<color({0})> {1}", (color ?? Color.White).PackedValue.ToString("X"), message));
            messages.Add(new ChatMessage(message, color ?? Color.White));

            SyncChatMessageServer.Instance.Send(new SyncChatMessageServer.ChatToSend
            {
                time = DateTime.Now,
                str = message,
                color = color ?? Color.White,
            });
        }

        public string GetMessagePrefixedWithPlayerName(string message, int playerId)
        {
            Debug.Assert(netManager.IsServer, "Must be a server to get player prefix");
            string playerName = "";
            if (playerId != SYSTEM_MESSAGE)
            {
                playerName = string.Format("{0}: ", netManager.GetNetPlayer(playerId).playerName);
            }

            return string.Format("{0}{1}", playerName, message);
        }
    }
}
