using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Gui.Interaction;
using T3.Gui.Interaction.Variations;
using T3.Gui.Interaction.Variations.Model;
using T3.Gui.OutputUi;
using T3.Gui.Selection;
using T3.Gui.UiHelpers;
using T3.Gui.Windows.Exploration;
using T3.Gui.Windows.Output;
using UiHelpers;
using Vector2 = System.Numerics.Vector2;

namespace T3.Gui.Windows.Variations
{
    public class VariationCanvas : ScalableCanvas, ISelectionContainer
    {
        public VariationCanvas(VariationsWindow variationsWindow)
        {
            _variationsWindow = variationsWindow;
        }

        public void Draw(ImDrawListPtr drawList, SymbolVariationPool activePoolForPresets)
        {
            // Complete deferred actions
            if (!T3Ui.IsCurrentlySaving && KeyboardBinding.Triggered(UserActions.DeleteSelection))
                DeleteSelectedElements();

            var viewNeedsRefresh = false;

            // Render variations to pinned output
            if (OutputWindow.OutputWindowInstances.FirstOrDefault(window => window.Config.Visible) is OutputWindow outputWindow)
            {
                var renderInstance = outputWindow.ShownInstance;
                if (renderInstance is { Outputs: { Count: > 0 } }
                    && renderInstance.Outputs[0] is Slot<Texture2D> textureSlot)
                {
                    _thumbnailCanvasRendering.InitializeCanvasTexture(VariationThumbnail.ThumbnailSize);

                    if (renderInstance != _lastRenderInstance)
                    {
                        viewNeedsRefresh = true;
                        _lastRenderInstance = renderInstance;
                        
                    }
                    var symbolUi = SymbolUiRegistry.Entries[renderInstance.Symbol.Id];
                    if (symbolUi.OutputUis.ContainsKey(textureSlot.Id))
                    {
                        var outputUi = symbolUi.OutputUis[textureSlot.Id];
                        UpdateNextVariationThumbnail(outputUi, textureSlot);
                    }
                }
            }
            
            // Get instance for variations
            var instance = VariationHandling.ActiveInstanceForPresets;
            var instanceChanged =instance != _instance;
            viewNeedsRefresh |= instanceChanged;
            
            if (viewNeedsRefresh)
            {
                RefreshView();
                _instance = instance;
            }
            
            UpdateCanvas();
            HandleFenceSelection();

            // Blending...
            IsBlendingActive = ImGui.GetIO().KeyAlt && Selection.SelectedElements.Count is 2 or 3;

            var mousePos = ImGui.GetMousePos();
            if (IsBlendingActive)
            {
                _blendPoints.Clear();
                _blendWeights.Clear();
                _blendVariations.Clear();
                foreach (var s in Selection.SelectedElements)
                {
                    _blendPoints.Add(GetNodeCenterOnScreen(s));
                    _blendVariations.Add(s as Variation);
                }

                if (Selection.SelectedElements.Count == 2)
                {
                    // TODO: Implement
                    _blendWeights.Add(0.5f);
                    _blendWeights.Add(0.5f);
                }
                else
                {
                    Barycentric(mousePos, _blendPoints[0], _blendPoints[1], _blendPoints[2], out var u, out var v, out var w);
                    _blendWeights.Add(u);
                    _blendWeights.Add(v);
                    _blendWeights.Add(w);
                }
            }

            // Rendering thumbnails
            var modified = false;
            for (var index = 0; index < activePoolForPresets.Variations.Count; index++)
            {
                modified |= VariationThumbnail.Draw(this,
                                                    activePoolForPresets.Variations[index],
                                                    drawList,
                                                    _thumbnailCanvasRendering.CanvasTextureSrv,
                                                    GetUvRectForIndex(index));
            }

            // Draw blending overlay
            if (IsBlendingActive && Selection.SelectedElements.Count == 3)
            {
                drawList.AddTriangleFilled(_blendPoints[0], _blendPoints[1], _blendPoints[2], Color.Black.Fade(0.3f));
                foreach (var p in _blendPoints)
                {
                    drawList.AddCircleFilled(p, 5, Color.Black.Fade(0.5f));
                    drawList.AddLine(mousePos, p, Color.White, 2);
                    drawList.AddCircleFilled(p, 3, Color.White);
                }

                drawList.AddCircleFilled(mousePos, 5, Color.White);
                VariationPool.BeginWeightedBlend(_instance, _blendVariations, _blendWeights, UserSettings.Config.PresetsResetToDefaultValues);

                if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                {
                    VariationPool.ApplyCurrentBlend();
                }
            }

            if (modified)
                VariationPool.SaveVariationsToFile();

            DrawContextMenu();
        }

        private readonly List<float> _blendWeights = new(3);
        private readonly List<Vector2> _blendPoints = new(3);
        private readonly List<Variation> _blendVariations = new(3);
        public bool IsBlendingActive { get; private set; }

        public bool TryGetBlendWeight(Variation v, out float weight)
        {
            var index = _blendVariations.IndexOf(v);
            if (index == -1)
            {
                weight = 0;
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
            if (T3Ui.OpenedPopUpName == string.Empty)
            {
                CustomComponents.DrawContextMenuForScrollCanvas(() =>
                                                                {
                                                                    var oneOrMoreSelected = Selection.SelectedElements.Count > 0;
                                                                    var oneSelected = Selection.SelectedElements.Count == 1;

                                                                    if (ImGui.MenuItem("Delete selected",
                                                                                       KeyboardBinding.ListKeyboardShortcuts(UserActions.DeleteSelection,
                                                                                           false),
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

                                                                    ImGui.Separator();
                                                                    if (ImGui.MenuItem("Automatically reset to defaults",
                                                                                       "",
                                                                                       UserSettings.Config.PresetsResetToDefaultValues))
                                                                    {
                                                                        UserSettings.Config.PresetsResetToDefaultValues =
                                                                            !UserSettings.Config.PresetsResetToDefaultValues;
                                                                    }
                                                                }, ref _contextMenuIsOpen);
            }
        }

        private bool _contextMenuIsOpen;

        public void ApplyVariation(Variation variation, bool resetNonDefaults)
        {
            VariationPool.StopHover();

            VariationPool.ApplyPreset(_instance, variation, resetNonDefaults);

            if (resetNonDefaults)
                TriggerThumbnailUpdate();
        }

        public void StartHover(Variation variation)
        {
            VariationPool.BeginHoverPreset(_instance, variation, UserSettings.Config.PresetsResetToDefaultValues);
        }

        public void StartBlendTo(Variation variation, float blend)
        {
            VariationPool.BeginBlendToPresent(_instance, variation, blend, UserSettings.Config.PresetsResetToDefaultValues);
        }

        public void StopHover()
        {
            VariationPool.StopHover();
        }

        public void TriggerThumbnailUpdate()
        {
            _thumbnailCanvasRendering.ClearTexture();
            _updateIndex = 0;
            _updateCompleted = false;
        }

        public void ResetView()
        {
            if (TryToGetBoundingBox(VariationPool.Variations, 20, out var area))
            {
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
            var elementsToSelect = (from child in VariationPool.Variations
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

            _variationsWindow.DeleteVariations(list);
        }

        #region thumbnail rendering
        private void UpdateNextVariationThumbnail(IOutputUi outputUi, Slot<Texture2D> textureSlot)
        {
            if (_updateCompleted)
                return;

            _thumbnailCanvasRendering.InitializeCanvasTexture(VariationThumbnail.ThumbnailSize);
            
            var pool = VariationHandling.ActivePoolForPresets;

            if (pool.Variations.Count == 0)
            {
                _updateCompleted = true;
                return;
            }

            if (_updateIndex >= pool.Variations.Count)
            {
                _updateCompleted = true;
                return;
            }

            var variation = pool.Variations[_updateIndex];
            RenderThumbnail(variation, _updateIndex, outputUi, textureSlot);
            _updateIndex++;
        }

        private void RenderThumbnail(Variation variation, int thumbnailIndex, IOutputUi outputUi, Slot<Texture2D> textureSlot)
        {
            // Set variation values
            VariationPool.BeginHoverPreset(VariationHandling.ActiveInstanceForPresets, variation, UserSettings.Config.PresetsResetToDefaultValues);

            // Render variation
            _thumbnailCanvasRendering.EvaluationContext.Reset();
            _thumbnailCanvasRendering.EvaluationContext.TimeForKeyframes = 13.4f;

            // NOTE: This is horrible hack to prevent _imageCanvas from being rendered by ImGui
            // DrawValue will use the current ImageOutputCanvas for rendering
            _imageCanvas.SetAsCurrent();
            ImGui.PushClipRect(new Vector2(0, 0), new Vector2(1, 1), true);
            outputUi.DrawValue(textureSlot, _thumbnailCanvasRendering.EvaluationContext);
            ImGui.PopClipRect();
            _imageCanvas.Deactivate();

            var rect = GetPixelRectForIndex(thumbnailIndex);

            _thumbnailCanvasRendering.CopyToCanvasTexture(textureSlot, rect);

            VariationPool.StopHover();
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
        private void RefreshView()
        {
            TriggerThumbnailUpdate();
            Selection.Clear();
            ResetView();
        }

        private static bool TryToGetBoundingBox(List<Variation> variations, float extend, out ImRect area)
        {
            area = new ImRect();
            if (VariationPool?.Variations == null)
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
        internal Vector2 FindFreePositionForNewThumbnail(List<Variation> variations)
        {
            if (!TryToGetBoundingBox(variations, 0, out var area))
            {
                return Vector2.Zero;
            }

            // var areaOnScreen = TransformRect(area); 
            // ImGui.GetForegroundDrawList().AddRect(areaOnScreen.Min, areaOnScreen.Max, Color.Blue);
            
            const int columns = 4;
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
            return VariationPool.Variations;
        }

        private static SymbolVariationPool VariationPool => VariationHandling.ActivePoolForPresets;

        private Instance _instance;
        private int _updateIndex;
        private bool _updateCompleted;
        private readonly ImageOutputCanvas _imageCanvas = new();
        private readonly VariationsWindow _variationsWindow;
        private readonly ThumbnailCanvasRendering _thumbnailCanvasRendering = new();
        private SelectionFence.States _fenceState;
        internal readonly CanvasElementSelection Selection = new();
        private Instance _lastRenderInstance;
    }
}