using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using T3.Core.Operator;
using T3.Gui.Commands;
using T3.Gui.Graph;
using T3.Gui.OutputUi;

namespace T3.Gui.Windows
{
    public class ViewWindow : Window
    {
        public ViewWindow() : base()
        {
            _title = "View";
            _visible = true;
        }

        protected override void DrawContent()
        {
            DrawToolbar();
            UpdateSelectedInstance();
            if (_selectedInstance == null)
                return;

            SymbolUi selectedUi = SymbolUiRegistry.Entries[_selectedInstance.Symbol.Id];
            var selectedChildUi = selectedUi.ChildUis.FirstOrDefault(childUi => childUi.IsSelected);
            if (selectedChildUi != null)
            {
                _selectedInstance = _selectedInstance.Children.Single(child => child.Id == selectedChildUi.Id);
                selectedUi = SymbolUiRegistry.Entries[_selectedInstance.Symbol.Id];
            }

            if (_selectedInstance.Outputs.Count > 0)
            {
                var firstOutput = _selectedInstance.Outputs[0];
                IOutputUi outputUi = selectedUi.OutputUis[firstOutput.Id];
                outputUi.DrawValue(firstOutput);
            }
        }


        private void DrawToolbar()
        {
            ImGui.Checkbox("pin", ref _enablePinning);
            ImGui.SameLine();
            if (_selectedInstance != null)
            {
                ImGui.Text(_selectedInstance.Symbol.Name);
            }
        }


        private void UpdateSelectedInstance()
        {
            if (GraphCanvasWindow.WindowInstances.Count == 0)
                return;

            if (_selectedInstance != null && _enablePinning)
                return;

            var firstInstace = GraphCanvasWindow.WindowInstances[0] as GraphCanvasWindow;
            _selectedInstance = firstInstace.Canvas.CompositionOp; // todo: fix
        }
        private bool _enablePinning = false;
        private Instance _selectedInstance;
    }
}
