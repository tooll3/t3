using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
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
        public void OpenAt(Vector2 positionOnCanvas, Type type)
        {
            _isOpen = true;
            PosOnCanvas = positionOnCanvas;
            _focusInputNextTime = true;
            _filterType = type;
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
                return;

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

        // public void Cancel()
        // {
        //     _isOpen = false;
        // }
        #endregion

        #region internal implementation -----------------------------------------------------------
        private void DrawSearchInput()
        {
            _drawList.AddRect(_posInScreen, _posInScreen + _size, Color.Gray);

            ImGui.SetCursorPos(_posInWindow);
            ImGui.SetNextItemWidth(90);

            if (_focusInputNextTime)
            {
                ImGui.SetKeyboardFocusHere();
                _focusInputNextTime = false;
            }

            ImGui.SetCursorPos(_posInWindow + new Vector2(1, 1));
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(7, 6));
            ImGui.InputText("##filter", ref _searchString, 10);

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
            if(_keepNavEnableKeyboard)
                ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
            
            _isOpen = false;
        }

        private void DrawMatchesList()
        {
            ImGui.SetCursorPos(_posInWindow + new Vector2(91 + 8, 1));
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(5, 5));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(10, 10));

            //var typeUi = TypeUiRegistry.Entries[_filterType];

            if (ImGui.BeginChildFrame(234, new Vector2(150, 200)))
            {
                ImGui.PushFont(Fonts.FontSmall);
                ImGui.TextDisabled(_filterType.Name);
                ImGui.PopFont();

                if (_selectedSymbol == null && SymbolRegistry.Entries.Values.Any())
                    _selectedSymbol = SymbolRegistry.Entries.Values.FirstOrDefault();

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
                            ImGui.TextDisabled(symbol.Namespace);
                            ImGui.SameLine();
                        }
                    }
                    ImGui.PopID();
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

            if (symbol.InputDefinitions.Any())
            {
                var temp = ConnectionMaker.TempConnection;
                if (temp.SourceParentOrChildId == ConnectionMaker.UseDraftChildId)
                {
                    // connecting to output
                    ConnectionMaker.CompleteConnectionFromBuiltNode(parent, newSymbolChild, _filter.GetOutputMatchingType(symbol, _filterType));
                }
                else
                {
                    // connecting to input
                    ConnectionMaker.CompleteConnectionIntoBuiltNode(parent, newSymbolChild, _filter.GetInputMatchingType(symbol, _filterType));
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
                bool needsUpdate = false;

                if (_currentSearchString != _searchString)
                {
                    _currentSearchString = _searchString;
                    var pattern = string.Join(".*", _currentSearchString.ToCharArray());
                    _currentRegex = new Regex(pattern, RegexOptions.IgnoreCase);
                    needsUpdate = true;
                }

                if (_currentType != _filterType)
                {
                    _currentType = _filterType;
                    needsUpdate = true;
                }

                if (needsUpdate)
                {
                    UpdateMatchingSymbols();
                }
            }

            private Type _currentType;
            //public Symbol SelectedSymbol = null;

            private void UpdateMatchingSymbols()
            {
                var parentSymbols = new List<Symbol>(GraphCanvas.Current.GetParentSymbols());

                MatchingSymbols = new List<Symbol>();

                foreach (var symbol in SymbolRegistry.Entries.Values)
                {
                    if (parentSymbols.Contains(symbol))
                        continue;

                    var matchingInputDef = GetInputMatchingType(symbol, _filterType);
                    if (matchingInputDef == null)
                        continue;

                    if (!_currentRegex.IsMatch(symbol.Name))
                        continue;

                    MatchingSymbols.Add(symbol);
                }
            }

            public Symbol.InputDefinition GetInputMatchingType(Symbol symbol, Type type)
            {
                foreach (var inputDefinition in symbol.InputDefinitions)
                {
                    if (inputDefinition.DefaultValue.ValueType == type)
                        return inputDefinition;
                }

                return null;
            }

            public Symbol.OutputDefinition GetOutputMatchingType(Symbol symbol, Type type)
            {
                foreach (var outputDefinition in symbol.OutputDefinitions)
                {
                    if (outputDefinition.ValueType == type)
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

        private static Type _filterType;

        //public List<Symbol.Connection> ConnectionsIn = new List<Symbol.Connection>();
        // public Symbol.Connection ConnectionOut = null;

        public Vector2 PosOnCanvas { get; private set; }
        private readonly Vector2 _size  = GraphCanvas.DefaultOpSize;

        private bool _focusInputNextTime;
        private Vector2 _posInScreen;
        private ImDrawListPtr _drawList;
        private Vector2 _posInWindow;

        private bool _isOpen;

        private Symbol _selectedSymbol;
        private static readonly int UiId = "DraftNode".GetHashCode();
        private static string _searchString = "";

        public static SymbolBrowser Current;
    }
}