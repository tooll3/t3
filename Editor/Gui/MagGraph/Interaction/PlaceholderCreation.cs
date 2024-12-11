using System.Diagnostics;
using ImGuiNET;
using T3.Core.DataTypes.Vector;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Core.Utils;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Commands.Graph;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.MagGraph.Model;
using T3.Editor.Gui.MagGraph.States;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows;
using T3.Editor.UiModel;
using T3.SystemUi;

namespace T3.Editor.Gui.MagGraph.Interaction;

/// <summary>
/// Controls when and how a placeholder is shown and used to create new items / outputs etc.
/// </summary>
internal sealed class PlaceholderCreation
{
    internal MagGraphItem? PlaceholderItem;

    private sealed class PlaceholderSelectable : ISelectableCanvasObject
    {
        public Guid Id { get; }
        public Vector2 PosOnCanvas { get; set; }
        public Vector2 Size { get; set; }
    }

    private static readonly PlaceholderSelectable _placeHolderSelectable = new();

    public static Guid PlaceHolderId = Guid.Parse("ffffffff-eeee-47C7-A17F-E297672EE1F3");

    public void OpenToSplitHoveredConnections(GraphUiContext context)
    {
        if (context.ConnectionHovering.ConnectionHoversWhenClicked.Count == 0)
        {
            Log.Warning("No connections found? ");
            context.StateMachine.SetState(GraphStates.Default, context);
            return;
        }
        
        context.TempConnections.Clear();
        
        // TODO: This will crash... 
        context.StartMacroCommand("Insert operator");
        //context.MacroCommand??= new MacroCommand("Insert Operator");
        var posOnCanvas = context.Canvas.InverseTransformPositionFloat(ImGui.GetMousePos());

        var firstHover = context.ConnectionHovering.ConnectionHoversWhenClicked[0];
        
        // Add temp connection into placeholder...
        var tempConnectionIn = new MagGraphConnection
                                 {
                                     Style = MagGraphConnection.ConnectionStyles.Unknown,
                                     SourcePos = firstHover.Connection.SourcePos,
                                     SourceItem = firstHover.Connection.SourceItem,
                                     SourceOutput = firstHover.Connection.SourceOutput,
                                     TargetPos = default,
                                     TargetItem = null,
                                     OutputLineIndex = 0,
                                     VisibleOutputIndex = 0,
                                     ConnectionHash = 0,
                                     IsTemporary = true,
                                 };
        context.TempConnections.Add(tempConnectionIn);
        

        
        PlaceholderItem = new MagGraphItem
                              {
                                  Selectable = _placeHolderSelectable,
                                  PosOnCanvas = posOnCanvas,
                                  Id = PlaceHolderId,
                                  Size = MagGraphItem.GridSize,
                                  Variant = MagGraphItem.Variants.Placeholder,
                              };
        
        context.Layout.Items[PlaceHolderId] = PlaceholderItem;
        _focusInputNextTime = true;
        
        _filter.FilterInputType = firstHover.Connection.Type;
        _filter.FilterOutputType = firstHover.Connection.Type;
        _filter.WasUpdated = true;
        _filter.SearchString = string.Empty;
        _selectedSymbolUi = null;
        _favoriteGroup = string.Empty;
        _opGroups = GetOperatorSuggestions();
        _filter.UpdateIfNecessary(context.Selector, forceUpdate: true);
        
        //context.PrimaryOutputItem = context.ActiveItem;
        //context.DraggedPrimaryOutputType = output.ValueType;
        context.StateMachine.SetState(GraphStates.Placeholder, context);
    }
    
    public void OpenOnCanvas(GraphUiContext context, Vector2 posOnCanvas, Type inputTypeFilter = null)
    {
        //context.MacroCommand??= new MacroCommand("Insert Operator");
        context.StartMacroCommand("Insert Operator");

        PlaceholderItem = new MagGraphItem
                              {
                                  Selectable = _placeHolderSelectable,
                                  PosOnCanvas = posOnCanvas,
                                  Id = PlaceHolderId,
                                  Size = MagGraphItem.GridSize,
                                  Variant = MagGraphItem.Variants.Placeholder,
                              };

        context.Layout.Items[PlaceHolderId] = PlaceholderItem;
        _focusInputNextTime = true;

        //_filter.PresetFilterString = string.Empty;
        _filter.FilterInputType = inputTypeFilter;
        _filter.WasUpdated = true;
        _filter.SearchString = string.Empty;
        _selectedSymbolUi = null;
        _favoriteGroup = string.Empty;
        _opGroups = GetOperatorSuggestions();
        _filter.UpdateIfNecessary(context.Selector, forceUpdate: true);

        _selectedItemChanged = true;
    }

    private MagGraphItem _focusedItem;

    public void OpenForItem(GraphUiContext context, ISelectableCanvasObject item, MagGraphItem.Directions direction = MagGraphItem.Directions.Vertical)
    {
        if (!context.Layout.Items.TryGetValue(item.Id, out _focusedItem))
        {
            Log.Warning("Can't find item graph?");
            return;
        }
        
        context.StartMacroCommand("Insert Operator");
        //context.MacroCommand = new MacroCommand("Insert Operator");
        
        var focusedItemPosOnCanvas = direction == MagGraphItem.Directions.Vertical 
        ? _focusedItem.PosOnCanvas + new Vector2(0, _focusedItem.Size.Y)
          : _focusedItem.PosOnCanvas + new Vector2(_focusedItem.Size.X, 0);
        
        
        PlaceholderItem = new MagGraphItem
                              {
                                  Selectable = _placeHolderSelectable,
                                  PosOnCanvas = focusedItemPosOnCanvas,
                                  Id = PlaceHolderId,
                                  Size = MagGraphItem.GridSize,
                                  Variant = MagGraphItem.Variants.Placeholder,
                              };

        // Make space vertically
        if (direction == MagGraphItem.Directions.Vertical)
        {
            // Keep for after creation because inserted node might exceed unit height and further pushing is required... 
            _snappedItems = MagItemMovement.CollectSnappedItems(_focusedItem);

            MagItemMovement
               .MoveSnappedItemsVertically(context,
                                           _snappedItems,
                                           _focusedItem.PosOnCanvas.Y + _focusedItem.Size.Y - MagGraphItem.GridSize.Y / 2,
                                           MagGraphItem.GridSize.Y);
        }

        
        context.Selector.Selection.Clear();
        context.Layout.Items[PlaceHolderId] = PlaceholderItem;
        _focusInputNextTime = true;

        // Initialize filters
        _filter.WasUpdated = true;
        _filter.SearchString = string.Empty;
        _selectedSymbolUi = null;
        _favoriteGroup = string.Empty;
        _opGroups = GetOperatorSuggestions();
        
        if (_focusedItem.OutputLines.Length > 0)
        {
            _filter.FilterInputType = _focusedItem.OutputLines[0].Output.ValueType;

            if (_focusedItem.OutputLines[0].ConnectionsOut.Count > 0)
                _filter.FilterOutputType = _focusedItem.OutputLines[0].Output.ValueType;
        }

        _filter.UpdateIfNecessary(context.Selector, forceUpdate: true);
        _selectedItemChanged = true;
    }

    public void Cancel(GraphUiContext context)
    {
        //context.MacroCommand?.Undo();
        if(context.MacroCommand != null)
            context.CancelMacroCommand();
        
        if (_focusedItem != null)
        {
            context.Selector.SetSelection(_focusedItem.Selectable, _focusedItem.Instance);
        }

        Reset(context);
    }

    private void Close(GraphUiContext context)
    {
        context.CompleteMacroCommand();
        //if (context.MacroCommand != null)
        //    UndoRedoStack.Add(context.MacroCommand);

        Reset(context);
    }

    internal void Reset(GraphUiContext context)
    {
        if (context.MacroCommand != null)
        {
            Log.Debug("cancelling placeholder command... " + context.MacroCommand);
            context.CancelMacroCommand();
        }
        _filter.Reset();
        _focusedItem = null;
        context.ConnectionHovering.ConnectionHoversWhenClicked.Clear();

        if (PlaceholderItem == null)
            return;

        context.Layout.Items.Remove(PlaceHolderId);
        PlaceholderItem = null;

        DrawUtils.RestoreImGuiKeyboardNavigation();
    }

    internal void DrawPlaceholder(GraphUiContext context, ImDrawListPtr drawList)
    {
        if (PlaceholderItem == null)
            return;

        FrameStats.Current.OpenedPopUpName = "SymbolBrowser";

        _filter.UpdateIfNecessary(context.Selector);

        DrawSearchInput(context, drawList);

        // Might have been closed from input
        if (PlaceholderItem == null)
            return;

        var pMin = context.Canvas.TransformPosition(PlaceholderItem.PosOnCanvas);
        var pMax = context.Canvas.TransformPosition(PlaceholderItem.Area.Max);
        DrawResultsList(context, new ImRect(pMin, pMax));

        // Cancel by click outside
        var mousePos = ImGui.GetMousePos();
        var wasClickedOutside = ImGui.IsMouseClicked(ImGuiMouseButton.Left) &&
                                !(_placeholderAreaOnScreen.Contains(mousePos) || _resultAreaOnScreen.Contains(mousePos));
        if (wasClickedOutside)
        {
            Cancel(context);
        }

        if (_selectedSymbolUi != null)
        {
            if (_filter.PresetFilterString != string.Empty && (_filter.WasUpdated || _selectedItemChanged))
            {
                // TODO: Implement preset search
            }
        }

        if (_focusInputNextTime)
        {
            ImGui.SetKeyboardFocusHere();
            _focusInputNextTime = false;
            _selectedItemChanged = true;
        }
    }

    private sealed record OpSuggestionGroup(string Name, List<Guid> Items);

    private List<OpSuggestionGroup> GetOperatorSuggestions()
    {
        return
            [
                new("IMAGE", null),
                new("generate", [
                        Guid.Parse("f9fe78c5-43a6-48ae-8e8c-6cdbbc330dd1"), //RenderTarget
                        Guid.Parse("27b0e1af-cb2e-4603-83f9-5c9b042d87e6"), //blob
                        Guid.Parse("2c3d2c26-ac45-42e9-8f13-6ea338333568"), //LinearGradient
                        Guid.Parse("82ad8911-c930-4851-803d-3f24422445bc"), //RadialGradient
                        Guid.Parse("5a0b0485-7a55-4bf4-ae23-04f51d890334"), //FractalNoise
                        Guid.Parse("dc34c54b-f856-4fd2-a182-68fd75189d7d"), //ShardNoise
                        Guid.Parse("5cf7a1e2-7369-4e74-a7a9-b0eae61bdc21"), //WorleyNoise
                        Guid.Parse("0b3436db-e283-436e-ba85-2f3a1de76a9d"), //LoadImage
                        Guid.Parse("4f89b41b-1643-442b-bec8-9f9ef2173baa"), //Raster
                        Guid.Parse("d002bd90-5921-48b0-a940-a8d0c779f674"), //Rings
                        Guid.Parse("3ee3597d-dbf1-43a2-89d9-2f7b099112c7"), //RyojiPattern
                        Guid.Empty,
                        Guid.Parse("0b3436db-e283-436e-ba85-2f3a1de76a9d"), //LoadImage
                    ]),

                new("adjust", [
                        Guid.Parse("a29cf1c8-d9cd-4a5d-b06c-597cbeb5b33d"), //Crop
                        Guid.Parse("1192ae86-b174-4b58-9cc6-38afb666ce35"), //DirectionalBlur
                        Guid.Parse("2ab1bbef-8322-4638-8b1d-7e31aaa6a457"), //KeyColor
                        Guid.Parse("da93f7d1-ef91-4b4a-9708-2d9b1baa4c14"), //RemapColor
                        Guid.Parse("d9a71078-8296-4a07-b7de-250d4e2b95ac"), //Tint
                        Guid.Parse("32e18957-3812-4f64-8663-18454518d005"), //TransformImage
                    ]),

                new("effects", [
                        Guid.Parse("2a5475c8-9e16-409f-8c40-a3063e045d38"), //DetectEdges
                        Guid.Parse("d392d4af-4c78-4f4a-bc3f-4c54c8c73538"), //Glow
                        Guid.Parse("4cdc0f90-6ce9-4a03-9cd0-efeddee70567"), //Steps
                        Guid.Parse("1b149f1f-529c-4418-ac9d-3871f24a9e38"), //Displace
                        Guid.Parse("299e9912-2a6a-40ea-aa31-4c357bbec125"), //Dither
                        Guid.Parse("72e627e9-f570-4936-92b1-b12ed8d6004e"), //EdgeRepeat
                        Guid.Parse("06621b4b-43be-4ef9-80d0-f1b36fa4dbd1"), //MirrorRepeat
                        Guid.Parse("c68fbb84-2f56-4aed-97ab-3c2df0ec700b"), //MosaicTiling
                        Guid.Parse("6820b166-1782-43b9-bc5c-6b4f63b16f86"), //FakeLight
                        Guid.Parse("d75de240-28a1-48cc-9b8f-388272188023"), //AfterGlow
                        Guid.Parse("33424f7f-ea2d-4753-bbc3-8df00830c4b5"), //AdvancedFeedback
                        Guid.Parse("42e6319e-669c-4524-8d0d-9416a86afdb3"), //AsciiRender
                        Guid.Parse("8a203866-148d-4785-ae0e-61328b7646bb"), //ChromaticAbberation
                        Guid.Parse("ecf2c782-4461-4a94-8995-067425e3f84b"), //ChromaticDistortion
                    ]),
                new("POINTS", null),
                new("generate points", [
                        Guid.Parse("3352d3a1-ab04-4d0a-bb43-da69095b73fd"), //RadialPoints
                        Guid.Parse("3ee8f66d-68df-43c1-b0eb-407259bf7e86"), //GridPoints
                        Guid.Parse("4ae9e2f5-7cb3-40b0-a662-0662e8cb7c68"), //LinePoints
                        Guid.Parse("17188f49-1243-4511-a46c-1804cae10768"), //PointsOnMesh
                        Guid.Parse("722e79cc-47bc-42cc-8fce-2e06f36f8caa"), //PointsOnImage
                    ]),

                new("draw", [
                        Guid.Parse("ffd94e5a-bc98-4e70-84d8-cce831e6925f"), //DrawPoints
                        Guid.Parse("836f211f-b387-417c-8316-658e0dc6e117"), //DrawLines
                        Guid.Parse("42cb88bc-beb8-4d89-ac99-44b77be5f03e"), //DrawMeshAtPoints
                    ]),
                new("NUMBERS", null),
                new("math", [
                        Guid.Parse("5d7d61ae-0a41-4ffa-a51d-93bab665e7fe"), // Value
                        Guid.Parse("c160f925-0a66-4505-a569-cadd878dbb6f"), // Add
                        Guid.Parse("2f851b5b-b66d-40b0-9445-e733dc4b907d"), // Sum
                        Guid.Parse("17b60044-9125-4961-8a79-ca94697b3726"), // Multiply
                        Guid.Parse("926ab3fd-fbaf-4c4b-91bc-af277000dcb8"), // Vec2
                        Guid.Parse("94a5de3b-ee6a-43d3-8d21-7b8fe94b042b"), // Vec3
                        Guid.Parse("6ab63114-6477-4ab2-a071-a66a64a6d2b9"), // Sin
                        Guid.Parse("f0acd1a4-7a98-43ab-a807-6d1bd3e92169"), // Remap
                        Guid.Parse("5202d3f6-c970-4006-933d-3c60d6c202dc"), // Modulo
                    ]),
                new("animation", [
                        Guid.Parse("9cb4d49e-135b-400b-a035-2b02c5ea6a72"), // Time
                        Guid.Parse("436e93a8-03c0-4366-8d9a-2245e5bcaa6c"), // PerlinNoise
                        Guid.Parse("03477b9a-860e-4887-81c3-5fe51621122c"), // AudioReaction
                        Guid.Parse("ea7b8491-2f8e-4add-b0b1-fd068ccfed0d"), // AnimValue
                        Guid.Parse("95d586a2-ee14-4ff5-a5bb-40c497efde95"), // TriggerAnim
                        Guid.Parse("32325c5b-53f7-4414-b4dd-a436e45528b0"), // SetCommandTime
                        Guid.Parse("59a0458e-2f3a-4856-96cd-32936f783cc5"), // MidiIn
                    ]),
            ];
    }

    private void DrawSearchInput(GraphUiContext context, ImDrawListPtr drawList)
    {
        Debug.Assert(PlaceholderItem != null);

        var canvasScale = context.Canvas.Scale.X;
        var item = this.PlaceholderItem;
        var pMin = context.Canvas.TransformPosition(item.PosOnCanvas);
        var pMax = context.Canvas.TransformPosition(item.PosOnCanvas + item.Size);
        var pMinVisible = pMin;
        var pMaxVisible = pMax;

        _placeholderAreaOnScreen = ImRect.RectBetweenPoints(pMin, pMax);

        // Background and Outline
        drawList.AddRectFilled(pMinVisible + Vector2.One * canvasScale, pMaxVisible - Vector2.One,
                               UiColors.BackgroundFull, 6 * canvasScale);

        drawList.AddRect(pMinVisible, pMaxVisible, UiColors.ForegroundFull, 6 * canvasScale);

        if (_focusInputNextTime)
        {
            ImGui.SetKeyboardFocusHere();
            _focusInputNextTime = false;
            _selectedItemChanged = true;
        }

        var posInWindow = pMin - ImGui.GetWindowPos();

        // var odl = ImGui.GetForegroundDrawList();
        // odl.AddRect(ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize(), Color.Red);

        ImGui.SetCursorPos(posInWindow);
        //
        if (string.IsNullOrEmpty(_favoriteGroup))
        {
            var padding = new Vector2(7, 6);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, padding);
            ImGui.PushStyleColor(ImGuiCol.FrameBg, Color.Transparent.Rgba);
            ImGui.SetNextItemWidth(item.Size.X);
            ImGui.InputText("##symbolBrowserFilter",
                            ref _filter.SearchString,
                            20, ImGuiInputTextFlags.AutoSelectAll);

            ImGui.PopStyleColor();
        }
        else
        {
            var g = _opGroups.FirstOrDefault(gg => gg.Name == _favoriteGroup);
            if (g != null)
            {
                ImGui.SetCursorPos(ImGui.GetCursorPos() + new Vector2(5, 5));
                ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 10);
                if (ImGui.Button(g.Name + "  ×"))
                {
                    _favoriteGroup = string.Empty;
                }

                ImGui.PopStyleColor();
            }
        }

        if (ImGui.IsKeyPressed((ImGuiKey)Key.Return))
        {
            if (_selectedSymbolUi != null)
            {
                CreateInstance(context, _selectedSymbolUi.Symbol);
            }
        }

        if (_filter.WasUpdated)
        {
            _selectedSymbolUi = _filter.MatchingSymbolUis.Count > 0
                                    ? _filter.MatchingSymbolUis[0]
                                    : null;
            _selectedItemChanged = true;
        }

        var clickedOutside = false; //ImGui.IsMouseClicked(ImGuiMouseButton.Left) && ImGui.IsWindowHovered();
        var shouldCancelConnectionMaker = clickedOutside
                                          //|| ImGui.IsMouseClicked(ImGuiMouseButton.Right)
                                          || ImGui.IsKeyDown((ImGuiKey)Key.Esc);

        if (shouldCancelConnectionMaker)
        {
            Cancel(context);
        }

        ImGui.PopStyleVar();

        if (!ImGui.IsItemActive())
            return;

        if (ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            PlaceholderItem.PosOnCanvas += context.Canvas.InverseTransformDirection(ImGui.GetIO().MouseDelta);
        }

        // Label...
        // ImGui.PushFont(Fonts.FontBold);
        // var labelSize = ImGui.CalcTextSize(item.ReadableName);
        // ImGui.PopFont();
        // var downScale = MathF.Min(1, MagGraphItem.Width * 0.9f / labelSize.X);
        // var labelPos = pMin + new Vector2(8, 7) * CanvasScale + new Vector2(0, -1);
        // labelPos = new Vector2(MathF.Round(labelPos.X), MathF.Round(labelPos.Y));
        // drawList.AddText(Fonts.FontBold,
        //                  Fonts.FontBold.FontSize * downScale * CanvasScale,
        //                  labelPos,
        //                  UiColors.ForegroundFull,
        //                  "Search...");
    }

    private void DrawResultsList(GraphUiContext context, ImRect screenItemArea)
    {
        var size = new Vector2(150, 235) * T3Ui.UiScaleFactor;
        var windowSize = ImGui.GetWindowSize();
        var windowPos = ImGui.GetWindowPos();
        Vector2 resultPosOnScreen = new Vector2(screenItemArea.Min.X, screenItemArea.Max.Y + 3);
        if (Orientation == MagGraphItem.Directions.Horizontal)
        {
            var y = screenItemArea.GetCenter().Y - 0.3f * size.Y;
            resultPosOnScreen.Y = y.Clamp(windowPos.Y + 10, windowSize.Y + windowPos.Y - size.Y - 10);
            resultPosOnScreen.X = screenItemArea.Max.X.Clamp(windowPos.X + 10,
                                                             windowPos.X + windowSize.X - size.X - 10);
        }

        _resultAreaOnScreen = ImRect.RectWithSize(resultPosOnScreen, size);

        var resultPosOnWindow = resultPosOnScreen - ImGui.GetWindowPos();

        ImGui.SetCursorPos(resultPosOnWindow);

        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 6);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 14);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(6, 6));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(3, 6));

        ImGui.PushStyleColor(ImGuiCol.ChildBg, UiColors.BackgroundFull.Fade(0.8f).Rgba);
        ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, Color.Transparent.Rgba);

        if (ImGui.BeginChild(999, size, true,
                             ImGuiWindowFlags.None | ImGuiWindowFlags.AlwaysUseWindowPadding
                            ))
        {
            if (!string.IsNullOrEmpty(_filter.SearchString)
                || _filter.FilterInputType != null
                || _filter.FilterOutputType != null)
            {
                DrawSearchResultEntries(context);
            }
            else
            {
                PrintTypeFilter();
                var groups = GetOperatorSuggestions();
                if (string.IsNullOrEmpty(_favoriteGroup))
                {
                    foreach (var g in groups)
                    {
                        if (g.Items == null)
                        {
                            ImGui.PushStyleColor(ImGuiCol.Text, UiColors.TextMuted.Rgba);
                            ImGui.PushFont(Fonts.FontSmall);
                            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.One);
                            ImGui.Text(g.Name);
                            ImGui.PopStyleVar();
                            ImGui.PopFont();
                            ImGui.PopStyleColor();
                        }
                        else if (ImGui.Selectable(g.Name))
                        {
                            _favoriteGroup = g.Name;
                        }
                    }
                }
                else
                {
                    var g = groups.FirstOrDefault(gg => gg.Name == _favoriteGroup);
                    if (g != null)
                    {
                        foreach (var id in g.Items)
                        {
                            if (SymbolUiRegistry.TryGetSymbolUi(id, out var symbolUi))
                            {
                                DrawSymbolUiEntry(context, symbolUi);
                            }
                        }
                    }
                }
            }
        }

        ImGui.EndChild();

        ImGui.PopStyleColor(2);
        ImGui.PopStyleVar(4);
    }

    private void PrintTypeFilter()
    {
        if (_filter.FilterInputType == null && _filter.FilterOutputType == null)
            return;

        ImGui.PushFont(Fonts.FontSmall);

        var inputTypeName = _filter.FilterInputType != null
                                ? TypeNameRegistry.Entries[_filter.FilterInputType]
                                : string.Empty;

        var outputTypeName = _filter.FilterOutputType != null
                                 ? TypeNameRegistry.Entries[_filter.FilterOutputType]
                                 : string.Empty;

        var isMultiInput = _filter.OnlyMultiInputs ? "[..]" : "";

        var headerLabel = $"{inputTypeName}{isMultiInput}  -> {outputTypeName}";
        ImGui.TextDisabled(headerLabel);
        ImGui.PopFont();
    }

    private void DrawSearchResultEntries(GraphUiContext context)
    {
        if (ImGui.IsKeyReleased((ImGuiKey)Key.CursorDown))
        {
            UiListHelpers.AdvanceSelectedItem(_filter.MatchingSymbolUis, ref _selectedSymbolUi, 1);
            _selectedItemChanged = true;
        }
        else if (ImGui.IsKeyReleased((ImGuiKey)Key.CursorUp))
        {
            UiListHelpers.AdvanceSelectedItem(_filter.MatchingSymbolUis, ref _selectedSymbolUi, -1);
            _selectedItemChanged = true;
        }

        var gotAMatch = _filter.MatchingSymbolUis.Count > 0 && !_filter.MatchingSymbolUis.Contains(_selectedSymbolUi);
        if (gotAMatch)
            _selectedSymbolUi = _filter.MatchingSymbolUis[0];

        if (_selectedSymbolUi == null && EditorSymbolPackage.AllSymbolUis.Any())
            _selectedSymbolUi = EditorSymbolPackage.AllSymbolUis.First();

        foreach (var symbolUi in _filter.MatchingSymbolUis)
        {
            DrawSymbolUiEntry(context, symbolUi);
        }
    }

    private void DrawSymbolUiEntry(GraphUiContext context, SymbolUi symbolUi)
    {
        var symbolHash = symbolUi.Symbol.Id.GetHashCode();
        ImGui.PushID(symbolHash);
        {
            var symbolNamespace = symbolUi.Symbol.Namespace;
            var isRelevantNamespace = IsRelevantNamespace(context, symbolNamespace);

            var color = symbolUi.Symbol.OutputDefinitions.Count > 0
                            ? TypeUiRegistry.GetPropertiesForType(symbolUi.Symbol.OutputDefinitions[0]?.ValueType).Color
                            : UiColors.Gray;

            if (!isRelevantNamespace)
            {
                color = color.Fade(0.4f);
            }

            ImGui.PushStyleColor(ImGuiCol.Header, ColorVariations.OperatorBackground.Apply(color).Rgba);

            var hoverColor = ColorVariations.OperatorBackgroundHover.Apply(color).Rgba;
            hoverColor.W = 0.3f;
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, hoverColor);
            ImGui.PushStyleColor(ImGuiCol.Text, ColorVariations.OperatorLabel.Apply(color).Rgba);

            var isSelected = symbolUi == _selectedSymbolUi;

            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 14);
            _selectedItemChanged |= ImGui.Selectable($"##Selectable{symbolHash.ToString()}", isSelected);
            ImGui.PopStyleVar();

            var isHovered = ImGui.IsItemHovered();
            if (isHovered)
            {
                ImGui.SetNextWindowSize(new Vector2(300, 0));
                ImGui.BeginTooltip();
                OperatorHelp.DrawHelpSummary(symbolUi);
                ImGui.EndTooltip();
            }

            if (ImGui.IsItemActivated())
            {
                CreateInstance(context, symbolUi.Symbol);
            }

            ImGui.SameLine();

            ImGui.TextUnformatted(symbolUi.Symbol.Name);
            ImGui.PopStyleColor(3);
        }
        ImGui.PopID();
    }

    private static bool IsRelevantNamespace(GraphUiContext context, string symbolNamespace)
    {
        var projectNamespace = "user." + context.CompositionOp.Symbol.SymbolPackage.AssemblyInformation.Name + ".";
        var compositionNameSpace = context.CompositionOp.Symbol.Namespace;

        var isRelevantNamespace = symbolNamespace.StartsWith("Lib.")
                                  || symbolNamespace.StartsWith("Types.")
                                  || symbolNamespace.StartsWith("Examples.lib.")
                                  || symbolNamespace.StartsWith(projectNamespace)
                                  || symbolNamespace.StartsWith(compositionNameSpace);
        return isRelevantNamespace;
    }

    private void CreateInstance(GraphUiContext context, Symbol symbol)
    {
        if (context.MacroCommand == null || PlaceholderItem == null)
        {
            Log.Warning("Macro command missing for insertion");
            return;
        }

        var parentSymbol = context.CompositionOp.Symbol;
        var parentSymbolUi = parentSymbol.GetSymbolUi();

        var addSymbolChildCommand = new AddSymbolChildCommand(parentSymbol, symbol.Id) { PosOnCanvas = PlaceholderItem.PosOnCanvas };
        context.MacroCommand.AddAndExecCommand(addSymbolChildCommand);

        // Select new node
        if (!parentSymbolUi.ChildUis.TryGetValue(addSymbolChildCommand.AddedChildId, out var newChildUi))
        {
            Log.Warning($"Unable to create new operator - failed to retrieve new child ui \"{addSymbolChildCommand.AddedChildId}\" " +
                        $"from parent symbol ui {parentSymbolUi.Symbol}");
            return;
        }

        var newSymbolChild = newChildUi.SymbolChild;
        var newInstance = context.CompositionOp.Children[newChildUi.Id];
        context.Selector.SetSelection(newChildUi, newInstance);

        // Connect to focus node...
        if (_focusedItem != null)
        {
            if (_focusedItem.OutputLines.Length > 0
                && newInstance.Inputs.Count > 0
               )
            {
                if (_focusedItem.OutputLines[0].ConnectionsOut.Count > 0)
                {
                    var newItemOutput = newInstance.Outputs[0];

                    // Reroute original connections...
                    foreach (var mc in _focusedItem.OutputLines[0].ConnectionsOut)
                    {
                        if (context.ActiveOutputDirection == MagGraphItem.Directions.Vertical &&
                            (mc.Style == MagGraphConnection.ConnectionStyles.RightToLeft
                             || mc.Style == MagGraphConnection.ConnectionStyles.MainOutToInputSnappedHorizontal
                             || mc.Style == MagGraphConnection.ConnectionStyles.MainOutToMainInSnappedHorizontal))
                            continue;
                        
                        context.MacroCommand
                               .AddAndExecCommand(new DeleteConnectionCommand(context.CompositionOp.Symbol,
                                                                              mc.AsSymbolConnection(), 0));

                        context.MacroCommand
                               .AddAndExecCommand(new AddConnectionCommand(context.CompositionOp.Symbol,
                                                                           new Symbol.Connection(newInstance.SymbolChildId,
                                                                                                 newItemOutput.Id,
                                                                                                 mc.TargetItem.Id,
                                                                                                 mc.TargetInput.Id
                                                                                                ),
                                                                           0));
                    }
                }

                // Create new Connection
                context.MacroCommand
                       .AddAndExecCommand(new AddConnectionCommand(context.CompositionOp.Symbol,
                                                                   new Symbol.Connection(_focusedItem.Id,
                                                                                         _focusedItem.OutputLines[0].Output.Id,
                                                                                         newInstance.SymbolChildId,
                                                                                         newInstance.Inputs[0].Id
                                                                                        ),
                                                                   0));
            }

            // push snapped ops further down if new op exceed default height 
            {
                var newItem = new MagGraphItem
                                  {
                                      Variant = MagGraphItem.Variants.Operator,
                                      Selectable = newChildUi,
                                      Size = default,
                                      SymbolUi = symbol.GetSymbolUi(),
                                      SymbolChild = null,
                                      Instance = newInstance,
                                  };

                List<MagGraphItem.InputLine> inputLines = [];
                List<MagGraphItem.OutputLine> outputLines = [];
                MagGraphLayout.CollectVisibleLines(newItem, inputLines, outputLines);
                var newHeight = inputLines.Count + outputLines.Count - 1;
                if (newHeight > 1)
                {
                    //var snappedItems = MagItemMovement.CollectSnappedItems(_focusedItem);

                    MagItemMovement
                       .MoveSnappedItemsVertically(context,
                                                   _snappedItems,
                                                   _focusedItem.PosOnCanvas.Y + _focusedItem.Size.Y - MagGraphItem.GridSize.Y / 2,
                                                   MagGraphItem.GridSize.Y * (newHeight - 1));
                }
            }
        }
        else if (context.TryGetActiveOutputLine(out var outputLine))
        {
            var primaryInput = newInstance.Inputs.FirstOrDefault();
            if (primaryInput != null && primaryInput.ValueType == context.DraggedPrimaryOutputType)
            {
                var connectionToAdd = new Symbol.Connection(context.ActiveSourceItem!.Id,
                                                            outputLine.Id,
                                                            newInstance.SymbolChildId,
                                                            primaryInput.Id);
                
                context.MacroCommand
                       .AddAndExecCommand(new AddConnectionCommand(context.CompositionOp.Symbol,
                                                                   connectionToAdd,
                                                                   0));
            }
        }
        // Wire connect temp connections
        else
        {
            foreach(var h in context.ConnectionHovering.ConnectionHoversWhenClicked)
            {
                // Remove current connections
                context.MacroCommand
                       .AddAndExecCommand
                            (new DeleteConnectionCommand(context.CompositionOp.Symbol,
                                                         h.Connection.AsSymbolConnection(),
                                                         h.Connection.MultiInputIndex
                                                        ));
    
            
                var tempConnectionOut = new MagGraphConnection
                                            {
                                                Style = MagGraphConnection.ConnectionStyles.Unknown,
                                                SourcePos = default,
                                                SourceOutput = null,
                                                SourceItem = null,
                                                TargetPos = default,
                                                TargetItem = h.Connection.TargetItem,
                                                InputLineIndex = h.Connection.InputLineIndex,
                                                MultiInputIndex = h.Connection.MultiInputIndex,
                                                OutputLineIndex = 0,
                                                VisibleOutputIndex = 0,
                                                ConnectionHash = 0,
                                                IsTemporary = true,
                                            };
            
                context.TempConnections.Add(tempConnectionOut);
            }
            
            var primaryInput = newInstance.Inputs.FirstOrDefault();
            var primaryOutput = newInstance.Outputs.FirstOrDefault();
            if (primaryInput != null && primaryOutput != null)
            {
                foreach (var tc in context.TempConnections)
                {
                    if (!tc.IsTemporary)
                        continue;
                    
                    if (tc.SourceItem != null && tc.TargetItem == null)
                    {
                        var connectionToAdd = new Symbol.Connection(tc.SourceItem.Id,
                                                                    tc.SourceOutput.Id,
                                                                    newInstance.SymbolChildId,
                                                                    primaryInput.Id);
                        context.MacroCommand
                               .AddAndExecCommand(new AddConnectionCommand(context.CompositionOp.Symbol,
                                                                           connectionToAdd,
                                                                           tc.MultiInputIndex));
                    }
                    else if(tc.SourceItem == null && tc.TargetItem != null)
                    {
                        var connectionToAdd = new Symbol.Connection(newInstance.SymbolChildId,
                                                                    primaryOutput.Id,
                                                                    tc.TargetItem.Id,
                                                                    tc.TargetInput.Id);
                        context.MacroCommand
                               .AddAndExecCommand(new AddConnectionCommand(context.CompositionOp.Symbol,
                                                                           connectionToAdd,
                                                                           tc.MultiInputIndex));
                    }
                }
            }
        }

        // TODO: add preset selection...

        ParameterPopUp.NodeIdRequestedForParameterWindowActivation = newSymbolChild.Id;
        context.Layout.FlagAsChanged();

        Close(context);
    }

    private string _favoriteGroup = string.Empty;
    private static List<OpSuggestionGroup> _opGroups = [];
    private readonly SymbolFilter _filter = new();
    private bool _focusInputNextTime = true;
    private bool _selectedItemChanged;

    private ImRect _placeholderAreaOnScreen;
    private ImRect _resultAreaOnScreen;

    private MagGraphItem.Directions Orientation = MagGraphItem.Directions.Horizontal;

    private SymbolUi _selectedSymbolUi;
    private static readonly int _uiId = "DraftNode".GetHashCode();
    private HashSet<MagGraphItem> _snappedItems = [];
}