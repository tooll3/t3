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
            if (GraphCanvasWindow.WindowInstances.Count == 0)
                return;

            var firstInstace = GraphCanvasWindow.WindowInstances[0] as GraphCanvasWindow;

            Instance selectedInstance = firstInstace.Canvas.CompositionOp; // todo: fix
            SymbolUi selectedUi = SymbolUiRegistry.Entries[selectedInstance.Symbol.Id];
            var selectedChildUi = selectedUi.ChildUis.FirstOrDefault(childUi => childUi.IsSelected);
            if (selectedChildUi != null)
            {
                selectedInstance = selectedInstance.Children.Single(child => child.Id == selectedChildUi.Id);
                selectedUi = SymbolUiRegistry.Entries[selectedInstance.Symbol.Id];
            }

            if (selectedInstance.Outputs.Count > 0)
            {
                var firstOutput = selectedInstance.Outputs[0];
                IOutputUi outputUi = selectedUi.OutputUis[firstOutput.Id];
                outputUi.DrawValue(firstOutput);
            }
        }
    }
}
