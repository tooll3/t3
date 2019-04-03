using ImGuiNET;
using System;
using System.Numerics;
using T3.Core.Operator;

namespace T3.Gui.graph
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



            if (_opened && ImGui.Begin(_windowTitle, ref _opened))
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

            foreach (var symbol in SymbolRegistry.Instance.Definitions.Values)
            {
                ImGui.PushID(symbol.Id.ToString());
                {
                    if (ImGui.Selectable(symbol.SymbolName, symbol == _selectedSymbol))
                    {
                        var newChild = new SymbolChild() { InstanceId = Guid.NewGuid(), Symbol = symbol };
                        symbol._children.Add(newChild);
                        _opened = false;
                    }
                }
                ImGui.PopID();
            }
        }


        static public void OpenAtPosition(Vector2 position, Symbol homeOp, Vector2 positionInOp)
        {
            if (_instance == null)
            {
                throw (new NotImplementedException("Quick Create window hasn't be initialized"));
            }
            ImGui.SetWindowPos(_instance._windowTitle, position);
            _instance._refocus = true;
            _instance._parentOp = homeOp;
            _instance._positionInOp = positionInOp;
            _opened = true;
        }

        private string _windowTitle { get { return "Find Operator##" + _windowGui; } }
        private Symbol _parentOp = null;
        private Vector2 _positionInOp;
        private Symbol _selectedSymbol;


        private static bool _opened = false;
        public bool _refocus = false;
        private string _searchInput = "";
        private Guid _windowGui = Guid.NewGuid();
        private static QuickCreateWindow _instance = null;
    }
}
