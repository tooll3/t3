using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using T3.Core.Operator;
using T3.Gui.Graph;

namespace T3.Gui.Windows
{
    /// <summary>
    /// A helper that decides with graph element to show. This is used by <see cref="OutputWindow"/> and <see cref="ParameterWindow"/>.
    /// </summary>
    public class SelectionPinning
    {
        public void UpdateSelection()
        {
            if (!_enablePinning || _pinnedInstance == null || _pinnedUi == null)
            {
                SelectedChildUi = null;
                SelectedInstance = null;
                SelectedUi = null;

                if (GraphCanvas.Current.CompositionOp == null)
                    return;

                SelectedInstance = GraphCanvas.Current.CompositionOp;

                if (SelectedInstance == null)
                {
                    return;
                }

                SelectedUi = SymbolUiRegistry.Entries[SelectedInstance.Symbol.Id];
                SelectedChildUi = SelectedUi.ChildUis.FirstOrDefault(childUi => childUi.IsSelected);
                if (SelectedChildUi != null)
                {
                    SelectedInstance = SelectedInstance.Children.Single(child => child.SymbolChildId == SelectedChildUi.Id);
                    SelectedUi = SymbolUiRegistry.Entries[SelectedInstance.Symbol.Id];
                }
                else
                {
                    var parents = GraphCanvas.Current.GetParents();
                    var parent = parents.LastOrDefault();
                    if (parent != null)
                    {
                        SelectedUi = SymbolUiRegistry.Entries[parent.Symbol.Id];
                        SelectedChildUi = SelectedUi.ChildUis.FirstOrDefault(childUi => childUi.Id == SelectedInstance.SymbolChildId );
                    } 
                }

                _pinnedInstance = SelectedInstance;
                _pinnedUi = SelectedUi;
                _pinnedChildUi = SelectedChildUi;
            }
            else
            {
                SelectedInstance = _pinnedInstance;
                SelectedUi = _pinnedUi;
                SelectedChildUi = _pinnedChildUi;
            }
        }
        
        public void DrawPinning()
        {
            ImGui.Checkbox("pin to...   ", ref _enablePinning);
            ImGui.SameLine();

            if (SelectedInstance != null)
            {
                ImGui.Text(SelectedInstance.Symbol.Name + "   ");
            }
            ImGui.SameLine();
        }

        public Instance SelectedInstance { get; private set; }
        public SymbolUi SelectedUi { get; private set; }
        public SymbolChildUi SelectedChildUi { get; private set; }

        private bool _enablePinning = false;
        private Instance _pinnedInstance = null;
        private SymbolUi _pinnedUi = null;
        private SymbolChildUi _pinnedChildUi = null;
    }
}
