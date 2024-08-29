using ImGuiNET;
using SharpDX.MediaFoundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ViMG
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

    public static class IMGUISettings
    {
        [IMGUIAutoSlider<int>(1, 10, 1)]
        public static int CopiesPerFrame = 3;

        public static void AutoIMGUI()
        {
            foreach (FieldInfo fieldInfo in typeof(IMGUISettings).GetFields())
            {
                var sliderInt = fieldInfo.GetCustomAttribute<IMGUIAutoSliderAttribute<int>>();
                var sliderFloat = fieldInfo.GetCustomAttribute<IMGUIAutoSliderAttribute<float>>();

                if (sliderInt != null)
                {
                    var value = (int)fieldInfo.GetValue(null);
                    if (ImGui.SliderInt(fieldInfo.Name, ref value, sliderInt.min, sliderInt.max)) {
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
            }
        }
    }
}
