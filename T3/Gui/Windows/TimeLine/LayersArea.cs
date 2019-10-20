using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using SharpDX.Direct2D1;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Commands;
using T3.Gui.Graph;
using UiHelpers;

namespace T3.Gui.Windows.TimeLine
{
    /// <summary>
    /// Shows a list of TimeClipLayerUi with Clip
    /// </summary>
    public class LayersArea
    {
        public void Draw(Instance compositionOp)
        {
            var animator = compositionOp.Symbol.Animator;            

            ImGui.BeginGroup();
            
            var layerIndex = 0;
            foreach(var layer in animator.Layers)
            {
                DrawLayer(layer, compositionOp);
                layerIndex++;

            }
            ImGui.EndChild();
        }


        private void DrawLayer(Animator.Layer layer, Instance compositionOp)
        {
            ImGui.InvisibleButton("Layer#layer", new Vector2(0, LayerHeight-1));

            var min = ImGui.GetItemRectMin();
            var max = ImGui.GetItemRectMax();
            ImGui.GetWindowDrawList().AddRectFilled(new Vector2(min.X, max.Y-1), 
                                                    new Vector2(max.X, max.Y), Color.Black);
            
            var layerArea= new ImRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax());
            foreach (var clip in layer.Clips)
            {
                ClipUi.Draw(clip, layerArea, compositionOp);
            }
        }

        private const int LayerHeight = 25;
    }


    /// <summary>
    /// Draws a layer clip
    /// </summary>
    public class ClipUi
    {
        public static void Draw(Animator.Clip clip, ImRect layerArea, Instance compositionOp)
        {
            var posOnCanvas = new Vector2((float)clip.StartTime,0);
            var xStartTime = TimeLineCanvas.Current.TransformPositionX((float)clip.StartTime);
            var xEndTime = TimeLineCanvas.Current.TransformPositionX((float)clip.EndTime);
            ImGui.SetCursorScreenPos(new Vector2(xStartTime, layerArea.Min.Y));
            ImGui.PushID(clip.Id.GetHashCode());
            
            if (ImGui.Button(clip.Name, new Vector2(xEndTime - xStartTime, layerArea.GetHeight())))
            {
                Log.Debug("Clicked clip " + clip.Name);
                _moveClipsCommand.StoreCurrentValues();
                UndoRedoStack.Add(_moveClipsCommand);
                _moveClipsCommand = null;
            }
            
            // Dragging
            if (ImGui.IsItemActive() && ImGui.IsMouseDragging(0, 0f))
            {
                if (_moveClipsCommand == null)
                {
                    var listWithOne = new List<Animator.Clip>() { clip };
                    _moveClipsCommand = new MoveTimeClipCommand( compositionOp.Symbol.Id, listWithOne);
                }
                
                var vectorInCanvas = TimeLineCanvas.Current.InverseTransformDirection(ImGui.GetIO().MouseDelta);
                clip.StartTime += vectorInCanvas.X;
                clip.EndTime += vectorInCanvas.X;
            }

            ImGui.PopID();
        }
        
        private static MoveTimeClipCommand _moveClipsCommand = null;
    }
}