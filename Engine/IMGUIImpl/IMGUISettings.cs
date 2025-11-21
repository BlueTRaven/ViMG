using ImGuiNET;
using SharpDX.MediaFoundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.IMGUIImpl
{
    [AttributeUsage(AttributeTargets.Field)]
    public class IMGUIAutoSliderAttribute<T> : Attribute
    {
        public T min, max, num;

        public IMGUIAutoSliderAttribute(T min, T max, T num)
        {
            this.min = min;
            this.max = max;
            this.num = num;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class IMGUIAutoCheckBox : Attribute
    {
        public IMGUIAutoCheckBox()
        {

        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class IMGUIAutoCombo : Attribute
    {
        public IMGUIAutoCombo()
        {

        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class IMGUIAutoCallback : Attribute
    {
    }

    public static class IMGUISettings
    {
        public enum RendererGBufferOverrideDraw
        {
            Composited,
            All,
            Diffuse,
            LightAccum,
            Depth,
            Position,
            Normal,
            Ao,
        }

        [ConsoleCommandVar("show_settings", "Show settings menu")]
        public static bool Show = false;
        [ConsoleCommandVar("show_debug_info", "Show debug info")]
        public static bool ShowDebugInfo = false;
        [ConsoleCommandVar("show_console", "Show console")]
        public static bool ShowConsole = false;
        [ConsoleCommandVar("show_ent_io", "Show Ent IO Debug")]
        public static bool ShowEntIODebug = false;

        [IMGUIAutoSlider<int>(1, 10, 1)]
        [ConsoleCommandVar("CopiesPerFrame", "The number of chunk copies that can be produced in one frame.")]
        public static int CopiesPerFrame = 10;

        [IMGUIAutoCombo]
        [ConsoleCommandVar("GBufferOverrideDraw", "Composite: full rendering output\nOtherwise: gbuffer output")]
        public static RendererGBufferOverrideDraw GBufferOverrideDraw = RendererGBufferOverrideDraw.Composited;

        public static void AutoIMGUI()
        {
            foreach (FieldInfo fieldInfo in typeof(IMGUISettings).GetFields())
            {
                var sliderInt = fieldInfo.GetCustomAttribute<IMGUIAutoSliderAttribute<int>>();
                var sliderFloat = fieldInfo.GetCustomAttribute<IMGUIAutoSliderAttribute<float>>();

                var combo = fieldInfo.GetCustomAttribute<IMGUIAutoCombo>();

                if (sliderInt != null)
                {
                    var value = (int)fieldInfo.GetValue(null);
                    if (ImGui.SliderInt(fieldInfo.Name, ref value, sliderInt.min, sliderInt.max))
                    {
                        fieldInfo.SetValue(null, value);
                    }
                }

                if (sliderFloat != null)
                {
                    var value = (float)fieldInfo.GetValue(null);
                    if (ImGui.SliderFloat(fieldInfo.Name, ref value, sliderFloat.min, sliderFloat.max))
                    {
                        fieldInfo.SetValue(null, value);
                    }
                }

                if (combo != null)
                {
                    if (ImGui.BeginCombo(fieldInfo.Name, fieldInfo.GetValue(null).ToString()))
                    {
                        foreach (string enumName in fieldInfo.FieldType.GetEnumNames())
                        {
                            if (ImGui.Selectable(enumName, fieldInfo.GetValue(null).ToString() == enumName))
                            {
                                fieldInfo.SetValue(null, Enum.Parse(fieldInfo.FieldType, enumName));
                            }
                        }
                        ImGui.EndCombo();
                    }
                }
            }
        }
    }
}
