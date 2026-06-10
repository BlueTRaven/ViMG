using Hexa.NET.ImGui;
using Hexa.NET.ImPlot;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using BrUtility;
using ViMG.IMGUIImpl;

namespace Engine.IMGUIImpl
{
    public static class IMGUINetworkDebug
    {
        [ConsoleCommandVar("net_debug_show", "Show the network debug menu")]
        public static bool ShowNetworkDebug = false;

        [ConsoleCommandVar("net_debug_graph_show", "Show the network debug graph")]
        public static bool ShowNetworkDebugGraph = false;

        private static bool showServerMessages = true;
        private static bool showClientMessages = true;
        private static bool scrollToBottom = false;

        private const int MAX_MESSAGES = 4096;
        private static NetworkDebugMessage[] messages = new NetworkDebugMessage[MAX_MESSAGES];
        private static int messageHead;
        private static int messagesNum;
        private static int messagesTail = 0;

        public struct NetworkDebugMessage
        {
            public required int messageType;
            public required DateTime time;
            public required bool isServer;
        }

        public struct NetworkDebugFrame
        {
            public int frame;
            public double expectedTime;
            public double actualTime;
            public double variance;

            public double firstEntSyncArrival;
            public int numEntitiesSyncd;
            public int numBits;
            public int numBytes;

            public double firstSyncAckArrival;
        }

        public static int MAX_FRAMES = 25;
        public static float MIN_VARIANCE = -0.25f;
        public static float MAX_VARIANCE = 0.25f;
        public static float RANGE_VARIANCE = MAX_VARIANCE - MIN_VARIANCE;

        private static NetworkDebugFrame[] frames = new NetworkDebugFrame[MAX_FRAMES];
        private static int head = 0;

        private static TextHelper.FontInfo fi;

        public static void AddMessage(NetworkDebugMessage message)
        {
            messages[messageHead] = message;
            messageHead += 1;
            messageHead %= MAX_MESSAGES;
            messagesNum += 1;
            if (messagesNum > MAX_MESSAGES)
            {
                messagesNum = MAX_MESSAGES;
                messagesTail += 1;
                messagesTail %= MAX_MESSAGES;
            }
        }

        public static void ClearMessages()
        {
            messageHead = 0;
            messagesNum = 0;
            messagesTail = 0;
        }

        public static void AddServerFrame(NetworkDebugFrame frame)
        {
            frames[head] = frame;
            head += 1;
            head %= MAX_FRAMES;
        }

        private static int FindFrame(int frame)
        {
            int i = 0;
            while (i < MAX_FRAMES)
            {
                int currFrameI = EngineMathHelper.Mod(head - i, MAX_FRAMES);

                if (frames[currFrameI].frame == frame)
                {
                    return currFrameI;
                }

                i += 1;
            }

            return 0;
        }

        public static void AddClientEntSync(int frame, double time, int numEnts, int numBits, int numBytes)
        {
            int currFrame = FindFrame(frame);

            if (frames[currFrame].firstEntSyncArrival == 0)
                frames[currFrame].firstEntSyncArrival = time;
            frames[currFrame].numEntitiesSyncd += numEnts;
            frames[currFrame].numBits += numBits;
            frames[currFrame].numBytes += numBytes;
        }

        public static void AddClientEntAck(int frame, double ackTime)
        {
            int currFrame = FindFrame(frame);
            if (frames[currFrame].firstSyncAckArrival == 0)
                frames[currFrame].firstSyncAckArrival = ackTime;
        }

        public static void Render(SpriteBatch batch)
        {
            if (!ShowNetworkDebugGraph)
                return;

            float WIDTH = 256;
            float HEIGHT = 96;

            if (fi.font == null)
            {
                var font = GlobalState.AssetsManager.GetAsset<SpriteFont>("fira_mono");
                fi = new TextHelper.FontInfo
                {
                    font = font,
                    outline = false,
                    outlineColor = Color.White,
                    size = 0.5f,
                };
            }

            var basePos = Options.CurrentWindowResolution.ToVector2() - new Vector2(WIDTH, HEIGHT);

            batch.DrawRectangle(new RectangleF(basePos, WIDTH, HEIGHT), Color.Gray * 0.5f);
            batch.DrawLine(basePos + new Vector2(0, HEIGHT / 2), basePos + new Vector2(WIDTH, HEIGHT / 2), Color.White);
            float syncFrameHeight = 1 - ((World.SyncTime - MIN_VARIANCE) / RANGE_VARIANCE);
            batch.DrawLine(basePos + new Vector2(0, (HEIGHT * syncFrameHeight)), basePos + new Vector2(WIDTH, (HEIGHT * syncFrameHeight)), Color.Pink);

            var i = 0;
            while (i < MAX_FRAMES)
            {
                var curr = (i + head) % MAX_FRAMES;
                var next = (i + 1 + head) % MAX_FRAMES;
                //var prev = EngineMathHelper.Mod((i - 1) + head, MAX_FRAMES);

                float currP = (float)i / (float)MAX_FRAMES;
                float nextP = (float)(i + 1) / (float)MAX_FRAMES;

                {
                    float variancePCurr = 1 - (((float)frames[curr].variance - MIN_VARIANCE) / RANGE_VARIANCE);
                    float variancePNext = 1 - (((float)frames[next].variance - MIN_VARIANCE) / RANGE_VARIANCE);
                    var min = new Vector2(WIDTH * currP, HEIGHT * variancePCurr);
                    var max = new Vector2(WIDTH * nextP, HEIGHT * variancePNext);
                    batch.DrawLine(basePos + min, basePos + max, Color.Red);
                }
                {
                    float entSyncTimePCurr = 1 - (((float)frames[curr].firstEntSyncArrival - MIN_VARIANCE) / RANGE_VARIANCE);
                    float entSyncTimePNext = 1 - (((float)frames[next].firstEntSyncArrival - MIN_VARIANCE) / RANGE_VARIANCE);
                    var min = new Vector2(WIDTH * currP, HEIGHT * entSyncTimePCurr);
                    var max = new Vector2(WIDTH * nextP, HEIGHT * entSyncTimePNext);
                    batch.DrawLine(basePos + min, basePos + max, Color.Yellow);
                }
                {
                    float entAckTimePCurr = 1 - (((float)frames[curr].firstSyncAckArrival - MIN_VARIANCE) / RANGE_VARIANCE);
                    float entAckTimePNext = 1 - (((float)frames[next].firstSyncAckArrival - MIN_VARIANCE) / RANGE_VARIANCE);
                    var min = new Vector2(WIDTH * currP, HEIGHT * entAckTimePCurr);
                    var max = new Vector2(WIDTH * nextP, HEIGHT * entAckTimePNext);
                    batch.DrawLine(basePos + min, basePos + max, Color.Orange);
                }

                RectangleF bounds = new RectangleF(basePos + new Vector2(WIDTH * currP, 0), new Vector2(WIDTH * nextP - WIDTH * currP, HEIGHT));
                if (bounds.Contains(Main.inputManager.GetMousePosition().ToVector2()))
                {
                    var offset = new Vector2(0, -fi.font.LineSpacing * 0.5f - 8);
                    StringBuilder sb = new StringBuilder();
                    sb.AppendFormat("Frame {0}\n", frames[curr].frame);
                    sb.AppendFormat("Expected {0:0.00}\n", frames[curr].expectedTime);
                    sb.AppendFormat("Actual {0:0.00}\n", frames[curr].actualTime);
                    sb.AppendFormat("Variance {0:0.00} {1}\n", frames[curr].variance, frames[curr].variance > 0 ? "Late" : "Early");
                    sb.AppendFormat("Ent Arrival Time {0:0.00}\n", frames[curr].firstEntSyncArrival);
                    sb.AppendFormat("Num Entities {0} - Bits {1}, Bytes {2}", frames[curr].numEntitiesSyncd, frames[curr].numBits, frames[curr].numBytes);
                    //TextHelper.GetWrappedText(fi, sb.ToString(), WIDTH);

                    TextHelper.DrawText(batch, fi, sb.ToString(), Color.White, Rectangle.Empty, Enums.Alignment.TopLeft, -1);
                }
                i++;
            }
        }

        // TODO: Hexa.Net.Implot is nonfunctional (segfaulting all over the place). Replace this native version with imgui eventually...
        public static void Show()
        {
            if (ShowNetworkDebug && ImGui.Begin("Network Debug", ref ShowNetworkDebug))
            {
                ImGui.Checkbox("Draw graph", ref ShowNetworkDebugGraph);

                ImGui.BeginGroup();
                ImGui.Checkbox("Show Server", ref showServerMessages);
                ImGui.SameLine();
                ImGui.Checkbox("Show Client", ref showClientMessages);
                if (ImGui.Button("Scroll to bottom"))
                {
                    scrollToBottom = true;
                }

                if (ImGui.BeginTable("Messages", 3))
                {
                    ImGui.TableSetupColumn("Date");
                    ImGui.TableSetupColumn("Side");
                    ImGui.TableSetupColumn("Message Type");
                    ImGui.TableHeadersRow();
                    for (int i = 0; i < messagesNum; i++)
                    {
                        int cur = (messagesTail + i) % MAX_MESSAGES;
                        if ((messages[cur].isServer && showServerMessages) || (!messages[cur].isServer && showClientMessages))
                        {
                            ImGui.TableNextRow();
                            ImGui.TableSetColumnIndex(0);
                            ImGui.TextUnformatted(string.Format("{0}", messages[cur].time));
                            ImGui.TableSetColumnIndex(1);
                            ImGui.TextUnformatted(string.Format("{0}", messages[cur].isServer ? "Server" : "Client"));
                            ImGui.TableSetColumnIndex(2);
                            ImGui.TextUnformatted(string.Format("{0}", GlobalState.Registry.MessageRegistry.Get(messages[cur].messageType)?.Identifier));
                        }
                    }
                    ImGui.EndTable();
                }

                if (scrollToBottom)
                {
                    if (ImGui.GetIO().MouseWheel != 0.0f)
                    {
                        scrollToBottom = false;
                    }
                    ImGui.SetScrollHereY(1.0f);
                }
                ImGui.EndGroup();
                ImGui.End();
            }
        }
    }
}
