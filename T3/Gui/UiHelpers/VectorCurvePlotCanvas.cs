using System;
using System.Numerics;
using ImGuiNET;
using T3.Core;
using T3.Core.Logging;
using T3.Gui.Interaction;
using T3.Gui.Styling;
using T3.Gui.Windows.TimeLine;
using UiHelpers;

namespace T3.Gui.UiHelpers
{
    public class VectorCurvePlotCanvas<T>
    {
        public VectorCurvePlotCanvas(int resolution = 500)
        {
            _sampleCount = resolution;
            _graphValues = new T[_sampleCount];
            _graphPoints = new Vector2[MaxComponents, _sampleCount];

            if (typeof(T) == typeof(float))
            {
                _componentCount = 1;
            }
            else if (typeof(T) == typeof(Vector2))
            {
                _componentCount = 2;
            }
            else if (typeof(T) == typeof(Vector3))
            {
                _componentCount = 3;
            }
            else if (typeof(T) == typeof(Vector4))
            {
                _componentCount = 4;
            }
        }

        private readonly int _componentCount = 1;

        public void Draw(T value)
        {
            var dl = ImGui.GetForegroundDrawList();
            var min = float.PositiveInfinity;
            var max = float.NegativeInfinity;

            var newValueComponents = Utilities.GetFloatsFromVector(value);

            foreach (var vv in _graphValues)
            {
                var components = Utilities.GetFloatsFromVector<T>(vv);
                foreach (var v in components)
                {
                    if (v > max)
                        max = v;

                    if (v < min)
                        min = v;
                }
            }

            var padding = (max - min) * 0.2f;
            min -= padding;
            max += padding;
            dl.PushClipRect(_canvas.WindowPos, _canvas.WindowPos + _canvas.WindowSize, true);
            if (ImGui.IsWindowHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                _paused = !_paused;
            }

            if (!ImGui.IsWindowFocused())
            {
                _paused = false;
            }

            if (!_paused)
            {
                _canvas.SetScopeToCanvasArea(new ImRect(0, min, 1, max), flipY: true);
                _lastValues = newValueComponents;
            }

            _canvas.UpdateCanvas();
            _raster.Draw(_canvas);
            if (!_paused)
            {
                _graphValues[_sampleOffset] = value;
                _sampleOffset = (_sampleOffset + 1) % _sampleCount;
            }

            var x = +_canvas.WindowPos.X;
            var dx = (_canvas.WindowSize.X - 3) / _sampleCount;
            for (var index = 0; index < _graphValues.Length; index++)
            {
                var v = _graphValues[(index + _sampleOffset) % _sampleCount];
                var components = Utilities.GetFloatsFromVector(v);
                for (var cIndex = 0; cIndex < components.Length; cIndex++)
                {
                    var color = _componentCount == 1 ? GrayCurveColor : CurveColors[cIndex];
                    
                    var c = components[cIndex];
                    _graphPoints[cIndex,index] = new Vector2((int)x,
                                                      (int)_canvas.TransformY(c));
                }
                x += dx;
            }

            for(int cIndex= 0; cIndex< _componentCount; cIndex ++)
            {
                var color = _componentCount == 1 ? GrayCurveColor : CurveColors[cIndex];
                dl.AddPolyline(ref _graphPoints[cIndex,0], _sampleCount - 1, color, ImDrawFlags.None, 1);
                dl.AddCircleFilled(_graphPoints[cIndex, _sampleCount - 1], 3, color);
            }

            
            var y = _canvas.WindowSize.Y * 0.5f - Fonts.FontLarge.FontSize * _componentCount / 2;

            for (var cIndex = 0; cIndex < _lastValues.Length; cIndex++)
            {
                var color = _componentCount == 1 ? GrayCurveColor : TextColors[cIndex];
                color.Rgba.W= 1;
                
                var lastValue = _lastValues[cIndex];
                var valueAsString = $"{lastValue:G4}";
                dl.AddText(Fonts.FontLarge,
                           Fonts.FontLarge.FontSize,
                           _canvas.WindowPos
                           + new Vector2(_canvas.WindowSize.X - 100f,
                                         y),
                           color,
                           valueAsString);
                y += Fonts.FontLarge.FontSize;
            }

            dl.PopClipRect();
        }

        public void Reset(T clearValue)
        {
            for (var index = 0; index < _sampleCount; index++)
            {
                _graphValues[index] = clearValue;
            }
        }

        private readonly T[] _graphValues;
        private readonly Vector2[,] _graphPoints;
        private int _sampleOffset;
        private readonly int _sampleCount;

        private bool _paused;
        private float[] _lastValues = new float[MaxComponents];

        private readonly HorizontalRaster _raster = new();
        private readonly ScalableCanvas _canvas = new() { FillMode = ScalableCanvas.FillModes.FillAvailableContentRegion };
        private const int MaxComponents = 4;
        
        private static readonly Color GrayCurveColor = new Color(1f, 1f, 1.0f, 0.3f);
        internal static readonly Color[] CurveColors =
            {
                new Color(1f, 0.2f, 0.2f, 0.3f),
                new Color(0.1f, 1f, 0.2f, 0.3f),
                new Color(0.1f, 0.4f, 1.0f, 0.5f),
                new Color(0.5f, 0.5f, 0.5f, 0.5f),
                GrayCurveColor,
            };
        
        internal static readonly Color[] TextColors =
            {
                new Color(1f, 0.5f, 0.5f, 1f),
                new Color(0.4f, 1f, 0.5f, 1f),
                new Color(0.6f, 0.671f, 1.0f, 1f),
                new Color(0.6f, 0.6f, 0.6f, 1f),
                GrayCurveColor,
            };        
    }
}