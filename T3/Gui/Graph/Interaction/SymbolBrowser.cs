using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Commands;
using T3.Gui.Graph.Interaction;
using T3.Gui.Styling;

namespace T3.Gui.Graph
{
    /// <summary>
    /// Represents the placeholder for a new <see cref="GraphNode"/> on the <see cref="GraphCanvas"/>. 
    /// It can be connected to other nodes and provide search functionality. It's basically the
    /// T2's CreateOperatorWindow.
    /// </summary>
    public class SymbolBrowser
    {
        private bool _keepNavEnableKeyboard;

        #region public API ------------------------------------------------------------------------
        public void OpenAt(Vector2 positionOnCanvas, Type filterInputType, Type filterOutputType)
        {
            _isOpen = true;
            PosOnCanvas = positionOnCanvas;
            _focusInputNextTime = true;
            _filter.FilterInputType = filterInputType;
            _filter.FilterOutputType = filterOutputType;
            _filter.SearchString = "";
            _selectedSymbol = null;
            _filter.UpdateIfNeccessary();

            // Keep navigation setting to restore after window gets closed
            _keepNavEnableKeyboard = (ImGui.GetIO().ConfigFlags & ImGuiConfigFlags.NavEnableKeyboard) != ImGuiConfigFlags.None;

            if (_selectedSymbol == null && _filter.MatchingSymbols.Count > 0)
            {
                _selectedSymbol = _filter.MatchingSymbols[0];
            }
        }

        public void Draw()
        {
            if (!_isOpen)
            {
                if (ImGui.IsKeyReleased((int)Key.Tab))
                {
                    Log.Debug("open create with tab");
                    OpenAt(GraphCanvas.Current.InverseTransformPosition(ImGui.GetIO().MousePos), null, null);
                }

                return;
            }

            // Disable keyboard navigation to allow cursor up down
            ImGui.GetIO().ConfigFlags &= ~ImGuiConfigFlags.NavEnableKeyboard;
            Current = this;

            _filter.UpdateIfNeccessary();

            ImGui.PushID(UiId);
            {
                _posInWindow = GraphCanvas.Current.ChildPosFromCanvas(PosOnCanvas);
                _posInScreen = GraphCanvas.Current.TransformPosition(PosOnCanvas);
                _drawList = ImGui.GetWindowDrawList();

                DrawMatchesList();
                DrawSearchInput();
            }
            ImGui.PopID();
        }
        #endregion

        #region internal implementation -----------------------------------------------------------
        private void DrawSearchInput()
        {
            ImGui.SetCursorPos(_posInWindow);
            ImGui.SetNextItemWidth(90);

            if (_focusInputNextTime)
            {
                ImGui.SetKeyboardFocusHere();
                _focusInputNextTime = false;
            }

            ImGui.SetCursorPos(_posInWindow + new Vector2(1, 1));
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(7, 6));
            ImGui.SetNextItemWidth(_size.X);
            ImGui.InputText("##filter", ref _filter.SearchString, 10);
            _drawList.AddRect(_posInScreen, _posInScreen + _size, Color.Gray);

            if (ImGui.IsKeyReleased((int)Key.CursorDown))
            {
                if (_filter.MatchingSymbols.Count > 0)
                {
                    var index = _filter.MatchingSymbols.IndexOf(_selectedSymbol);
                    index++;
                    index %= _filter.MatchingSymbols.Count;
                    _selectedSymbol = _filter.MatchingSymbols[index];
                }
            }
            else if (ImGui.IsKeyReleased((int)Key.CursorUp))
            {
                if (_filter.MatchingSymbols.Count > 0)
                {
                    var index = _filter.MatchingSymbols.IndexOf(_selectedSymbol);
                    index--;
                    if (index < 0)
                        index = _filter.MatchingSymbols.Count - 1;

                    _selectedSymbol = _filter.MatchingSymbols[index];
                }
            }
            else if (ImGui.IsKeyPressed((int)Key.Return))
            {
                if (_selectedSymbol != null)
                {
                    CreateInstance(_selectedSymbol);
                }
            }

            if (ImGui.IsItemDeactivated() || ImGui.GetIO().KeysDownDuration[(int)Key.Esc] > 0)
            {
                ConnectionMaker.Cancel();
                Log.Debug("Closing...");
                Close();
            }

            ImGui.PopStyleVar();

            if (ImGui.IsItemActive())
            {
                if (ImGui.IsMouseDragging(0))
                {
                    PosOnCanvas += GraphCanvas.Current.InverseTransformDirection(ImGui.GetIO().MouseDelta);
                }
            }
        }

        private void Close()
        {
            if (_keepNavEnableKeyboard)
                ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;

            _isOpen = false;
        }

        private void DrawMatchesList()
        {
            ImGui.SetCursorPos(_posInWindow + new Vector2(_size.X + 1, 1));
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(5, 5));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(10, 10));

            if (ImGui.BeginChildFrame(999, ResultListSize))
            {
                if (_filter.MatchingSymbols.Count > 0 && !_filter.MatchingSymbols.Contains(_selectedSymbol))
                    _selectedSymbol = _filter.MatchingSymbols[0];
                
                if ((_selectedSymbol == null && SymbolRegistry.Entries.Values.Any()))
                    _selectedSymbol = SymbolRegistry.Entries.Values.FirstOrDefault();

                if (_filter.FilterInputType != null || _filter.FilterOutputType != null)
                {
                    ImGui.PushFont(Fonts.FontSmall);

                    var inputTypeName = _filter.FilterInputType != null
                                            ? TypeNameRegistry.Entries[_filter.FilterInputType]
                                            : string.Empty;

                    var outputTypeName = _filter.FilterOutputType != null
                                             ? TypeNameRegistry.Entries[_filter.FilterOutputType]
                                             : string.Empty;

                    var headerLabel = $"{inputTypeName} -> {outputTypeName}";
                    ImGui.TextDisabled(headerLabel);
                    ImGui.PopFont();
                }

                foreach (var symbol in _filter.MatchingSymbols)
                {
                    ImGui.PushID(symbol.Id.GetHashCode());
                    {
                        ImGui.Selectable("", symbol == _selectedSymbol);

                        if (ImGui.IsItemActivated())
                        {
                            CreateInstance(symbol);
                        }

                        ImGui.SameLine();
                        ImGui.Text(symbol.Name);
                        if (!String.IsNullOrEmpty(symbol.Namespace))
                        {
                            ImGui.SameLine();
                            ImGui.TextDisabled(symbol.Namespace);
                        }
                    }
                    ImGui.PopID();
                }

                var index = _filter.MatchingSymbols.IndexOf(_selectedSymbol);
                var rectMin = ImGui.GetWindowContentRegionMin() + ImGui.GetWindowPos() + new Vector2(0, (index + 1) * ImGui.GetFrameHeight());
                var rectMax = rectMin + new Vector2(10, 10);
                var isVisible = ImGui.IsRectVisible(rectMin, rectMax);

                if (!isVisible)
                {
                    var keepPos = ImGui.GetCursorScreenPos();
                    ImGui.SetCursorScreenPos(rectMin);
                    ImGui.SetScrollHereY(0);
                    ImGui.SetCursorScreenPos(keepPos);
                }
            }

            ImGui.EndChildFrame();
            ImGui.PopStyleVar(2);
        }

        private void CreateInstance(Symbol symbol)
        {
            var parent = GraphCanvas.Current.CompositionOp.Symbol;
            var childUi= NodeOperations.CreateInstance(symbol, parent, PosOnCanvas);
            GraphCanvas.Current.SelectionHandler.SetElement(childUi);

            if (ConnectionMaker.TempConnection != null && symbol.InputDefinitions.Any())
            {
                var temp = ConnectionMaker.TempConnection;
                if (temp.SourceParentOrChildId == ConnectionMaker.UseDraftChildId)
                {
                    // connecting to output
                    ConnectionMaker.CompleteConnectionFromBuiltNode(parent, childUi.SymbolChild, _filter.GetOutputMatchingType(symbol, _filter.FilterInputType));
                }
                else
                {
                    // connecting to input
                    ConnectionMaker.CompleteConnectionIntoBuiltNode(parent, childUi.SymbolChild, _filter.GetInputMatchingType(symbol, _filter.FilterInputType));
                }
            }
            else
            {
                ConnectionMaker.Cancel();
            }

            Close();
        }


        private readonly SymbolFilter _filter = new SymbolFilter();
        #endregion



        //public List<Symbol.Connection> ConnectionsIn = new List<Symbol.Connection>();
        // public Symbol.Connection ConnectionOut = null;

        public Vector2 PosOnCanvas { get; private set; }

        public Vector2 OutputPositionOnScreen => _posInScreen + _size;

        private readonly Vector2 _size = SymbolChildUi.DefaultOpSize;

        private bool _focusInputNextTime;
        private Vector2 _posInScreen;
        private ImDrawListPtr _drawList;
        private Vector2 _posInWindow;

        private bool _isOpen;
        private static readonly Vector2 ResultListSize = new Vector2(300, 200);

        private Symbol _selectedSymbol;
        private static readonly int UiId = "DraftNode".GetHashCode();

        public static SymbolBrowser Current;
    }
}