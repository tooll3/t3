using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using T3.Core.Operator;
using T3.Gui.Commands;
using T3.Gui.Graph;
using T3.Gui.OutputUi;

namespace T3.Gui.Windows
{
    public class OutputWindow : Window
    {
        public OutputWindow() : base()
        {
            _title = "Output";
            _visible = true;
        }

        protected override void DrawContent()
        {
            _pinning.UpdateSelection();

            _imageCanvas.Draw();
            ImGui.SetCursorPos(ImGui.GetWindowContentRegionMin() + new Vector2(0, 40));
            DrawSelection(_pinning.SelectedInstance, _pinning.SelectedUi);
            DrawToolbar();
        }


        private void DrawToolbar()
        {
            ImGui.SetCursorPos(ImGui.GetWindowContentRegionMin());
            _pinning.DrawPinning();

            if (ImGui.Button("1:1"))
            {
                _imageCanvas.SetScaleToMatchPixels();
                _imageCanvas.SetViewMode(ImageOutputCanvas.Modes.Pixel);
            }
            ImGui.SameLine();

            if (ImGui.Button("M"))
            {
                _imageCanvas.SetViewMode(ImageOutputCanvas.Modes.Fitted);
            }
            ImGui.SameLine();
        }



        public static void DrawSelection(Instance _selectedInstance, SymbolUi selectedUi)
        {
            if (_selectedInstance.Outputs.Count > 0)
            {
                var firstOutput = _selectedInstance.Outputs[0];
                IOutputUi outputUi = selectedUi.OutputUis[firstOutput.Id];
                outputUi.DrawValue(firstOutput);
            }
        }

        //private void UpdateSelection()
        //{
        //    if (!_enablePinning || _pinnedInstance == null || _pinnedUi == null)
        //    {
        //        if (GraphCanvasWindow.WindowInstances.Count == 0)
        //            return;

        //        var defaultGraphWindow = GraphCanvasWindow.WindowInstances[0] as GraphCanvasWindow;
        //        _selectedInstance = defaultGraphWindow.Canvas.CompositionOp;

        //        if (_selectedInstance == null)
        //            return;

        //        _selectedUi = SymbolUiRegistry.Entries[_selectedInstance.Symbol.Id];
        //        var selectedChildUi = _selectedUi.ChildUis.FirstOrDefault(childUi => childUi.IsSelected);
        //        if (selectedChildUi != null)
        //        {
        //            _selectedInstance = _selectedInstance.Children.Single(child => child.Id == selectedChildUi.Id);
        //            _selectedUi = SymbolUiRegistry.Entries[_selectedInstance.Symbol.Id];
        //        }

        //        _pinnedInstance = _selectedInstance;
        //        _pinnedUi = _selectedUi;
        //    }
        //    else
        //    {
        //        _selectedInstance = _pinnedInstance;
        //        _selectedUi = _pinnedUi;
        //    }
        //}

        private ImageOutputCanvas _imageCanvas = new ImageOutputCanvas();
        private SelectionPinning _pinning = new SelectionPinning();
    }
}
