using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Gui.Interaction;
using T3.Gui.Interaction.Variations;
using T3.Gui.Interaction.Variations.Model;
using T3.Gui.Selection;
using T3.Gui.UiHelpers;
using T3.Gui.Windows.Exploration;
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
            if (!T3Ui.IsCurrentlySaving && KeyboardBinding.Triggered(UserActions.DeleteSelection))
            {
                DeleteSelectedElements();
            }

            _thumbnailCanvasRendering.InitializeCanvasTexture(VariationThumbnail.ThumbnailSize);

            var instance = VariationHandling.ActiveInstanceForPresets;
            if (instance != _instance)
            {
                RefreshView();
                _instance = instance;
            }

            // Draw Canvas Texture
            _firstOutputSlot = instance.Outputs[0];
            if (!(_firstOutputSlot is Slot<Texture2D> textureSlot))
            {
                CustomComponents.EmptyWindowMessage("Output window must be pinned\nto a texture operator.");
                _firstOutputSlot = null;
                return;
            }

            // Set correct output ui
            {
                var symbolUi = SymbolUiRegistry.Entries[instance.Symbol.Id];
                if (!symbolUi.OutputUis.ContainsKey(_firstOutputSlot.Id))
                    return;

                _variationsWindow.OutputUi = symbolUi.OutputUis[_firstOutputSlot.Id];
            }

            if (textureSlot.Value == null)
                return;

            UpdateNextVariationThumbnail();
            UpdateCanvas();
            HandleFenceSelection();

            var modified = false;
            for (var index = 0; index < activePoolForPresets.Variations.Count; index++)
            {
                modified |= VariationThumbnail.Draw(this,
                                                    activePoolForPresets.Variations[index],
                                                    drawList,
                                                    _thumbnailCanvasRendering.CanvasTextureSrv,
                                                    GetUvRectForIndex(index));
            }

            if (modified)
                VariationPool.SaveVariationsToFile();

            DrawContextMenu();

            // Draw Canvas Texture
            // var canvasSize = _thumbnailCanvasRendering.GetCanvasTextureSize();
            // var rectOnScreen = ImRect.RectWithSize(WindowPos, canvasSize);
            // drawList.AddImage((IntPtr)_thumbnailCanvasRendering.CanvasTextureSrv, rectOnScreen.Min, rectOnScreen.Max);

            // _hoveringVariation?.RestoreValues();
            // var size = THelpers.GetContentRegionArea();
            // ImGui.InvisibleButton("variationCanvas", size.GetSize());
            //
            // if (ImGui.IsItemHovered())
            // {
            //     _hoveringVariation = CreateVariationAtMouseMouse();
            //
            //     if (_hoveringVariation != null)
            //     {
            //         if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            //         {
            //             var savedVariation = _hoveringVariation.Clone();
            //
            //             _explorationWindow.SaveVariation(savedVariation);
            //             savedVariation.ApplyPermanently();
            //         }
            //
            //         _hoveringVariation.KeepCurrentAndApplyNewValues();
            //     }
            // }
            // else
            // {
            //     _hoveringVariation = null;
            // }
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
                                                                                       KeyboardBinding.ListKeyboardShortcuts(UserActions.DeleteSelection, false),
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

        public void TryToApply(Variation variation, bool resetNonDefaults)
        {
            if (!_updateCompleted)
                return;

            VariationPool.StopHover();
            VariationPool.ApplyPreset(_instance, variation, resetNonDefaults);
            if (resetNonDefaults)
                TriggerThumbnailUpdate();
        }

        public void StartHover(Variation variation)
        {
            if (!_updateCompleted)
                return;

            VariationPool.BeginHoverPreset(_instance, variation, UserSettings.Config.PresetsResetToDefaultValues);
        }

        public void StopHover()
        {
            VariationPool.StopHover();
        }

        public void RefreshView()
        {
            TriggerThumbnailUpdate();
            Selection.Clear();
            ResetView();
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

        private void UpdateNextVariationThumbnail()
        {
            if (_updateCompleted)
                return;

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
            RenderThumbnail(variation, _updateIndex);
            _updateIndex++;
        }

        private void RenderThumbnail(Variation variation, int thumbnailIndex)
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
            _variationsWindow.OutputUi.DrawValue(_firstOutputSlot, _thumbnailCanvasRendering.EvaluationContext);
            ImGui.PopClipRect();
            _imageCanvas.Deactivate();

            var rect = GetPixelRectForIndex(thumbnailIndex);

            if (_firstOutputSlot is Slot<Texture2D> textureSlot)
            {
                _thumbnailCanvasRendering.CopyToCanvasTexture(textureSlot, rect);
            }

            VariationPool.StopHover();
        }

        private ImRect GetPixelRectForIndex(int thumbnailIndex)
        {
            var columns = (int)(_thumbnailCanvasRendering.GetCanvasTextureSize().X / VariationThumbnail.ThumbnailSize.X);
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

        /// <summary>
        /// This uses a primitive algorithm: Look for the bottom edge of a all element bounding box
        /// Then step through possible positions and check if a position would intersect with an existing element.
        /// Wrap columns to enforce some kind of grid.  
        /// </summary>
        public static Vector2 FindFreePositionForNewThumbnail(List<Variation> variations)
        {
            if (!TryToGetBoundingBox(variations, 0, out var area))
            {
                return Vector2.Zero;
            }
            
            const int columns = 4;
            var columnIndex = 0;

            var stepWidth = VariationThumbnail.ThumbnailSize.X + VariationThumbnail.SnapPadding.X;
            var stepHeight = VariationThumbnail.ThumbnailSize.Y + VariationThumbnail.SnapPadding.Y;
            
            var pos = new Vector2(area.Min.X, area.Max.Y - VariationThumbnail.ThumbnailSize.Y);
            var rowStartPos = pos;

            while (true)
            {
                var intersects = false;
                var targetArea = new ImRect(pos, pos + VariationThumbnail.ThumbnailSize);

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

        private ISlot _firstOutputSlot;

        private readonly ThumbnailCanvasRendering _thumbnailCanvasRendering = new();
        private SelectionFence.States _fenceState;
        internal readonly CanvasElementSelection Selection = new();
    }
}