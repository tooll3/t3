using ImGuiNET;
using System.Linq;
using T3.Core.Commands;
using T3.Core.Logging;
using T3.Core.Operator;

namespace T3.Gui
{
    /// <summary>
    /// 
    /// </summary>
    class ParameterWindow
    {
        public static void Draw(Instance compositionOp, SymbolChildUi childUi)
        {
            ImGui.Begin("ParameterView");
            {
                if (childUi != null)
                    DrawParameters(compositionOp, childUi);
            }
            ImGui.End();
        }

        public static void DrawParameters(Instance compositionOp, SymbolChildUi selectedChildUi)
        {
            //var compositionOp = _instance._graphCanvasWindows[0].Canvas.CompositionOp; // todo: fix
            //Instance selectedInstance = compositionOp;
            //var childUiEntries = SymbolChildUiRegistry.Entries[compositionOp.Symbol.Id];
            //var selectedChildUi = (from childUi in childUiEntries
            //                       where childUi.Value.IsSelected
            //                       select childUi).FirstOrDefault().Value;

            if (selectedChildUi == null || compositionOp == null)
                return;

            var symbolChild = selectedChildUi.SymbolChild;
            var selectedInstance = compositionOp.Children.SingleOrDefault(child => child.Id == symbolChild.Id);
            if (selectedInstance == null)
                return;

            foreach (var input in selectedInstance.Inputs)
            {
                ImGui.PushID(input.Id.GetHashCode());
                IInputUi inputUi = InputUiRegistry.Entries[selectedInstance.Symbol.Id][input.Id];
                var editState = inputUi.DrawInputEdit(input.Input.InputDefinition.Name, input);
                switch (editState)
                {
                    case InputEditState.SingleCommand:
                        Log.Debug("single command setup");
                        UndoRedoStack.Add(new ChangeInputValueCommand(compositionOp.Symbol, symbolChild.Id, input.Input));
                        break;
                    case InputEditState.Focused:
                        // create command for possible editing
                        Log.Debug("setup 'ChangeInputValue' command");
                        UndoRedoStack.CommandInFlight = new ChangeInputValueCommand(compositionOp.Symbol, symbolChild.Id, input.Input);
                        break;
                    case InputEditState.Modified:
                        // update command in flight
                        Log.Debug("updated 'ChangeInputValue' command");
                        ((ChangeInputValueCommand)UndoRedoStack.CommandInFlight).Value.Assign(input.Input.Value); // todo: ugly!
                        break;
                    case InputEditState.Finished:
                    case InputEditState.ModifiedAndFinished:
                        // add command to undo queue
                        Log.Debug("Finalized 'ChangeInputValue' command");
                        UndoRedoStack.AddCommandInFlightToStack();
                        break;
                }

                ImGui.PopID();
            }


        }


    }
}
