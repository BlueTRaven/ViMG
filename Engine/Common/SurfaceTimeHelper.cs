using Engine.Clients;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;

namespace Engine.Common
{
    public static class SurfaceTimeHelper
    {
        public static float GetTimeOfDay(double time, float dawnStartOffsetScale = 1f, float dawnEndOffsetScale = 1f, float duskStartOffsetScale = 1, float duskEndOffsetScale = 1, float timeOffset = 0)
        {
            //values here are in % of day cycle time;
            //dawn starts at the last 8% of the total cycle
            const float DAWN_START = 0.92f;
            //dawn ends after 16% of the total cycle (8% of the day cycle)
            const float DAWN_END = 0.16f;

            //Dusk starts at the last 8% of the day cycle.
            const float DUSK_START = 0.42f;
            //dusk ends after 16% of the night cycle.
            const float DUSK_END = 0.66f;

            float dawnStart = 1 - ((1 - DAWN_START) * dawnStartOffsetScale);
            float dawnEnd = DAWN_END * dawnEndOffsetScale;
            float duskStart = 0.5f - ((1 - DUSK_START - 0.5f) * duskStartOffsetScale);
            float duskEnd = ((DUSK_END - 0.5f) * duskEndOffsetScale) + 0.5f;

            float timeOfDayPercent = (float)((time + timeOffset) % World.DAY_CYCLE_TIME) / World.DAY_CYCLE_TIME;

            //Night time
            if (timeOfDayPercent > duskEnd && timeOfDayPercent <= dawnStart)
                return 1;
            else if (timeOfDayPercent > duskStart && timeOfDayPercent <= duskEnd)
                return (timeOfDayPercent - duskStart) / (duskEnd - duskStart);
            else if ((timeOfDayPercent > dawnStart && timeOfDayPercent <= 1) || (timeOfDayPercent >= 0 && timeOfDayPercent <= dawnEnd))
            {
                float percent = 0;
                if (timeOfDayPercent > dawnStart)
                    percent = (timeOfDayPercent - dawnStart) / ((timeOfDayPercent + dawnEnd) - dawnStart);
                else if (timeOfDayPercent <= dawnEnd)
                    percent = (timeOfDayPercent + (1 - dawnStart)) / (dawnEnd + (1 - dawnStart));

                percent = MathHelper.Clamp(percent, 0, 1);

                return 1 - percent;
            }

            return 0;
        }

        public static float GetDuskTime(double time)
        {
            //Dusk starts at the last 8% of the day cycle.
            const float DUSK_START = 0.42f;
            const float DUSK_END = 0.56f;

            float timeOfDayPercent = (float)(time % World.DAY_CYCLE_TIME) / World.DAY_CYCLE_TIME;

            if (timeOfDayPercent > DUSK_START && timeOfDayPercent <= DUSK_END)
                return (timeOfDayPercent - DUSK_START) / (DUSK_END - DUSK_START);

            return 0;
        }

        public static bool IsDay(double time)
        {
            return (time % World.DAY_CYCLE_TIME) <= World.DAY_CYCLE_TIME / 2f;
        }

        public static bool IsNight(double time)
        {
            return (time % World.DAY_CYCLE_TIME) > World.DAY_CYCLE_TIME / 2f;
        }

        public static float GetTimeOfNight(double time)
        {
            float timeOfDay = (float)(time % World.DAY_CYCLE_TIME);

            if (!IsNight(time))
                return 0;
            else
            {
                float nightTime = timeOfDay - (World.DAY_CYCLE_TIME / 2f);

                float midnightTime = World.DAY_CYCLE_TIME * 0.25f;

                if (nightTime < midnightTime)
                {
                    const float START = World.DAY_CYCLE_TIME / 2f;
                    const float END = World.DAY_CYCLE_TIME * 0.75f;

                    return (timeOfDay - START) / (END - START);
                }
                else
                {
                    const float START = World.DAY_CYCLE_TIME * 0.75f;
                    const float END = World.DAY_CYCLE_TIME;

                    return 1 - ((timeOfDay - START) / (END - START));
                }
            }
        }
    }
}
