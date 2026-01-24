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

namespace Engine.IMGUIImpl
{
    public static class IMGUINetworkDebug
    {
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
        }

        public static int MAX_FRAMES = 25;
        public static float MIN_VARIANCE = -0.25f;
        public static float MAX_VARIANCE = 0.25f;

        private static NetworkDebugFrame[] frames = new NetworkDebugFrame[MAX_FRAMES];
        private static int head = 0;

        private static TextHelper.FontInfo fi;

        public static void AddServerFrame(NetworkDebugFrame frame)
        {
            frames[head] = frame;
            head += 1;
            head %= MAX_FRAMES;
        }

        public static void AddClientEntSync(int frame, double time, int numEnts, int numBits, int numBytes)
        {
            int i = 0;
            while (i < MAX_FRAMES)
            {
                int currFrameI = EngineMathHelper.Mod(head - i, MAX_FRAMES);

                if (frames[currFrameI].frame == frame)
                {
                    if (frames[currFrameI].firstEntSyncArrival == 0)
                        frames[currFrameI].firstEntSyncArrival = time;

                    frames[currFrameI].numEntitiesSyncd += numEnts;
                    frames[currFrameI].numBits += numBits;
                    frames[currFrameI].numBytes += numBytes;
                    break;
                }

                i += 1;
            }
        }

        public static void Render(SpriteBatch batch)
        {
            float WIDTH = 256;
            float HEIGHT = 96;

            if (fi.font == null)
            {
                var font = Main.assetsManager.GetAsset<SpriteFont>("fira_mono");
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

            var i = 0;
            while (i < MAX_FRAMES)
            {
                var curr = (i + head) % MAX_FRAMES;
                var next = (i + 1 + head) % MAX_FRAMES;
                //var prev = EngineMathHelper.Mod((i - 1) + head, MAX_FRAMES);

                float range = MAX_VARIANCE - MIN_VARIANCE;
                float variancePCurr = 1 - (((float)frames[curr].variance - MIN_VARIANCE) / range);
                float variancePNext = 1 - (((float)frames[next].variance - MIN_VARIANCE) / range);

                float entSyncTimePCurr = 1 - (((float)frames[curr].firstEntSyncArrival - MIN_VARIANCE) / range);
                float entSyncTimePNext = 1 - (((float)frames[next].firstEntSyncArrival - MIN_VARIANCE) / range);

                float currP = (float)i / (float)MAX_FRAMES;
                float nextP = (float)(i + 1) / (float)MAX_FRAMES;

                {
                    var min = new Vector2(WIDTH * currP, HEIGHT * variancePCurr);
                    var max = new Vector2(WIDTH * nextP, HEIGHT * variancePNext);
                    batch.DrawLine(basePos + min, basePos + max, Color.Red);
                }
                {
                    var min = new Vector2(WIDTH * currP, HEIGHT * entSyncTimePCurr);
                    var max = new Vector2(WIDTH * nextP, HEIGHT * entSyncTimePNext);
                    batch.DrawLine(basePos + min, basePos + max, Color.Yellow);
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
            //if (ImGui.Begin("Network Debug"))
            //{
                
            //}
        }
    }
}
