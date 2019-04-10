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


        public bool Draw()
        {
            if (_refocus)
            {
                ImGui.SetNextWindowFocus();
            }

            if (_opened && ImGui.Begin(WindowTitle, ref _opened))
            {
                if (_refocus)
                    ImGui.SetKeyboardFocusHere(0);

                if (ImGui.InputText("Search", ref _searchInput, maxLength: 20))
                {

                }

                DrawOpList();

                _refocus = false;
                ImGui.End();
            }

            return _opened;
        }


        private void DrawOpList()
        {
            ImGui.Separator();

            foreach (var symbol in SymbolRegistry.Entries.Values)
            {
                ImGui.PushID(symbol.Id.ToString());
                {
                    if (ImGui.Selectable(symbol.SymbolName, symbol == _selectedSymbol))
                    {
                        Guid newInstanceId = _compositionOp.AddChild(symbol);
                        // create and register ui info for new op
                        var uiEntriesForCompositionOp = InstanceUiRegistry.Instance.UiEntries[_compositionOp.Id];
                        uiEntriesForCompositionOp.Add(_compositionOp.Id, new SymbolChildUi { SymbolChild = _compositionOp._children.Find(entry => entry.InstanceId == newInstanceId)});

                        _opened = false;
                    }
                }
                ImGui.PopID();
            }
        }


        public static void OpenAtPosition(Vector2 position, Symbol compositionOp, Vector2 positionInOp)
        {
            ImGui.SetWindowPos(_instance.WindowTitle, position);
            _instance._refocus = true;
            _instance._compositionOp = compositionOp;
            _instance._positionInOp = positionInOp;
            _opened = true;
        }

        private string WindowTitle => "Find Operator##" + _windowGui;
        private Symbol _compositionOp = null;
        private Vector2 _positionInOp;
        private Symbol _selectedSymbol;


        private static bool _opened = false;
        public bool _refocus = false;
        private string _searchInput = "";
        private Guid _windowGui = Guid.NewGuid();
        private static QuickCreateWindow _instance = null;
    }
}
