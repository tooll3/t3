using ImGuiNET;
using System;
using System.Numerics;
using T3.Core.Operator;

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

                DrawOpList();
                _bringWindowToFront = false;
            }
            ImGui.End();
        }


        private void DrawOpList()
        {
            ImGui.Separator();

            foreach (var symbol in SymbolRegistry.Entries.Values)
            {
                ImGui.PushID(symbol.Id.GetHashCode());
                {
                    if (ImGui.Selectable(symbol.SymbolName, symbol == _selectedSymbol))
                    {
                        Guid newInstanceId = _compositionOp.AddChild(symbol);
                        // Create and register ui info for new op
                        var uiEntriesForChildrenOfSymbol = SymbolChildUiRegistry.Entries[_compositionOp.Id];
                        uiEntriesForChildrenOfSymbol.Add(newInstanceId, new SymbolChildUi { SymbolChild = _compositionOp.Children.Find(entry => entry.Id == newInstanceId) });

                        _opened = false;
                    }
                }
                ImGui.PopID();
            }
        }


        public static void OpenAtPosition(Vector2 position, Symbol compositionOp, Vector2 positionInOp)
        {
            _instance._bringWindowToFront = true;
            _instance._positionInScreen = position;
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
