using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Gui.Graph;
using T3.Gui.Graph.Rendering;
using T3.Gui.Selection;
using T3.Gui.Styling;
using T3.Gui.Windows.Output;
using UiHelpers;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace T3.Gui.Windows
{
    /// <summary>
    /// Renders the <see cref="PresetWindow"/>
    /// </summary>
    public class PresetWindow : Window
    {
        public PresetWindow()
        {
            Config.Title = "Presets";
            Config.Visible = true;
        }

        protected override void DrawContent()
        {
            ImGui.BeginChild("#params", new Vector2(200, -1));
            {
                foreach (var symbolChildUi in SelectionManager.GetSelectedSymbolChildUis())
                {
                    ImGui.PushFont(Fonts.FontBold);
                    ImGui.Selectable(symbolChildUi.SymbolChild.ReadableName);
                    ImGui.PopFont();
                    ImGui.PushStyleColor(ImGuiCol.Text, Color.Gray.Rgba);
                    foreach (var param in symbolChildUi.SymbolChild.InputValues.Values)
                    {
                        var p = param.DefaultValue;
                        if (p.ValueType == typeof(float)
                            || p.ValueType == typeof(Vector2)
                            || p.ValueType == typeof(Vector3)
                            || p.ValueType == typeof(Vector4)
                            )
                        {
                            ImGui.Selectable(param.Name);
                        }
                    }

                    ImGui.Dummy(Spacing);
                    ImGui.PopStyleColor();
                }
            }
            ImGui.EndChild();
            ImGui.SameLine();

            ImGui.BeginChild("canvas", new Vector2(-1, -1));
            {
                _previewCanvas.Draw();
            }
            ImGui.EndChild();
        }

        public override List<Window> GetInstances()
        {
            return new List<Window>();
        }

        
        
        
        //=====================================================================
        private class PreviewCanvas : ScalableCanvas
        {
            public PreviewCanvas()
            {
                var extend = new Vector2(3, 3);
                var center = new Vector2(VariationGridSize / 2f, VariationGridSize / 2f);
                var left = (center - extend) * _thumbnailSize;
                var right = (center + extend) * _thumbnailSize;

                FitAreaOnCanvas(new ImRect(left, right));
            }


            
            
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
                
                var firstOutput = instance.Outputs[0];
                if (!(firstOutput is Slot<Texture2D> textureSlot))
                {
                    ImGui.Text("Output should be texture");
                    return;
                }
                
                _previewTexture = textureSlot.Value;
                if (_previewTexture == null)
                    return;

                _previewTextureSrv = SrvManager.GetSrvForTexture(_previewTexture);
                
                FillInNextVariation();
                UpdateCanvas();
                Invalidate();

                ImGui.Text(""+Scroll+" " + Scale + " test offset" + _currentOffsetIndexForFocus);
                var drawlist = ImGui.GetWindowDrawList();
                
                // Draw Canvas Texture
                var rectOnScreen = ImRect.RectWithSize(WindowPos, new Vector2(_canvasTexture.Description.Width, _canvasTexture.Description.Height));
                drawlist.AddImage((IntPtr)_canvasTextureSrv,
                                  rectOnScreen.Min,
                                  rectOnScreen.Max);

                foreach (var variation in _variationByGridIndex.Values)
                {
                    var screenRect = GetGridPosScreenRect(variation.GridPos);
                    drawlist.AddRect(screenRect.Min, screenRect.Max, variation.ThumbnailNeedsUpdate ? Color.Orange : Color.Green);
                }
            }
            
            private void InitializeCanvasTexture()
            {
                if (_canvasTexture == null)
                {
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
                }
            }

            private ImRect GetGridPosScreenRect(GridPos gridPos)
            {
                var thumbnailInCanvas = ImRect.RectWithSize(new Vector2(gridPos.X, gridPos.Y) * _thumbnailSize, _thumbnailSize);
                //var screenPos = TransformPosition(thumbnailInCanvas.Min);
                //return new ImRect(screenPos, screenPos + new Vector2(20,20));
                var r= TransformRect(thumbnailInCanvas);
                return new ImRect((int)r.Min.X, (int)r.Min.Y, (int)r.Max.X, (int)r.Max.Y);
            }

            
            private bool IsGridPosVisible(GridPos gridPos)
            {
                var contentRegion = new ImRect(ImGui.GetWindowContentRegionMin() + ImGui.GetWindowPos(),
                                               ImGui.GetWindowContentRegionMax() + ImGui.GetWindowPos());
                
                var rectOnScreen= GetGridPosScreenRect(gridPos);
                var visible= contentRegion.Contains(rectOnScreen);
                return visible;
            }

            
            private void Invalidate()
            {
                var scaleChanged = Math.Abs(Scale.X - _lastScale) > 0.01f;
                if (scaleChanged)
                {
                    _lastScale = Scale.X;
                    _currentOffsetIndexForFocus = 0;
                    _updateCompleted = false;

                    foreach (var variation in _variationByGridIndex.Values)
                    {
                        variation.ThumbnailNeedsUpdate = true;
                    }
                }

                var scrollChanged = Scroll != _lastScroll;
                if (scrollChanged)
                {
                    _currentOffsetIndexForFocus = 0;
                    _updateCompleted = false;
                    _lastScroll = Scroll;
                }
            }


            private void FillInNextVariation()
            {
                if (_updateCompleted)
                    return;

                while (_currentOffsetIndexForFocus < SortedOffset.Length)
                {
                    var offset = SortedOffset[_currentOffsetIndexForFocus++];
                    var gridPos = _currentFocusIndex + offset;
                    if (!gridPos.IsWithinGrid(VariationGridSize))
                        continue;

                    if (!IsGridPosVisible(gridPos))
                        continue;

                    var hasVariation = _variationByGridIndex.TryGetValue(gridPos.GridIndex, out var variation);
                    if (hasVariation)
                    {
                        if (!variation.ThumbnailNeedsUpdate)
                            continue;

                        RenderThumbnail(variation);
                        //variation.UpdateThumbnail();
                        return;
                    }
                    else
                    {
                        variation = CreateVariationAtGridPos(gridPos);
                        RenderThumbnail(variation);
                        //variation.UpdateThumbnail();
                        _variationByGridIndex[gridPos.GridIndex] = variation;
                        return;
                    }
                }

                _updateCompleted = true;
            }

            private static Variation CreateVariationAtGridPos(GridPos pos)
            {
                //TODO: Blend neighbours
                return new Variation(pos);
            }


            private static GridPos[] BuildSortedOffsets()
            {
                var offsets = new List<GridPos>();
                for (var x = -VariationGridSize; x < VariationGridSize; x++)
                {
                    for (var y = -VariationGridSize; y < VariationGridSize; y++)
                    {
                        offsets.Add(new GridPos(x, y));
                    }
                }

                offsets.Sort((a, b) => (a.X * a.X + a.Y * a.Y)
                                       - (b.X * b.X + b.Y * b.Y));

                return offsets.ToArray();
            }

            
            private void RenderThumbnail(Variation variation)
            {
                variation.ThumbnailNeedsUpdate = false;

                var screenRect = GetGridPosScreenRect(variation.GridPos);
                var posInCanvasTexture = screenRect.Min - WindowPos;
                //if (posInCanvasTexture.X < 0 || posInCanvasTexture.X > 500 || posInCanvasTexture.Y < 0 || posInCanvasTexture.Y > 500)
                if (posInCanvasTexture.X < 0  || posInCanvasTexture.Y < 0)
                    return;

                
                var region = new ResourceRegion()
                             {
                                 Left = 0,
                                 Right = (int)screenRect.GetWidth(),
                                 Top = 0,
                                 Bottom = (int)screenRect.GetHeight(),
                                 Back = 1,
                             };
                

                Program.Device.ImmediateContext.CopySubresourceRegion(
                                                                      source: _previewTexture,
                                                                      sourceSubresource: 0,
                                                                      sourceRegion: region,
                                                                      destination: _canvasTexture,
                                                                      destinationSubResource:0,
                                                                      dstX: (int)posInCanvasTexture.X,
                                                                      dstY: (int)posInCanvasTexture.Y,
                                                                      dstZ: 0);
            }

            private static readonly GridPos[] SortedOffset = BuildSortedOffsets();
            private static readonly GridPos GridCenter = new GridPos(VariationGridSize / 2, VariationGridSize / 2);
            private float _lastScale;
            private Vector2 _lastScroll = Vector2.One;

            public override IEnumerable<ISelectableNode> SelectableChildren { get; } = new List<ISelectableNode>();
            private readonly Vector2 _thumbnailSize = new Vector2(160, 160 / 16f * 9);
            private readonly Dictionary<int, Variation> _variationByGridIndex = new Dictionary<int, Variation>();
            private readonly GridPos _currentFocusIndex = GridCenter;
            private int _currentOffsetIndexForFocus;
            private bool _updateCompleted;
            
            private Texture2D _previewTexture;
            private ShaderResourceView _previewTextureSrv;
            
            private Texture2D _canvasTexture;
            private ShaderResourceView _canvasTextureSrv;
        }

        private class Variation
        {
            public readonly GridPos GridPos;
            public bool ThumbnailNeedsUpdate;

            public Variation(GridPos pos)
            {
                GridPos = pos;
                ThumbnailNeedsUpdate = true;
            }

            // public void UpdateThumbnail()
            // {
            //     _canv RenderThumbnail(this);
            //     ThumbnailNeedsUpdate = false;
            // }
        }

        public struct GridPos
        {
            public readonly int X;
            public readonly int Y;

            public GridPos(int x, int y)
            {
                X = x;
                Y = y;
            }

            public static GridPos operator +(GridPos a, GridPos b)
            {
                return new GridPos(a.X + b.X, a.Y + b.Y);
            }

            public bool IsWithinGrid(int gridSize)
            {
                return X > 0 && X < gridSize && Y > 0 && Y < gridSize;
            }

            public int GridIndex => Y * VariationGridSize + X;
        }

        private readonly PreviewCanvas _previewCanvas = new PreviewCanvas();
        private const int VariationGridSize = 100;
        private static readonly Vector2 Spacing = new Vector2(1, 5);
    }
}