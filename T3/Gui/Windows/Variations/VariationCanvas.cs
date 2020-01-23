using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;
using T3.Core;
using T3.Core.Operator;
using T3.Gui.Graph;
using T3.Gui.Graph.Rendering;
using T3.Gui.Selection;
using T3.Gui.Windows.Output;
using UiHelpers;
using Vector2 = System.Numerics.Vector2;

namespace T3.Gui.Windows.Variations
{
    public class VariationCanvas : ScalableCanvas
    {
        public VariationCanvas(VariationWindow variationWindow)
        {
            var extend = new Vector2(3, 3);
            var center = new Vector2(GridPos.VariationGridSize / 2f, GridPos.VariationGridSize / 2f);
            var left = (center - extend) * ThumbnailSize;
            var right = (center + extend) * ThumbnailSize;
            ZoomSpeed = 20000;

            FitAreaOnCanvas(new ImRect(left, right));
            _variationWindow = variationWindow;
        }

        private ISlot _firstOutputSlot;

        public void Draw()
        {
            InitializeCanvasTexture();

            var outputWindow = OutputWindow.OutputWindowInstances.FirstOrDefault(window => window.Config.Visible) as OutputWindow;
            if (outputWindow == null)
            {
                ImGui.Text("No output window found");
                return;
            }

            var instance = outputWindow.ShownInstance;
            if (instance == null)
            {
                ImGui.Text("Nothing selected");
                return;
            }

            _firstOutputSlot = instance.Outputs[0];
            if (!(_firstOutputSlot is Slot<Texture2D> textureSlot))
            {
                ImGui.Text("Output should be texture");
                _firstOutputSlot = null;
                return;
            }

            // Set correct output ui
            {
                var symbolUi = SymbolUiRegistry.Entries[instance.Symbol.Id];
                if (!symbolUi.OutputUis.ContainsKey(_firstOutputSlot.Id))
                    return;

                _variationWindow.OutputUi = symbolUi.OutputUis[_firstOutputSlot.Id];
            }

            _previewTexture = textureSlot.Value;
            if (_previewTexture == null)
                return;

            _previewTextureSrv = SrvManager.GetSrvForTexture(_previewTexture);

            FillInNextVariation();
            UpdateCanvas();
            Invalidate();

            // Draw Canvas Texture
            var rectOnScreen = ImRect.RectWithSize(WindowPos, new Vector2(_canvasTexture.Description.Width, _canvasTexture.Description.Height));

            var drawlist = ImGui.GetWindowDrawList();
            drawlist.AddImage((IntPtr)_canvasTextureSrv, rectOnScreen.Min, rectOnScreen.Max);

            foreach (var variation in _variationByGridIndex.Values)
            {
                if (!IsGridPosVisible(variation.GridPos))
                    continue;

                var screenRect = GetGridPosScreenRect(variation.GridPos);
                if (variation.ThumbnailNeedsUpdate)
                {
                    drawlist.AddRectFilled(screenRect.Min, screenRect.Max, NeedsUpdateColor);
                }
                else
                {
                    //drawlist.AddRect(screenRect.Min, screenRect.Max, Color.Black);
                    //drawlist.AddRectFilled(screenRect.Min, screenRect.Max, Color.Green);
                }
            }

            if (ImGui.IsWindowHovered())
            {
                var gridPosUnderMouse = GetScreenRectForGridCell(ImGui.GetMousePos());
                if (_variationByGridIndex.TryGetValue(gridPosUnderMouse.GridIndex, out var variation))
                {
                    var hoverVariation = CreateVariationAtMouseMouse();
                    //variation.ApplyValues();
                    hoverVariation.ApplyValues();
                }

                _hoveringVariation = variation;
            }
            else
            {
                if (_hoveringVariation != null)
                {
                    _hoveringVariation.RestoreValues();
                    _hoveringVariation = null;
                }
            }
        }

        private void InitializeCanvasTexture()
        {
            if (_canvasTexture != null)
                return;

            var description = new Texture2DDescription()
                              {
                                  Height = 2048,
                                  Width = 2048,
                                  ArraySize = 1,
                                  BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
                                  Usage = ResourceUsage.Default,
                                  CpuAccessFlags = CpuAccessFlags.None,
                                  Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
                                  //MipLevels = mipLevels,
                                  OptionFlags = ResourceOptionFlags.GenerateMipMaps,
                                  SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                              };

            _canvasTexture = new Texture2D(Program.Device, description);
            _canvasTextureSrv = SrvManager.GetSrvForTexture(_canvasTexture);
            _canvasTextureRtv = new RenderTargetView(Program.Device, _canvasTexture);
        }

        private ImRect GetGridPosScreenRect(GridPos gridPos)
        {
            var thumbnailInCanvas = ImRect.RectWithSize(new Vector2(gridPos.X, gridPos.Y) * ThumbnailSize, ThumbnailSize);
            var r = TransformRect(thumbnailInCanvas);
            return new ImRect((int)r.Min.X, (int)r.Min.Y, (int)r.Max.X - 1, (int)r.Max.Y - 1);
        }

        private GridPos GetScreenRectForGridCell(Vector2 screenPos)
        {
            var centerInCanvas = InverseTransformPosition(screenPos);
            return new GridPos(
                               (int)(centerInCanvas.X / ThumbnailSize.X),
                               (int)(centerInCanvas.Y / ThumbnailSize.Y));
        }

        private bool IsGridPosVisible(GridPos gridPos)
        {
            var contentRegion = new ImRect(ImGui.GetWindowContentRegionMin() + ImGui.GetWindowPos(),
                                           ImGui.GetWindowContentRegionMax() + ImGui.GetWindowPos());

            contentRegion.Expand(ThumbnailSize * Scale);

            var rectOnScreen = GetGridPosScreenRect(gridPos);

            var visible = contentRegion.Contains(rectOnScreen);
            return visible;
        }

        private void Invalidate()
        {
            var scaleChanged = Math.Abs(Scale.X - _lastScale) > 0.01f;
            var scrollChanged = Scroll != _lastScroll;

            if (Math.Abs(_lastScatter - Scatter) > 0.01f)
            {
                _lastScatter = Scatter;
                ClearVariations();
            }

            // TODO: optimize performance by only invalidating thumbnails and moving part of the canvas  
            if (scaleChanged || scrollChanged)
            {
                _lastScale = Scale.X;
                _lastScroll = Scroll;

                _currentOffsetIndexForFocus = 0;
                _updateCompleted = false;

                foreach (var variation in _variationByGridIndex.Values)
                {
                    variation.ThumbnailNeedsUpdate = true;
                }

                if (ImGui.IsWindowHovered())
                {
                    SetGridFocusToMousePos();
                }
                else
                {
                    SetGridFocusToWindowCenter();
                }

                Program.Device.ImmediateContext.ClearRenderTargetView(_canvasTextureRtv, new RawColor4(0, 0, 0, 0));
            }
        }

        private void SetGridFocusToWindowCenter()
        {
            var contentRegion = new ImRect(ImGui.GetWindowContentRegionMin() + ImGui.GetWindowPos(),
                                           ImGui.GetWindowContentRegionMax() + ImGui.GetWindowPos());
            var centerInCanvas = InverseTransformPosition(contentRegion.GetCenter());
            _gridFocusIndex.X = (int)(centerInCanvas.X / ThumbnailSize.X);
            _gridFocusIndex.Y = (int)(centerInCanvas.Y / ThumbnailSize.Y);
        }

        private void SetGridFocusToMousePos()
        {
            var centerInCanvas = InverseTransformPosition(ImGui.GetMousePos());
            _gridFocusIndex.X = (int)(centerInCanvas.X / ThumbnailSize.X);
            _gridFocusIndex.Y = (int)(centerInCanvas.Y / ThumbnailSize.Y);
        }

        private void FillInNextVariation()
        {
            if (_updateCompleted)
                return;

            while (_currentOffsetIndexForFocus < SortedOffset.Length)
            {
                var offset = SortedOffset[_currentOffsetIndexForFocus++];
                var gridPos = _gridFocusIndex + offset;
                if (!gridPos.IsWithinGrid())
                    continue;

                if (!IsGridPosVisible(gridPos))
                    continue;

                var hasVariation = _variationByGridIndex.TryGetValue(gridPos.GridIndex, out var variation);
                if (hasVariation)
                {
                    if (!variation.ThumbnailNeedsUpdate)
                        continue;

                    RenderThumbnail(variation);
                    return;
                }
                else
                {
                    variation = CreateVariationAtGridPos(gridPos);
                    if (variation.ValuesForParameters.Count > 0)
                    {
                        RenderThumbnail(variation);
                        _variationByGridIndex[gridPos.GridIndex] = variation;
                    }

                    return;
                }
            }

            _updateCompleted = true;
        }

        public void ClearVariations()
        {
            _currentOffsetIndexForFocus = 0;
            _updateCompleted = false;
            _variationByGridIndex.Clear();
        }


        private Variation CreateVariationAtMouseMouse()
        {
            var mousePos = ImGui.GetMousePos();
            var gridPosBelowMouse = GetScreenRectForGridCell(mousePos);
            var region = GetGridPosScreenRect(gridPosBelowMouse);
            var posInCell = mousePos - region.Min;

            var cellSize = region.GetSize();
            var halfSize = region.GetSize() / 2;

            posInCell -= halfSize;
            if (posInCell.X < 0)
            {
                gridPosBelowMouse.X--;
                posInCell.X += halfSize.X;
            }
            else
            {
                posInCell.X -= halfSize.X;
            }

            if (posInCell.Y < 0)
            {
                gridPosBelowMouse.Y--;
                posInCell.Y += halfSize.Y;
            }
            else
            {
                posInCell.Y -= halfSize.Y;
            }

            ImGui.GetForegroundDrawList().AddRect(region.Min, region.Max, Color.Orange);

            var clamp = cellSize / 2f * HoverEdgeBlendFactor;
            var xWeight = posInCell.X.Clamp(-clamp.X, clamp.X) / clamp.X / 2 + 0.5f;
            var yWeight = posInCell.Y.Clamp(-clamp.Y, clamp.Y) / clamp.Y / 2 + 0.5f;

            var neighbours = new List<Tuple<Variation, float>>();

            if (_variationByGridIndex.TryGetValue(gridPosBelowMouse.GridIndex, out var variationTopLeft))
            {
                var weight = (1 - xWeight) * (1 - yWeight);
                neighbours.Add(new Tuple<Variation, float>(variationTopLeft, weight));
            }

            if (_variationByGridIndex.TryGetValue((gridPosBelowMouse + new GridPos(1, 0)).GridIndex, out var variationTopRight))
            {
                var weight = xWeight * (1 - yWeight);
                neighbours.Add(new Tuple<Variation, float>(variationTopRight, weight));
            }

            if (_variationByGridIndex.TryGetValue((gridPosBelowMouse + new GridPos(0, 1)).GridIndex, out var variationBottomLeft))
            {
                var weight = (1 - xWeight) * yWeight;
                neighbours.Add(new Tuple<Variation, float>(variationBottomLeft, weight));
            }

            if (_variationByGridIndex.TryGetValue((gridPosBelowMouse + new GridPos(1, 1)).GridIndex, out var variationBottomRight))
            {
                var weight = xWeight * yWeight;
                neighbours.Add(new Tuple<Variation, float>(variationBottomRight, weight));
            }

            return Variation.Mix(_variationWindow.VariationParameters, neighbours, 0);
        }

        private Variation CreateVariationAtGridPos(GridPos pos)
        {
            // Collect neighbours
            var neighboursAndWeights = new List<Tuple<Variation, float>>();
            foreach (var nOffset in NeighbourOffsets)
            {
                var neighbourPos = pos + nOffset;

                if (_variationByGridIndex.TryGetValue(neighbourPos.GridIndex, out var neighbour))
                    neighboursAndWeights.Add(new Tuple<Variation, float>(neighbour, 1));
            }

            return Variation.Mix(_variationWindow.VariationParameters, neighboursAndWeights, Scatter, pos);
        }


        private void RenderThumbnail(Variation variation)
        {
            variation.ThumbnailNeedsUpdate = false;

            var screenRect = GetGridPosScreenRect(variation.GridPos);
            var posInCanvasTexture = screenRect.Min - WindowPos;

            // Set variation values
            variation.ApplyValues();

            // Render variation
            EvaluationContext.Reset();
            EvaluationContext.BeatTime = 13.4f;
            _variationWindow.OutputUi.DrawValue(_firstOutputSlot, EvaluationContext);

            // Setup graphics pipeline for rendering into the canvas texture
            var resourceManager = ResourceManager.Instance();
            var deviceContext = resourceManager.Device.ImmediateContext;
            deviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            deviceContext.Rasterizer.SetViewport(new ViewportF(posInCanvasTexture.X, posInCanvasTexture.Y,
                                                               screenRect.GetWidth(), screenRect.GetHeight(),
                                                               0.0f, 1.0f));
            deviceContext.OutputMerger.SetTargets(_canvasTextureRtv);

            var vertexShader = resourceManager.GetVertexShader(Program.FullScreenVertexShaderId);
            deviceContext.VertexShader.Set(vertexShader);
            var pixelShader = resourceManager.GetPixelShader(Program.FullScreenPixelShaderId);
            deviceContext.PixelShader.Set(pixelShader);
            deviceContext.PixelShader.SetShaderResource(0, _previewTextureSrv);

            // Render the preview in the canvas texture
            deviceContext.Draw(3, 0);
            deviceContext.PixelShader.SetShaderResource(0, null);

            // Restore values
            variation.RestoreValues();
        }
        
        private const float HoverEdgeBlendFactor = 0.5f;
        private static readonly GridPos[] SortedOffset = GridPos.BuildSortedOffsets();
        private static readonly GridPos GridCenter = GridPos.Center;
        private float _lastScale;
        private Vector2 _lastScroll = Vector2.One;

        public override IEnumerable<ISelectableNode> SelectableChildren { get; } = new List<ISelectableNode>();
        private static readonly Vector2 ThumbnailSize = new Vector2(160, 160 / 16f * 9);
        private static readonly Color NeedsUpdateColor = new Color(1f, 1f, 1f, 0.05f);
        private readonly Dictionary<int, Variation> _variationByGridIndex = new Dictionary<int, Variation>();
        private GridPos _gridFocusIndex = GridCenter;
        private int _currentOffsetIndexForFocus;
        private bool _updateCompleted;

        private Texture2D _previewTexture;
        private ShaderResourceView _previewTextureSrv;

        private Texture2D _canvasTexture;
        private ShaderResourceView _canvasTextureSrv;
        private RenderTargetView _canvasTextureRtv;
        private readonly VariationWindow _variationWindow;
        private Variation _hoveringVariation;

        private static readonly GridPos[] NeighbourOffsets =
        {
            new GridPos(-1, -1),
            new GridPos(0, -1),
            new GridPos(1, -1),
            new GridPos(1, 0),
            new GridPos(1, 1),
            new GridPos(0, 1),
            new GridPos(-1, 1),
            new GridPos(-1, 0),
        };


        public float Scatter = 0.5f;
        private float _lastScatter;

        private static readonly EvaluationContext EvaluationContext = new EvaluationContext()
                                                                      {
                                                                          RequestedResolution = new Size2((int)ThumbnailSize.X, (int)ThumbnailSize.Y)
                                                                      };

        
    }
}