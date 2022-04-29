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

        public void Draw(SymbolVariationPool activePoolForPresets)
        {
            _thumbnailCanvasRendering.InitializeCanvasTexture(VariationThumbnail.ThumbnailSize);

            // var outputWindow = OutputWindow.OutputWindowInstances.FirstOrDefault(window => window.Config.Visible) as OutputWindow;
            // if (outputWindow == null)
            // {
            //     ImGui.TextUnformatted("No output window found");
            //     return;
            // }
            //
            // var instance = outputWindow.ShownInstance;
            // if (instance == null || instance.Outputs == null || instance.Outputs.Count == 0)
            // {
            //     CustomComponents.EmptyWindowMessage("To explore variations\nselect a graph operator and\none or more of its parameters.");
            //     return;
            // }
            //
            //
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

            var drawList = ImGui.GetWindowDrawList();
            var modified = false;
            for (var index = 0; index < activePoolForPresets.Variations.Count; index++)
            {
                modified |= VariationThumbnail.Draw(this,
                                        activePoolForPresets.Variations[index],
                                        drawList,
                                        _thumbnailCanvasRendering.CanvasTextureSrv,
                                        GetUvRectForIndex(index));
            }
            
            if(modified)
                VariationPool.SaveVariationsToFile();

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
        
        public void TryToApply(Variation variation, bool resetNonDefaults)
        {
            if (!_updateCompleted)
                return;
            
            VariationPool.StopHover();
            VariationPool.ApplyPreset(_instance, variation, resetNonDefaults);
            if(resetNonDefaults)
                TriggerThumbnailUpdate();
        }

        public void StartHover(Variation variation)
        {
            if (!_updateCompleted)
                return;
            
            VariationPool.BeginHoverPreset(_instance, variation);
        }

        public void StopHover()
        {
            VariationPool.StopHover();
        }
        

        private void RefreshView()
        {
            TriggerThumbnailUpdate();
            Selection.Clear();
            ResetView();
        }

        private void TriggerThumbnailUpdate()
        {
            _thumbnailCanvasRendering.ClearTexture();
            _updateIndex = 0;
            _updateCompleted = false;
        }

        private void ResetView()
        {
            if (VariationPool?.Variations == null)
                return;
            
            var foundOne = false;
            
            ImRect area = new ImRect();
            
            foreach (var v in VariationPool.Variations)
            {
                if (!foundOne)
                {
                    area = new ImRect(v.PosOnCanvas, v.Size);
                    foundOne = true;
                }
                else
                {   
                    area.Add(ImRect.RectWithSize(v.PosOnCanvas,  v.Size));
                }
            }

            if (!foundOne)
                return;
            var extend = new Vector2(20, 20);
            area.Expand(extend);
            FitAreaOnCanvas(area);
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
            VariationPool.BeginHoverPreset(VariationHandling.ActiveInstanceForPresets, variation);

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