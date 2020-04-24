using System;
using System.Collections.Generic;
using ImGuiNET;
using SharpDX;
using T3.Core.Operator;
using T3.Gui.Graph.Interaction;
using T3.Gui.OutputUi;
using T3.Gui.Windows;
using T3.Gui.Windows.Output;
using Vector2 = System.Numerics.Vector2;

namespace T3.Gui.Graph
{
    public partial class GraphWindow
    {
        /// <summary>
        /// A helper class to render an image output into the background of the graph window 
        /// </summary>
        private class ImageBackground
        {
            internal List<Guid> BackgroundNodePath;

            internal bool IsActive => BackgroundNodePath != null;

            public void DrawResolutionSelector()
            {
                ResolutionHandling.DrawSelector(ref _selectedResolution, null);
            }
            
            public void Draw()
            {
                if (BackgroundNodePath == null)
                    return;

                _evaluationContext.Reset();
                _imageCanvas.PreventMouseInteraction = true;
                _imageCanvas.Update();
                
                ImGui.SetCursorPos(ImGui.GetWindowContentRegionMin() + new Vector2(0, 0));
                
                var instanceForOutput = NodeOperations.GetInstanceFromIdPath(BackgroundNodePath);

                if (instanceForOutput == null || instanceForOutput.Outputs.Count == 0)
                    return;

                var viewOutput = instanceForOutput.Outputs[0];
                var viewSymbolUi = SymbolUiRegistry.Entries[instanceForOutput.Symbol.Id];
                if (!viewSymbolUi.OutputUis.TryGetValue(viewOutput.Id, out IOutputUi viewOutputUi))
                    return;
                
                _evaluationContext.RequestedResolution = _selectedResolution.ComputeResolution();
                //var shouldEvaluate = viewOutput.DirtyFlag.FramesSinceLastUpdate > 0;
                viewOutputUi.DrawValue(viewOutput, _evaluationContext, recompute: true);
            }
            
            private readonly ImageOutputCanvas _imageCanvas = new ImageOutputCanvas();

            private readonly EvaluationContext _evaluationContext = new EvaluationContext();
            private ResolutionHandling.Resolution _selectedResolution = ResolutionHandling.DefaultResolution;
        }
    }
}