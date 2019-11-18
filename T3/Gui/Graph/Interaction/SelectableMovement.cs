using ImGuiNET;
using System;
using T3.Core.Logging;
using T3.Gui.Commands;
using T3.Gui.Selection;

namespace T3.Gui.Graph
{
    public static class SelectableMovement
    {
        private static ChangeSelectableCommand _moveCommand = null;

        public static void Handle(ISelectable selectable)
        {
            if (ImGui.IsItemActive())
            {
                if (ImGui.IsItemClicked(0))
                {
                    var handler = GraphCanvas.Current.SelectionHandler;
                    
                    if (!handler.SelectedElements.Contains(selectable))
                    {
                        if (!ImGui.GetIO().KeyShift)
                        {
                            handler.SetElement(selectable);
                        }
                        else
                        {
                            handler.AddElement(selectable);
                        }
                    }
                    else
                    {
                        if (ImGui.GetIO().KeyShift)
                        {
                            handler.RemoveElement(selectable);
                        }
                    }

                    Guid compositionSymbolId = GraphCanvas.Current.CompositionOp.Symbol.Id;
                    _moveCommand = new ChangeSelectableCommand(compositionSymbolId, handler.SelectedElements);
                }

                if (ImGui.IsMouseDragging(0))
                {
                    foreach (var e in GraphCanvas.Current.SelectionHandler.SelectedElements)
                    {
                        e.PosOnCanvas += GraphCanvas.Current.InverseTransformDirection(ImGui.GetIO().MouseDelta);
                    }
                }
            }
            else if (ImGui.IsMouseReleased(0) && _moveCommand != null)
            {
                if (ImGui.GetMouseDragDelta(0).LengthSquared() > 0.0f)
                {
                    // add to stack
                    Log.Debug("Added to undo stack");
                    _moveCommand.StoreCurrentValues();
                    UndoRedoStack.Add(_moveCommand);
                }

                _moveCommand = null;
            }
        }
    }
}