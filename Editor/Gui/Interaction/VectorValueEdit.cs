using ImGuiNET;
using T3.Editor.Gui.InputUi;

namespace T3.Editor.Gui.Interaction
{
    public static class VectorValueEdit
    {
        // Float control
        public static InputEditStateFlags Draw(float[] components, float min, float max, float scale, bool clamp, float rightPadding = 0, string format = null)
        {
            var width = (ImGui.GetContentRegionAvail().X - rightPadding) / components.Length;
            var size = new Vector2(width, 0);

            var resultingEditState = InputEditStateFlags.Nothing;
            for (var index = 0; index < components.Length; index++)
            {
                if (index > 0)
                    ImGui.SameLine();

                ImGui.PushID(index);
                resultingEditState |= SingleValueEdit.Draw(ref components[index], size: size, min, max, clamp, scale, format??="{0:0.000}");
                ImGui.PopID();
            }

            return resultingEditState;
        }
        
        // Integer control
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