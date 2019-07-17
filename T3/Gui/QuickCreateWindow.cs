using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using T3.Core.Operator;
using T3.Gui.Commands;

namespace T3.Gui.Graph
{
    /// <summary>
    /// Shows quick search for creating a new Operator <see cref="Instance"/>
    /// </summary>
    public class QuickCreateWindow
    {
        public QuickCreateWindow()
        {
            _instance = this;
        }


        public void Draw()
        {
            if (!_opened)
                return;


            // Pushing window to front has to be done before Begin()
            if (_bringWindowToFront)
                ImGui.SetNextWindowFocus();

            if (ImGui.Begin(WindowTitle, ref _opened))
            {
                if (_bringWindowToFront)
                {
                    ImGui.SetKeyboardFocusHere(0);
                    ImGui.SetWindowPos(_instance.WindowTitle, _positionInScreen); // Setting its position, after Begin()
                }

                if (ImGui.InputText("Search", ref _searchInput, maxLength: 20))
                {

                }

                DrawSymbolList();
                _bringWindowToFront = false;
            }
            ImGui.End();
        }


        private void DrawSymbolList()
        {
            ImGui.Separator();
            var parentSymbols = GraphCanvas.Current.GetParentSymbols().ToArray();

            foreach (var symbol in SymbolRegistry.Entries.Values)
            {
                ImGui.PushID(symbol.Id.GetHashCode());

                var flags = parentSymbols.Contains(symbol)
                                ? ImGuiSelectableFlags.Disabled
                                : ImGuiSelectableFlags.None;

                if (ImGui.Selectable(symbol.Name, symbol == _selectedSymbol, flags))
                {
                    UndoRedoStack.AddAndExecute(new AddSymbolChildCommand(_compositionOp, symbol.Id) { PosOnCanvas = _positionInOp });
                    _opened = false;
                }
                ImGui.PopID();
            }
        }


        public static void OpenAtPosition(Vector2 screenPosition, Symbol compositionOp, Vector2 positionInOp)
        {
            _instance._bringWindowToFront = true;
            _instance._positionInScreen = screenPosition;
            _instance._compositionOp = compositionOp;
            _instance._positionInOp = positionInOp;
            _opened = true;
        }

        private string WindowTitle => "Find Operator";
        private Symbol _compositionOp = null;
        private Vector2 _positionInOp;
        private Symbol _selectedSymbol = null;
        private Vector2 _positionInScreen;


        private static bool _opened = false;
        public bool _bringWindowToFront = false;
        private string _searchInput = "";

        //private Guid _windowGui = Guid.NewGuid();
        private static QuickCreateWindow _instance = null;
    }
}
