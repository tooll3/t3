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
            _filterInputType = filterInputType;
            _filterOutputType = filterOutputType;
            _searchString = "";
            _selectedSymbol = null;
            _filter.Update();

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

            _filter.Update();

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
            ImGui.InputText("##filter", ref _searchString, 10);
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

                if (_filterInputType != null || _filterOutputType != null)
                {
                    ImGui.PushFont(Fonts.FontSmall);

                    var inputTypeName = _filterInputType != null
                                            ? TypeNameRegistry.Entries[_filterInputType]
                                            : string.Empty;

                    var outputTypeName = _filterOutputType != null
                                             ? TypeNameRegistry.Entries[_filterOutputType]
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
            var addCommand = new AddSymbolChildCommand(parent, symbol.Id) { PosOnCanvas = PosOnCanvas };
            UndoRedoStack.AddAndExecute(addCommand);
            var newSymbolChild = parent.Children.Single(entry => entry.Id == addCommand.AddedChildId);

            // Select new node
            var symbolUi = SymbolUiRegistry.Entries[GraphCanvas.Current.CompositionOp.Symbol.Id];
            var childUi = symbolUi.ChildUis.Find(s => s.Id == newSymbolChild.Id);
            GraphCanvas.Current.SelectionHandler.SetElement(childUi);

            if (ConnectionMaker.TempConnection != null && symbol.InputDefinitions.Any())
            {
                var temp = ConnectionMaker.TempConnection;
                if (temp.SourceParentOrChildId == ConnectionMaker.UseDraftChildId)
                {
                    // connecting to output
                    ConnectionMaker.CompleteConnectionFromBuiltNode(parent, newSymbolChild, _filter.GetOutputMatchingType(symbol, _filterInputType));
                }
                else
                {
                    // connecting to input
                    ConnectionMaker.CompleteConnectionIntoBuiltNode(parent, newSymbolChild, _filter.GetInputMatchingType(symbol, _filterInputType));
                }
            }
            else
            {
                ConnectionMaker.Cancel();
            }

            Close();
        }

        /// <summary>
        /// Provides a regular expression to filter for matching <see cref="Symbol"/>s
        /// </summary>
        private class Filter
        {
            public void Update()
            {
                var needsUpdate = false;

                if (_currentSearchString != _searchString)
                {
                    _currentSearchString = _searchString;
                    var pattern = string.Join(".*", _currentSearchString.ToCharArray());
                    _currentRegex = new Regex(pattern, RegexOptions.IgnoreCase);
                    needsUpdate = true;
                }

                if (_inputType != _filterInputType)
                {
                    _inputType = _filterInputType;
                    needsUpdate = true;
                }

                if (_outputType != _filterOutputType)
                {
                    _outputType = _filterOutputType;
                    needsUpdate = true;
                }

                if (needsUpdate)
                {
                    UpdateMatchingSymbols();
                }
            }

            private Type _inputType;
            private Type _outputType;

            private void UpdateMatchingSymbols()
            {
                var parentSymbols = new List<Symbol>(GraphCanvas.Current.GetParentSymbols());

                MatchingSymbols = new List<Symbol>();

                foreach (var symbol in SymbolRegistry.Entries.Values)
                {
                    if (parentSymbols.Contains(symbol))
                        continue;

                    if (_inputType != null)
                    {
                        var matchingInputDef = GetInputMatchingType(symbol, _filterInputType);
                        if (matchingInputDef == null)
                            continue;
                    }

                    if (_outputType != null)
                    {
                        var matchingOutputDef = GetOutputMatchingType(symbol, _filterOutputType);
                        if (matchingOutputDef == null)
                            continue;
                    }

                    if (!_currentRegex.IsMatch(symbol.Name))
                        continue;

                    MatchingSymbols.Add(symbol);
                }
            }

            public Symbol.InputDefinition GetInputMatchingType(Symbol symbol, Type type)
            {
                foreach (var inputDefinition in symbol.InputDefinitions)
                {
                    if (type == null || inputDefinition.DefaultValue.ValueType == type)
                        return inputDefinition;
                }

                return null;
            }

            public Symbol.OutputDefinition GetOutputMatchingType(Symbol symbol, Type type)
            {
                foreach (var outputDefinition in symbol.OutputDefinitions)
                {
                    if (type == null || outputDefinition.ValueType == type)
                        return outputDefinition;
                }

                return null;
            }

            public List<Symbol> MatchingSymbols { private set; get; } = null;

            private Regex _currentRegex;
            private string _currentSearchString;

            // internal bool Match(Symbol symbol)
            // {
            //     return _currentRegex.IsMatch(symbol.Name);
            // }
        }

        private readonly Filter _filter = new Filter();
        #endregion

        private static Type _filterInputType;
        private static Type _filterOutputType;

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
        private static string _searchString = "";

        public static SymbolBrowser Current;
    }
}