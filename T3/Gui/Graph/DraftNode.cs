using ImGuiNET;
using imHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Selection;

namespace T3.Gui.Graph
{
    /// <summary>
    /// Represents the place holder of a new Node on the <see cref="GraphCanvas"/> that can be
    /// already connected to other nodes and provide search functionality. It's basically the
    /// T2's CreateOperatorWindow.
    /// </summary>
    public class DraftNode : ISelectable
    {
        public void Draw()
        {
            if (!_opened)
                return;

            ImGui.PushID(_uiId);
            {
                _posInWindow = GraphCanvas.Current.ChildPosFromCanvas(PosOnCanvas);
                var drawList = ImGui.GetWindowDrawList();
                var screenPos = GraphCanvas.Current.TransformPosition(PosOnCanvas);
                drawList.AddRect(screenPos, screenPos + Size, Color.Gray);

                ImGui.SetCursorPos(_posInWindow);
                ImGui.SetNextItemWidth(90);

                if (_focusInputNextTime)
                {
                    ImGui.SetKeyboardFocusHere();
                    _focusInputNextTime = false;
                }
                ImGui.SetCursorPos(_posInWindow + new Vector2(1, 1));
                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(7, 6));
                if (ImGui.InputTextWithHint("", "search", _searchString, 10))
                {
                    //Log.Debug("completed search input");
                }
                ImGui.PopStyleVar();

                if (ImGui.IsItemActive())
                {
                    if (ImGui.IsMouseDragging(0))
                    {
                        PosOnCanvas += GraphCanvas.Current.InverseTransformDirection(ImGui.GetIO().MouseDelta);
                    }
                }
                DrawMatchesList();
            }
            ImGui.PopID();
        }

        private Vector2 _posInWindow;
        private void DrawMatchesList()
        {
            ImGui.SetCursorPos(_posInWindow + new Vector2(91 + 8, 1));
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(5, 5));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(10, 10));
            if (ImGui.BeginChildFrame(234, new Vector2(150, 200)))
            {
                ImGui.PushFont(ImGuiDx11Impl.FontSmall);
                ImGui.TextDisabled("->float");
                ImGui.PopFont();

                var parentSymbols = new List<Symbol>(GraphCanvas.Current.GetParentSymbols());
                if (_selectedSymbol == null && SymbolRegistry.Entries.Values.Any())
                    _selectedSymbol = SymbolRegistry.Entries.Values.FirstOrDefault();

                foreach (var symbol in SymbolRegistry.Entries.Values)
                {
                    ImGui.PushID(symbol.Id.GetHashCode());

                    if (parentSymbols.Contains(symbol))
                        continue;

                    if (ImGui.Selectable("", symbol == _selectedSymbol))
                    {
                        Guid newSymbolChildId = GraphCanvas.Current.CompositionOp.Symbol.AddChild(symbol);

                        // Create and register ui info for new child
                        var uiEntriesForChildrenOfSymbol = SymbolChildUiRegistry.Entries[GraphCanvas.Current.CompositionOp.Symbol.Id];
                        uiEntriesForChildrenOfSymbol.Add(newSymbolChildId, new SymbolChildUi
                        {
                            SymbolChild = GraphCanvas.Current.CompositionOp.Symbol.Children.Find(entry => entry.Id == newSymbolChildId),
                            PosOnCanvas = _posInWindow,
                        });

                        _opened = false;
                    }
                    ImGui.SameLine();
                    ImGui.Text(symbol.Name);
                    ImGui.SameLine();
                    ImGui.TextDisabled(symbol.Namespace);
                    ImGui.PopID();
                }
            }
            ImGui.EndChildFrame();
            ImGui.PopStyleVar(2);
        }


        public void OpenAt(Vector2 positionOnCanvas)
        {
            _opened = true;
            PosOnCanvas = positionOnCanvas;
            _focusInputNextTime = true;

        }
        private bool _focusInputNextTime = false;

        public Vector2 PosOnCanvas { get; set; }

        public Vector2 Size { get; set; } = GraphCanvas.DefaultOpSize;

        public bool IsSelected { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        private bool _opened = false;
        private Symbol _selectedSymbol = null;
        private readonly static int _uiId = "DraftNode".GetHashCode();
        private string _searchString = "";
    }
}
