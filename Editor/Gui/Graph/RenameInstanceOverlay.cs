using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.IO;
using T3.Core.Logging;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Styling;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Graph
{
    /// <summary>
    /// If active renders a small input field above a symbolChildUi. Handles its state 
    /// </summary>
    public static class RenameInstanceOverlay
    {
        public static void OpenForSymbolChildUi(SymbolChildUi symbolChildUi)
        {
            _nextFocusedInstanceId = symbolChildUi.SymbolChild.Id;
        }

        private static Guid _nextFocusedInstanceId = Guid.Empty;

        public static void Draw()
        {
            var justOpened = false;

            var renameTriggered = _nextFocusedInstanceId != Guid.Empty;
            
            if (_focusedInstanceId == Guid.Empty)
            {
                if ((renameTriggered || ImGui.IsWindowFocused(ImGuiFocusedFlags.ChildWindows) || ImGui.IsWindowFocused()) 
                    && !ImGui.IsAnyItemActive() 
                    && !ImGui.IsAnyItemFocused() 
                    && (renameTriggered || ImGui.IsKeyPressed((ImGuiKey)Key.Return))
                    && string.IsNullOrEmpty(FrameStats.Current.OpenedPopUpName))
                {
                    var selectedInstances = NodeSelection.GetSelectedNodes<SymbolChildUi>().ToList();
                    if (_nextFocusedInstanceId != Guid.Empty)
                    {
                        _focusedInstanceId = _nextFocusedInstanceId;
                        _nextFocusedInstanceId = Guid.Empty;
                        justOpened = true;
                        ImGui.SetKeyboardFocusHere();

                    }
                    else if (selectedInstances.Count == 1)
                    {
                        _focusedInstanceId = selectedInstances[0].SymbolChild.Id;
                        justOpened = true;
                        ImGui.SetKeyboardFocusHere();
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

            ImGui.SetCursorScreenPos(positionInScreen + Vector2.One);
            
            var text = symbolChild.Name;
            //ImGui.SetNextItemWidth(160);
            CustomComponents.DrawInputFieldWithPlaceholder("Untitled", ref text, 200, false, ImGuiInputTextFlags.AutoSelectAll);
            //ImGui.InputText("##input", ref text, 256, ImGuiInputTextFlags.AutoSelectAll);
            symbolChild.Name = text;
            
            if (!justOpened && (ImGui.IsItemDeactivated() || ImGui.IsKeyPressed((ImGuiKey)Key.Return)))
            {
                _focusedInstanceId = Guid.Empty;
            }
        }

        public static bool IsOpen => _focusedInstanceId != Guid.Empty;
        
        private static Guid _focusedInstanceId;
    }
}