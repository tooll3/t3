using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using SharpDX.Direct2D1;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Graph;
using UiHelpers;

namespace T3.Gui.Windows.TimeLine
{
    /// <summary>
    /// Shows a list of TimeClipLayerUi with Clip
    /// </summary>
    public class LayersArea
    {
        public void Draw(Animator animator)
        {
            ImGui.BeginGroup();
            
            var layerIndex = 0;
            foreach(var layer in animator.Layers)
            {
                DrawLayer(layer, layerIndex);
                layerIndex++;

            }
            ImGui.EndChild();
        }


        private void DrawLayer(Animator.Layer layer, int layerIndex)
        {
            ImGui.PushID(layer.Id.GetHashCode());
            ImGui.InvisibleButton("Layer#layer", new Vector2(0, LayerHeight-1));

            var min = ImGui.GetItemRectMin();
            var max = ImGui.GetItemRectMax();
            ImGui.GetWindowDrawList().AddRectFilled(new Vector2(min.X, max.Y-1), 
                                                    new Vector2(max.X, max.Y), Color.Black);
            
            var layerArea= new ImRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax());
            foreach (var clip in layer.Clips)
            {
                ClipUi.Draw(clip, layerArea);
            }
            ImGui.PopID();
        }

        private const int LayerHeight = 25;
    }


    /// <summary>
    /// Draws a layer clip
    /// </summary>
    public class ClipUi
    {
        public static void Draw(Animator.Clip clip, ImRect layerArea)
        {
            var xStartTime = TimeLineCanvas.Current.TransformPositionX((float)clip.StartTime);
            var xEndTime = TimeLineCanvas.Current.TransformPositionX((float)clip.EndTime);
            ImGui.SetCursorScreenPos(new Vector2(xStartTime, layerArea.Min.Y));
            ImGui.PushID(clip.Name.GetHashCode());
            if (ImGui.Button(clip.Name, new Vector2(xEndTime - xStartTime, layerArea.GetHeight())))
            {
                Log.Debug("Clicked clip " + clip.Name);
            }
            ImGui.PopID();
        }
    }
}