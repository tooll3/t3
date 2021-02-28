using System.Numerics;
using ImGuiNET;
using T3.Gui.InputUi;

namespace T3.Gui.Interaction
{
    public static class VectorValueEdit
    {
        public static InputEditStateFlags Draw(float[] components, float min, float max, float scale, bool clamp)
        {
            var width = ImGui.GetContentRegionAvail().X / components.Length;
            var size = new Vector2(width, 0);

            var resultingEditState = InputEditStateFlags.Nothing;
            for (var index = 0; index < components.Length; index++)
            {
                if (index > 0)
                    ImGui.SameLine();

                ImGui.PushID(index);
                resultingEditState |= SingleValueEdit.Draw(ref components[index], size: size, min, max, clamp, scale);
                ImGui.PopID();
            }

            return resultingEditState;
        }
        
        
        public static InputEditStateFlags Draw(int[] components, int min, int max, float scale, bool clamp)
        {
            var width = ImGui.GetContentRegionAvail().X / components.Length;
            var size = new Vector2(width, 0);

            var resultingEditState = InputEditStateFlags.Nothing;
            for (var index = 0; index < components.Length; index++)
            {
                if (index > 0)
                    ImGui.SameLine();

                ImGui.PushID(index);
                resultingEditState |= SingleValueEdit.Draw(ref components[index], size: size, min, max, clamp);
                ImGui.PopID();
            }

            return resultingEditState;
        }
    }
}