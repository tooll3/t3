using System;
using System.Linq;
using ImGuiNET;
using T3.Core.IO;
using T3.Core.Logging;
using T3.Gui.Selection;

namespace T3.Gui.Graph
{
    /// <summary>
    /// If active renders a small input field above a symbolChildUi. Handles its state 
    /// </summary>
    public static class RenameInstanceOverlay
    {
        public static void OpenForSymbolChildUi(SymbolChildUi symbolChildUi)
        {
            _focusedInstanceId = symbolChildUi.SymbolChild.Id;
        }

        public static void Draw()
        {
            var justOpened = false;
            if (_focusedInstanceId == Guid.Empty)
            {
                if (ImGui.IsWindowFocused()
                    && !ImGui.IsAnyItemActive() 
                    && !ImGui.IsAnyItemFocused() 
                    && ImGui.IsKeyPressed((int)Key.Return)
                    && string.IsNullOrEmpty(T3Ui.OpenedPopUpName))
                {
                    var selectedInstances = SelectionManager.GetSelectedNodes<SymbolChildUi>().ToList();
                    if (selectedInstances.Count == 1)
                    {
                        justOpened = true;
                        ImGui.SetKeyboardFocusHere();
                        _focusedInstanceId = selectedInstances[0].SymbolChild.Id;
                    }
                }
            }

            if (_focusedInstanceId == Guid.Empty)
                return;

            var symbolChild = GraphCanvas.Current.CompositionOp.Symbol.Children.SingleOrDefault(child => child.Id == _focusedInstanceId);
            if (symbolChild == null)
            {
                Log.Error("canceling rename overlay of no longer valid selection");
                _focusedInstanceId = Guid.Empty;
                return;
            }
            var parentSymbolUi = SymbolUiRegistry.Entries[GraphCanvas.Current.CompositionOp.Symbol.Id];
            var symbolChildUi = parentSymbolUi.ChildUis.Single(child => child.Id == _focusedInstanceId);

            var positionInScreen = GraphCanvas.Current.TransformPosition(symbolChildUi.PosOnCanvas);

            ImGui.SetCursorScreenPos(positionInScreen);
            
            var text = symbolChild.Name;
            ImGui.SetNextItemWidth(150);
            ImGui.InputText("##input", ref text, 256, ImGuiInputTextFlags.AutoSelectAll);
            symbolChild.Name = text;
            
            //ImGui.SetKeyboardFocusHere();
            if (!justOpened && (ImGui.IsItemDeactivated() || ImGui.IsKeyPressed((int)Key.Return)))
            {
                _focusedInstanceId = Guid.Empty;
            }
        }

        public static bool IsOpen => _focusedInstanceId != Guid.Empty;
        
        private static Guid _focusedInstanceId;
    }
}