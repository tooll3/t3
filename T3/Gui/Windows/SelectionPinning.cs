using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using T3.Core.Operator;
using T3.Gui.Graph;
using T3.Gui.Graph.Interaction;
using T3.Gui.Selection;
using T3.Gui.Windows.Output;

namespace T3.Gui.Windows
{
    /// <summary>
    /// A helper that decides with graph element to show.
    /// This is used by <see cref="OutputWindow"/> and <see cref="ParameterWindow"/>.
    /// </summary>
    public class SelectionPinning
    {
        public void DrawPinning()
        {
            if (ImGui.Checkbox("pin to...   ", ref _enablePinning))
            {
                if (_enablePinning)
                {
                    _pinnedInstancePath = NodeOperations.BuildIdPathForInstance(SelectionManager.GetSelectedInstance());
                }
            }
            ImGui.SameLine();

            var selectedInstance = GetSelectedInstance();
            if (selectedInstance != null)
            {
                ImGui.Text(selectedInstance.Symbol.Name + "   ");
            }
            ImGui.SameLine();
        }

        public Instance GetSelectedInstance()
        {
            if (!_enablePinning)
                return SelectionManager.GetSelectedInstance();
            
            var instance = NodeOperations.GetInstanceFromIdPath(_pinnedInstancePath);
            return instance ?? SelectionManager.GetSelectedInstance();
        } 

        private bool _enablePinning;
        private List<Guid> _pinnedInstancePath;
    }
}
