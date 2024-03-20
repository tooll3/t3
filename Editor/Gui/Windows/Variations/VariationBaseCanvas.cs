using ImGuiNET;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.Interaction.Variations.Model;
using T3.Editor.Gui.OutputUi;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.UiHelpers.DelaunayVoronoi;
using T3.Editor.Gui.Windows.Exploration;
using T3.Editor.Gui.Windows.Output;
using T3.Editor.UiModel;
using Vector2 = System.Numerics.Vector2;

namespace T3.Editor.Gui.Windows.Variations
{
    internal abstract class VariationBaseCanvas : ScalableCanvas, ISelectionContainer
    {
        public abstract Variation CreateVariation();
        public abstract void DrawToolbarFunctions();
        public abstract string GetTitle();

        protected abstract Instance InstanceForBlendOperations { get; }
        private protected abstract SymbolVariationPool PoolForBlendOperations { get; }
        protected abstract void DrawAdditionalContextMenuContent();

        public void Draw(ImDrawListPtr drawList, bool hideHeader = false)
        {
            UpdateCanvas(out _);

            // Complete deferred actions
            if (!T3Ui.IsCurrentlySaving && KeyboardBinding.Triggered(UserActions.DeleteSelection))
                DeleteSelectedElements();

            UpdateThumbnailRendering(out var pinnedOutputChanged);

            // Get instance for variations
            var instanceForBlending = InstanceForBlendOperations;
            if (instanceForBlending != _instanceForBlending || pinnedOutputChanged)
            {
                _instanceForBlending = instanceForBlending;
                RefreshView();
            }

            //UpdateCanvas();
            HandleFenceSelection();

            // Blending...
            HandleBlendingInteraction();

            _thumbnailCanvasRendering.InitializeCanvasTexture(VariationThumbnail.ThumbnailSize);

            if (!hideHeader)
            {
                ImGui.PushFont(Fonts.FontLarge);
                ImGui.SetCursorPos(new Vector2(10, 35));
                ImGui.PushStyleColor(ImGuiCol.Text, UiColors.Gray.Rgba);
                ImGui.TextUnformatted(GetTitle());
                ImGui.PopStyleColor();
                ImGui.PopFont();
            }

            // Draw thumbnails...
            var modified = false;
            for (var index = 0; index < PoolForBlendOperations.AllVariations.Count; index++)
            {
                modified |= VariationThumbnail.Draw(this,
                                                    PoolForBlendOperations.AllVariations[index],
                                                    drawList,
                                                    _thumbnailCanvasRendering.CanvasTextureSrv,
                                                    GetUvRectForIndex(index));
            }

            DrawBlendingOverlay(drawList);

            if (modified)
                PoolForBlendOperations.SaveVariationsToFile();

            DrawContextMenu();
        }

        private bool _rerenderManuallyRequested = false;

        /// <summary>
        /// Updates keeps rendering thumbnails until all are processed.
        /// </summary>
        private void UpdateThumbnailRendering(out bool pinnedOutputChanged)
        {
            pinnedOutputChanged = false;

            if (!UserSettings.Config.VariationLiveThumbnails && !_rerenderManuallyRequested)
                return;

            // Render variations to pinned output
            if (OutputWindow.OutputWindowInstances.FirstOrDefault(window => window.Config.Visible) is not OutputWindow outputWindow)
                return;

            var renderInstance = outputWindow.ShownInstance;
            if (renderInstance is not { Outputs: { Count: > 0 } } || renderInstance.Outputs[0] is not Slot<Texture2D> textureSlot)
                return;

            _thumbnailCanvasRendering.InitializeCanvasTexture(VariationThumbnail.ThumbnailSize);

            if (renderInstance != _previousRenderInstance)
            {
                pinnedOutputChanged = true;
                _previousRenderInstance = renderInstance;
            }

            var symbolUi = renderInstance.GetSymbolUi();
            if (!symbolUi.OutputUis.ContainsKey(textureSlot.Id))
                return;

            var outputUi = symbolUi.OutputUis[textureSlot.Id];
            UpdateNextVariationThumbnail(outputUi, textureSlot);
        }

        private void DrawBlendingOverlay(ImDrawListPtr drawList)
        {
            if (IsBlendingActive)
            {
                var mousePos = ImGui.GetMousePos();
                if (_blendPoints.Count == 1)
                {
                    PoolForBlendOperations.BeginWeightedBlend(_instanceForBlending, _blendVariations, _blendWeights);

                    if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                    {
                        PoolForBlendOperations.ApplyCurrentBlend();
                    }
                }
                else if (_blendPoints.Count == 2)
                {
                    foreach (var p in _blendPoints)
                    {
                        drawList.AddCircleFilled(p, 5, UiColors.BackgroundFull.Fade(0.5f));
                        drawList.AddCircleFilled(p, 3, UiColors.ForegroundFull);
                    }

                    drawList.AddLine(_blendPoints[0], _blendPoints[1], UiColors.ForegroundFull, 2);
                    var blendPosition = _blendPoints[0] * _blendWeights[0] + _blendPoints[1] * _blendWeights[1];

                    drawList.AddCircleFilled(blendPosition, 5, UiColors.ForegroundFull);

                    PoolForBlendOperations.BeginWeightedBlend(_instanceForBlending, _blendVariations, _blendWeights);

                    if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                    {
                        PoolForBlendOperations.ApplyCurrentBlend();
                    }
                }
                else if (_blendPoints.Count == 3)
                {
                    drawList.AddTriangleFilled(_blendPoints[0], _blendPoints[1], _blendPoints[2], UiColors.BackgroundFull.Fade(0.3f));
                    foreach (var p in _blendPoints)
                    {
                        drawList.AddCircleFilled(p, 5, UiColors.BackgroundFull.Fade(0.5f));
                        drawList.AddLine(mousePos, p, UiColors.ForegroundFull, 2);
                        drawList.AddCircleFilled(p, 3, UiColors.ForegroundFull);
                    }

                    drawList.AddCircleFilled(mousePos, 5, UiColors.ForegroundFull);
                    PoolForBlendOperations.BeginWeightedBlend(_instanceForBlending, _blendVariations, _blendWeights);

                    if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                    {
                        PoolForBlendOperations.ApplyCurrentBlend();
                    }
                }
            }
        }

        private Vector2 HandleBlendingInteraction()
        {
            IsBlendingActive = (ImGui.IsWindowHovered() || ImGui.IsWindowFocused()) && ImGui.GetIO().KeyAlt;

            var mousePos = ImGui.GetMousePos();
            _blendPoints.Clear();
            _blendWeights.Clear();
            _blendVariations.Clear();

            if (IsBlendingActive)
            {
                foreach (var s in Selection.SelectedElements)
                {
                    _blendPoints.Add(GetNodeCenterOnScreen(s));
                    _blendVariations.Add(s as Variation);
                }

                if (Selection.SelectedElements.Count == 1)
                {
                    var posOnScreen = TransformPosition(_blendVariations[0].PosOnCanvas);
                    var sizeOnScreen = TransformDirection(_blendVariations[0].Size);
                    var a = (mousePos.X - posOnScreen.X) / sizeOnScreen.X;

                    _blendWeights.Add(a);
                }
                else if (Selection.SelectedElements.Count == 2)
                {
                    if (_blendPoints[0] == _blendPoints[1])
                    {
                        _blendWeights.Add(0.5f);
                        _blendWeights.Add(0.5f);
                    }
                    else
                    {
                        var v1 = _blendPoints[1] - _blendPoints[0];
                        var v2 = mousePos - _blendPoints[0];
                        var lengthV1 = v1.Length();

                        var a = Vector2.Dot(v1 / lengthV1, v2 / lengthV1);
                        _blendWeights.Add(1 - a);
                        _blendWeights.Add(a);
                    }
                }
                else if (Selection.SelectedElements.Count == 3)
                {
                    Barycentric(mousePos, _blendPoints[0], _blendPoints[1], _blendPoints[2], out var u, out var v, out var w);
                    _blendWeights.Add(u);
                    _blendWeights.Add(v);
                    _blendWeights.Add(w);
                }
                else
                {
                    var points = new List<Point>();

                    Vector2 minPos = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                    Vector2 maxPos = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

                    foreach (var v in PoolForBlendOperations.AllVariations)
                    {
                        var vec2 = GetNodeCenterOnScreen(v);
                        minPos = Vector2.Min(vec2, minPos);
                        maxPos = Vector2.Max(vec2, maxPos);
                        points.Add(new Point(vec2.X, vec2.Y));
                    }

                    minPos -= Vector2.One * 100;
                    maxPos += Vector2.One * 100;

                    var triangulator = new DelaunayTriangulator();
                    var borderPoints = triangulator.SetBorder(new Point(minPos.X, minPos.Y), new Point(maxPos.X, maxPos.Y));
                    points.AddRange(borderPoints);

                    var triangles = triangulator.BowyerWatson(points);

                    foreach (var t in triangles)
                    {
                        var p0 = t.Vertices[0].ToVec2();
                        var p1 = t.Vertices[1].ToVec2();
                        var p2 = t.Vertices[2].ToVec2();
                        Barycentric(mousePos,
                                    p0,
                                    p1,
                                    p2,
                                    out var u,
                                    out var v,
                                    out var w);

                        var insideTriangle = u >= 0 && u <= 1 && v >= 0 && v <= 1 && w >= 0 && w <= 1;
                        if (!insideTriangle)
                            continue;

                        _blendPoints.Clear();
                        _blendWeights.Clear();
                        _blendVariations.Clear();

                        var weights = new[] { u, v, w };

                        for (var vertexIndex = 0; vertexIndex < t.Vertices.Length; vertexIndex++)
                        {
                            var vertex = t.Vertices[vertexIndex];
                            var variationIndex = points.IndexOf(vertex);
                            if (variationIndex < PoolForBlendOperations.AllVariations.Count)
                            {
                                _blendVariations.Add(PoolForBlendOperations.AllVariations[variationIndex]);
                                _blendWeights.Add(weights[vertexIndex]);
                                _blendPoints.Add(vertex.ToVec2());
                            }
                        }

                        if (_blendWeights.Count == 2)
                        {
                            var sum = _blendWeights[0] + _blendWeights[1];
                            _blendWeights[0] /= sum;
                            _blendWeights[1] /= sum;
                        }
                        else if (_blendWeights.Count == 1)
                        {
                            _blendWeights.Clear();
                            _blendPoints.Clear();
                            _blendVariations.Clear();
                        }

                        break;
                    }
                }
            }

            return mousePos;
        }

        public bool TryGetBlendWeight(Variation v, out float weight)
        {
            weight = 0;
            if (_blendWeights.Count == 0)
                return false;

            var index = _blendVariations.IndexOf(v);
            if (index == -1)
            {
                return false;
            }

            weight = _blendWeights[index];
            return true;
        }

        private Vector2 GetNodeCenterOnScreen(ISelectableCanvasObject node)
        {
            var min = TransformPosition(node.PosOnCanvas);
            var max = TransformPosition(node.PosOnCanvas + node.Size);
            return (min + max) * 0.5f;
        }

        private void DrawContextMenu()
        {
            if (FrameStats.Current.OpenedPopUpName == string.Empty)
            {
                CustomComponents.DrawContextMenuForScrollCanvas(() =>
                                                                {
                                                                    var oneOrMoreSelected = Selection.SelectedElements.Count > 0;
                                                                    var oneSelected = Selection.SelectedElements.Count == 1;

                                                                    if (ImGui.MenuItem("Delete selected",
                                                                                       "Del", // We should use the correct assigned short cut, but "Del or Backspace" is too long for layout
                                                                                       false,
                                                                                       oneOrMoreSelected))
                                                                    {
                                                                        DeleteSelectedElements();
                                                                    }

                                                                    if (ImGui.MenuItem("Rename",
                                                                                       "",
                                                                                       false,
                                                                                       oneSelected))
                                                                    {
                                                                        VariationThumbnail.VariationForRenaming = Selection.SelectedElements[0] as Variation;
                                                                    }

                                                                    if (ImGui.MenuItem("Update thumbnails",
                                                                                       ""))
                                                                    {
                                                                        _rerenderManuallyRequested = true;
                                                                        TriggerThumbnailUpdate();
                                                                    }

                                                                    ImGui.Separator();
                                                                    ImGui.MenuItem("Live Render Previews", "", ref UserSettings.Config.VariationLiveThumbnails,
                                                                                   true);
                                                                    ImGui.MenuItem("Preview on Hover", "", ref UserSettings.Config.VariationHoverPreview, true);

                                                                    DrawAdditionalContextMenuContent();
                                                                }, ref _contextMenuIsOpen);
            }
        }

        private bool _contextMenuIsOpen;

        public void StartHover(Variation variation)
        {
            PoolForBlendOperations.BeginHover(_instanceForBlending, variation);
        }

        public void Apply(Variation variation)
        {
            PoolForBlendOperations.StopHover();
            PoolForBlendOperations.Apply(_instanceForBlending, variation);
        }

        public void StartBlendTo(Variation variation, float blend)
        {
            if (variation.IsPreset)
            {
                PoolForBlendOperations.BeginBlendToPresent(_instanceForBlending, variation, blend);
            }
        }

        public void StopHover()
        {
            PoolForBlendOperations.StopHover();
        }

        protected void TriggerThumbnailUpdate()
        {
            _thumbnailCanvasRendering.ClearTexture();
            _renderThumbnailIndex = 0;
            _allThumbnailsRendered = false;
        }

        protected void ResetView()
        {
            var pool = PoolForBlendOperations;
            if (pool == null)
                return;

            if (TryToGetBoundingBox(pool.AllVariations, 40, out var area))
            {
                area.Min.Y -= 200;
                FitAreaOnCanvas(area);
            }
        }

        private void HandleFenceSelection()
        {
            _fenceState = SelectionFence.UpdateAndDraw(_fenceState);
            switch (_fenceState)
            {
                case SelectionFence.States.PressedButNotMoved:
                    if (SelectionFence.SelectMode == SelectionFence.SelectModes.Replace)
                        Selection.Clear();
                    break;

                case SelectionFence.States.Updated:
                    HandleSelectionFenceUpdate(SelectionFence.BoundsInScreen);
                    break;

                case SelectionFence.States.CompletedAsClick:
                    Selection.Clear();
                    break;
            }
        }

        private void HandleSelectionFenceUpdate(ImRect boundsInScreen)
        {
            var boundsInCanvas = InverseTransformRect(boundsInScreen);
            var elementsToSelect = (from child in PoolForBlendOperations.AllVariations
                                    let rect = new ImRect(child.PosOnCanvas, child.PosOnCanvas + child.Size)
                                    where rect.Overlaps(boundsInCanvas)
                                    select child).ToList();

            Selection.Clear();
            foreach (var element in elementsToSelect)
            {
                Selection.AddSelection(element);
            }
        }

        private void DeleteSelectedElements()
        {
            if (Selection.SelectedElements.Count <= 0)
                return;

            var list = new List<Variation>();
            foreach (var e in Selection.SelectedElements)
            {
                if (e is Variation v)
                {
                    list.Add(v);
                }
            }

            VariationsWindow.DeleteVariationsFromPool(PoolForBlendOperations, list);
            PoolForBlendOperations.SaveVariationsToFile();
        }

        #region thumbnail rendering
        private void UpdateNextVariationThumbnail(IOutputUi outputUi, Slot<Texture2D> textureSlot)
        {
            if (_allThumbnailsRendered)
                return;

            _thumbnailCanvasRendering.InitializeCanvasTexture(VariationThumbnail.ThumbnailSize);

            if (PoolForBlendOperations.AllVariations.Count == 0)
            {
                _allThumbnailsRendered = true;
                _rerenderManuallyRequested = false;
                return;
            }

            if (_renderThumbnailIndex >= PoolForBlendOperations.AllVariations.Count)
            {
                _allThumbnailsRendered = true;
                _rerenderManuallyRequested = false;
                return;
            }

            var variation = PoolForBlendOperations.AllVariations[_renderThumbnailIndex];
            RenderThumbnail(variation, _renderThumbnailIndex, outputUi, textureSlot);
            _renderThumbnailIndex++;
        }

        private void RenderThumbnail(Variation variation, int atlasIndex, IOutputUi outputUi, Slot<Texture2D> textureSlot)
        {
            // Set variation values
            PoolForBlendOperations.BeginHover(InstanceForBlendOperations, variation);

            // Render variation
            _thumbnailCanvasRendering.EvaluationContext.Reset();
            _thumbnailCanvasRendering.EvaluationContext.LocalTime = 13.4f;

            // NOTE: This is horrible hack to prevent _imageCanvas from being rendered by ImGui
            // DrawValue will use the current ImageOutputCanvas for rendering
            _imageCanvas.SetAsCurrent();
            ImGui.PushClipRect(new Vector2(0, 0), new Vector2(1, 1), true);
            outputUi.DrawValue(textureSlot, _thumbnailCanvasRendering.EvaluationContext);
            ImGui.PopClipRect();
            _imageCanvas.Deactivate();

            var rect = GetPixelRectForIndex(atlasIndex);

            if (textureSlot.Value != null)
            {
                _thumbnailCanvasRendering.CopyToCanvasTexture(textureSlot, rect);
            }

            PoolForBlendOperations.StopHover();
        }

        private ImRect GetPixelRectForIndex(int thumbnailIndex)
        {
            var columns = (int)(_thumbnailCanvasRendering.GetCanvasTextureSize().X / VariationThumbnail.ThumbnailSize.X);
            if (columns == 0)
            {
                return ImRect.RectWithSize(Vector2.Zero, VariationThumbnail.ThumbnailSize);
            }

            var rowIndex = thumbnailIndex / columns;
            var columnIndex = thumbnailIndex % columns;
            var posInCanvasTexture = new Vector2(columnIndex, rowIndex) * VariationThumbnail.ThumbnailSize;
            var rect = ImRect.RectWithSize(posInCanvasTexture, VariationThumbnail.ThumbnailSize);
            return rect;
        }

        private ImRect GetUvRectForIndex(int thumbnailIndex)
        {
            var r = GetPixelRectForIndex(thumbnailIndex);
            return new ImRect(r.Min / _thumbnailCanvasRendering.GetCanvasTextureSize(),
                              r.Max / _thumbnailCanvasRendering.GetCanvasTextureSize());
        }
        #endregion

        #region layout and view
        public void RefreshView()
        {
            TriggerThumbnailUpdate();
            Selection.Clear();
            ResetView();
        }

        private static bool TryToGetBoundingBox(IEnumerable<Variation> variations, float extend, out ImRect area)
        {
            area = new ImRect();
            if (variations == null)
                return false;

            var foundOne = false;

            foreach (var v in variations)
            {
                if (!foundOne)
                {
                    area = ImRect.RectWithSize(v.PosOnCanvas, v.Size);
                    foundOne = true;
                }
                else
                {
                    area.Add(ImRect.RectWithSize(v.PosOnCanvas, v.Size));
                }
            }

            if (!foundOne)
                return false;

            area.Expand(Vector2.One * extend);
            return true;
        }

        /// <summary>
        /// This uses a primitive algorithm: Look for the bottom edge of a all element bounding box
        /// Then step through possible positions and check if a position would intersect with an existing element.
        /// Wrap columns to enforce some kind of grid.  
        /// </summary>
        internal static Vector2 FindFreePositionForNewThumbnail(IEnumerable<Variation> variations)
        {
            if (!TryToGetBoundingBox(variations, 0, out var area))
            {
                return Vector2.Zero;
            }

            // var areaOnScreen = TransformRect(area); 
            // ImGui.GetForegroundDrawList().AddRect(areaOnScreen.Min, areaOnScreen.Max, Color.Blue);

            const int columns = 3;
            var columnIndex = 0;

            var stepWidth = VariationThumbnail.ThumbnailSize.X + VariationThumbnail.SnapPadding.X;
            var stepHeight = VariationThumbnail.ThumbnailSize.Y + VariationThumbnail.SnapPadding.Y;

            var pos = new Vector2(area.Min.X,
                                  area.Max.Y - VariationThumbnail.ThumbnailSize.Y);
            var rowStartPos = pos;

            while (true)
            {
                var intersects = false;
                var targetArea = new ImRect(pos, pos + VariationThumbnail.ThumbnailSize);

                // var targetAreaOnScreen = TransformRect(targetArea);
                // ImGui.GetForegroundDrawList().AddRect(targetAreaOnScreen.Min, targetAreaOnScreen.Max, Color.Orange);

                foreach (var v in variations)
                {
                    if (!targetArea.Overlaps(ImRect.RectWithSize(v.PosOnCanvas, v.Size)))
                        continue;

                    intersects = true;
                    break;
                }

                if (!intersects)
                    return pos;

                columnIndex++;
                if (columnIndex == columns)
                {
                    columnIndex = 0;
                    rowStartPos += new Vector2(0, stepHeight);
                    pos = rowStartPos;
                }
                else
                {
                    pos += new Vector2(stepWidth, 0);
                }
            }
        }
        #endregion

        // Compute barycentric coordinates (u, v, w) for
        // point p with respect to triangle (a, b, c)
        private static void Barycentric(Vector2 p, Vector2 a, Vector2 b, Vector2 c, out float u, out float v, out float w)
        {
            Vector2 v0 = b - a, v1 = c - a, v2 = p - a;
            var den = v0.X * v1.Y - v1.X * v0.Y;
            v = (v2.X * v1.Y - v1.X * v2.Y) / den;
            w = (v0.X * v2.Y - v2.X * v0.Y) / den;
            u = 1.0f - v - w;
        }

        /// <summary>
        /// Implement selectionContainer
        /// </summary>
        public IEnumerable<ISelectableCanvasObject> GetSelectables()
        {
            return PoolForBlendOperations?.AllVariations;
        }

        public bool IsBlendingActive { get; private set; }
        private readonly List<float> _blendWeights = new(3);
        private readonly List<Vector2> _blendPoints = new(3);
        private readonly List<Variation> _blendVariations = new(3);

        private Instance _instanceForBlending;

        private int _renderThumbnailIndex;
        private bool _allThumbnailsRendered;
        private readonly ImageOutputCanvas _imageCanvas = new();
        private readonly ThumbnailCanvasRendering _thumbnailCanvasRendering = new();
        private SelectionFence.States _fenceState;
        internal readonly CanvasElementSelection Selection = new();
        private Instance _previousRenderInstance;
    }
}