using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Numerics;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using T3.Gui.Graph;
using T3.Gui.Interaction.Snapping;
using T3.Gui.Windows.TimeLine;
using UiHelpers;
using Brush = SharpDX.Direct2D1.Brush;

namespace T3.Gui.Animation
{
    /// <summary>
    /// Interaction logic for HorizontalScaleLines.xaml
    /// </summary>
    public class HorizontalScaleLines : IValueSnapAttractor
    {
        public HorizontalScaleLines(ICanvas canvas)
        {
            _canvas = canvas;
        }

        private ICanvas _canvas;


        public void Draw()
        {
            var drawList = ImGui.GetWindowDrawList();

            var bottom = _canvas.WindowPos.Y + _canvas.WindowSize.Y;

            var pixelsPerU = _canvas.Scale.X;
            var offset = _canvas.Scroll.X;
            _usedPositions.Clear();

            if (!(pixelsPerU > 0.001f))
                return;

            Dictionary<int, bool> usedPositions = new Dictionary<int, bool>();

            const float DENSITY = 1.0f;
            float width = _canvas.WindowSize.X;
            float uPerPixel = 1 / pixelsPerU;
            float logScale = (float)Math.Log10(uPerPixel) + DENSITY;
            float logScaleMod = (logScale + 1000) % 1.0f;
            float logScaleFloor = (float)Math.Floor(logScale);

            var lineDefinitions = new List<LineDefinition>();

            if (logScaleMod < 0.5)
            {
                lineDefinitions.Add(new LineDefinition()
                                    {
                                        Label = "N",
                                        Spacing = (float)Math.Pow(10, logScaleFloor) * 50,
                                        LabelOpacity = 1,
                                        LineOpacity = 1
                                    });
                lineDefinitions.Add(new LineDefinition()
                                    {
                                        Label = "N",
                                        Spacing = (float)Math.Pow(10, logScaleFloor) * 10,
                                        LabelOpacity = 1 - logScaleMod * 2,
                                        LineOpacity = 1 - logScaleMod * 2
                                    });
            }
            else
            {
                lineDefinitions.Add(new LineDefinition()
                                    {
                                        Label = "N",
                                        Spacing = (float)Math.Pow(10, logScaleFloor) * 100,
                                        LabelOpacity = 1.0f,
                                        LineOpacity = 1.0f
                                    });
                lineDefinitions.Add(new LineDefinition()
                                    {
                                        Label = "N",
                                        Spacing = (float)Math.Pow(10, logScaleFloor) * 50,
                                        LabelOpacity = 1 - (logScaleMod - 0.5f) * 2,
                                        LineOpacity = 1 - (logScaleMod - 0.5f) * 2
                                    });
            }

            foreach (LineDefinition linedef in lineDefinitions)
            {
                float t = -offset % linedef.Spacing;

                while (t / uPerPixel < width)
                {
                    float u = (t / uPerPixel);
                    float posX = u;
                    int x = (int)posX;

                    if (u > 0 && u < width && !_usedPositions.ContainsKey(x))
                    {
                        _usedPositions[x] = t + offset;

                        var p1 = new Vector2(posX + _canvas.WindowPos.X, bottom - 3);
                        drawList.AddRectFilled(p1, p1 + new Vector2(1, 3), Color.White);
                        //var pen = new Pen(GetTransparentBrush(linedef.LineOpacity * 0.3) , 1);
                        //pen.Freeze();
                        //dc.DrawLine(pen, new Point(posX,0 ), new Point(posX, ActualHeight));


                        if (linedef.Label != "")
                        {
                            String output = "";
                            foreach (char c in linedef.Label)
                            {
                                if (c == 'N')
                                {
                                    output += (t + offset).ToString("G5", CultureInfo.InvariantCulture);
                                }
                                else
                                {
                                    output += c;
                                }
                            }

                            //FormattedText text= new FormattedText(output,
                            //                                        CultureInfo.GetCultureInfo("en-us"),
                            //                                        FlowDirection.LeftToRight,
                            //                                        new Typeface("Verdana"),
                            //                                        7,
                            //                                        GetTransparentLabelBrush( linedef.LabelOpacity * 0.5)
                            //                                        );
                            //text.TextAlignment = TextAlignment.Left;
                            //dc.DrawText(text, new Point(posX + 5, ActualHeight-12.0));

                            var s = ImGui.CalcTextSize(output);
                            var p2 = new Vector2(posX + _canvas.WindowPos.X - s.X * 0.5f, bottom - 16);
                            //drawList.AddRectFilled(p1, p1 + new Vector2(1, 3), Color.White);
                            drawList.AddText(p2, Color.Gray, output);
                        }
                    }

                    t += linedef.Spacing;
                }
            }
        }


        #region implement snap attractor

        private const double SnapThreshold = 8;

        public SnapResult CheckForSnap(double time)
        {
            foreach (var beatTime in _usedPositions.Values)
            {
                double distanceToTime = Math.Abs(time - beatTime) * _canvas.WindowSize.X;
                if (distanceToTime < SnapThreshold)
                {
                    return new SnapResult(beatTime, distanceToTime);
                }
            }

            return null;
        }

        #endregion

        private struct LineDefinition
        {
            public String Label { get; set; }
            public float Spacing { get; set; }
            public float LineOpacity { get; set; }
            public float LabelOpacity { get; set; }
        }

        readonly Dictionary<int, double> _usedPositions = new Dictionary<int, double>();
    }


    public class BeatMarker : IValueSnapAttractor
    {
        public void Draw(ClipTime clipTime)
        {
            if (Math.Abs(_bpm - clipTime.BPM) > 0.001f || _scaleRanges == null)
            {
                _bpm = clipTime.BPM;
                InitializeTimeScaleDefinitions();
            }

            var bpmTimeOffset = 0;
            DrawTimeTicks(TimeLineCanvas.Current.Scale.X, TimeLineCanvas.Current.Scroll.X - bpmTimeOffset);
        }

        private double _bpm = 98;
        private double _bpmTimeOffset = 0;

        private readonly Dictionary<int, double> _usedPositions = new Dictionary<int, double>();
        private List<ScaleFraction> _scaleRanges;
        private const double Epsilon = 0.001f;
        const float density = 0.02f;

        #region paint

        private void DrawTimeTicks(double pixelsPerU, double offset)
        {
            if (!(pixelsPerU > Epsilon))
                return;
            
            var drawList = ImGui.GetWindowDrawList();
            var topLeft = TimeLineCanvas.Current.WindowPos;
            var viewHeight = TimeLineCanvas.Current.WindowSize.Y;
            var width = TimeLineCanvas.Current.WindowSize.X;
            
            _usedPositions.Clear();

            pixelsPerU = 1 / pixelsPerU;

            var scaleRange = _scaleRanges.FirstOrDefault(range => range.ScaleMax * density > pixelsPerU);
            if (scaleRange == null)
                return;
            
            // Debug string 
            // drawList.AddText(topLeft + new Vector2(20, 20), Color.Red, $"Scale: {pixelsPerU:0.1}  f={scaleRange:0}");

            foreach (var lineDefinition in scaleRange.LineDefinitions)
            {
                double t = -offset % lineDefinition.Spacing;

                var fadeFactor = (float)Im.Remap(pixelsPerU, scaleRange.ScaleMin *density, scaleRange.ScaleMax *density, 0, 1);

                var lineAlpha = lineDefinition.FadeLines ? (1 - (float)fadeFactor) : 1;
                var lineColor = new Color(0,0,0,lineAlpha * 0.3f);
                
                var textAlpha = lineDefinition.FadeLabels ? (1 - (float)fadeFactor) : 1;
                var textColor = new Color(textAlpha);

                while (t / pixelsPerU < width)
                {
                    var xIndex = (int)(t / pixelsPerU);

                    if (xIndex > 0 && xIndex < width && !_usedPositions.ContainsKey(xIndex))
                    {
                        _usedPositions[xIndex] = t + offset + _bpmTimeOffset;

                        drawList.AddRect(
                                         new Vector2(topLeft.X + xIndex, topLeft.Y),
                                         new Vector2(topLeft.X + xIndex, topLeft.Y + viewHeight), lineColor);


                        if (lineDefinition.Label != "")
                        {
                            var time = t + offset;
                            var output = "";
                            foreach (char c in lineDefinition.Label)
                            {
                                // bars
                                if (c == 'b')
                                {
                                    var bars = (int)(time * _bpm / 60f / 4f) +1;
                                    output += $"{bars}.";
                                }
                                // beats
                                else if (c == '.')
                                {
                                    var beats = (int)(time * _bpm / 60f ) % 4 +1;
                                    output += $".{beats}";
                                }
                                // ticks
                                else if (c == ':')
                                {
                                    var ticks = (int)(time * _bpm / 60f * 4f) %4 +1;
                                    output += $":{ticks}";
                                }
                                else
                                {
                                    output += c;
                                }
                            }

                            var p = topLeft + new Vector2(xIndex, viewHeight - 15);
                            drawList.AddText(p, textColor, output);
                        }
                    }

                    t += lineDefinition.Spacing;
                }
            }
        }

        private void InitializeTimeScaleDefinitions()
        {
            _scaleRanges = new List<ScaleFraction>
                         {
                             // 0
                             new ScaleFraction()
                             {
                                 ScaleMin = _bpm / 1600,
                                 ScaleMax = _bpm / 900,
                                 LineDefinitions = new List<LineDefinition>
                                                   {
                                                       new LineDefinition()
                                                       {
                                                           Label = "b", Height = 200, Spacing = 64 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = false
                                                       },
                                                       new LineDefinition()
                                                       {
                                                           Label = "b", Height = 200, Spacing = 16 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = false
                                                       },
                                                       new LineDefinition()
                                                       {
                                                           Label = "b", Height = 100, Spacing = 4 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = false
                                                       },
                                                       new LineDefinition()
                                                       {
                                                           Label = "b.", Height = 50, Spacing = 1 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = false
                                                       },
                                                       new LineDefinition()
                                                       {
                                                           Label = "", Height = 200,
                                                           Spacing = 1 / _bpm * 60 / 4, FadeLabels = false,
                                                           FadeLines = true
                                                       },
                                                   }
                             },

                             // 1
                             new ScaleFraction()
                             {
                                 ScaleMin = _bpm / 900,
                                 ScaleMax = _bpm / 700,
                                 LineDefinitions = new List<LineDefinition>
                                                   {
                                                       new LineDefinition()
                                                       {
                                                           Label = "b", Height = 200, Spacing = 64 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = false
                                                       },
                                                       new LineDefinition()
                                                       {
                                                           Label = "b", Height = 200, Spacing = 16 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = false
                                                       },
                                                       new LineDefinition()
                                                       {
                                                           Label = "b", Height = 100, Spacing = 4 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = false
                                                       },
                                                       new LineDefinition()
                                                       {
                                                           Label = "b.", Height = 50, Spacing = 1 / _bpm * 60,
                                                           FadeLabels = true, FadeLines = true
                                                       },
//                                                       new LineDefinition()
//                                                       {
//                                                           Label = "", Height = 200,
//                                                           Spacing = 1 / _bpm * 60 / 4, FadeLabels = false,
//                                                           FadeLines = true
//                                                       },               
                                                   },
                             },


                             // 2
                             new ScaleFraction()
                             {
                                 ScaleMin = _bpm / 700,
                                 ScaleMax = _bpm / 300,
                                 LineDefinitions = new List<LineDefinition>
                                                   {
                                                       new LineDefinition()
                                                       {
                                                           Label = "b", Height = 200, Spacing = 64 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = false
                                                       },
                                                       new LineDefinition()
                                                       {
                                                           Label = "b", Height = 200, Spacing = 16 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = false
                                                       },
                                                       new LineDefinition()
                                                       {
                                                           Label = "b", Height = 100, Spacing = 4 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = false
                                                       },
//                                                       new LineDefinition()
//                                                       {
//                                                           Label = "b.", Height = 50, Spacing = 1 / _bpm * 60,
//                                                           FadeLabels = false, FadeLines = false
//                                                       },
//                                                       new LineDefinition()
//                                                       {
//                                                           Label = "", Height = 200,
//                                                           Spacing = 1 / _bpm * 60 / 4, FadeLabels = false,
//                                                           FadeLines = true
//                                                       },               
                                                   },
                             },
                             // 3
                             new ScaleFraction()
                             {
                                 ScaleMin = _bpm / 300,
                                 ScaleMax = _bpm / 200,
                                 LineDefinitions = new List<LineDefinition>
                                                   {
                                                       new LineDefinition()
                                                       {
                                                           Label = "b", Height = 200, Spacing = 64 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = false
                                                       },
                                                       new LineDefinition()
                                                       {
                                                           Label = "b", Height = 200, Spacing = 16 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = false
                                                       },
                                                       new LineDefinition()
                                                       {
                                                           Label = "b", Height = 100, Spacing = 4 / _bpm * 60,
                                                           FadeLabels = true, FadeLines = false
                                                       },
//                                                       new LineDefinition()
//                                                       {
//                                                           Label = "b.", Height = 50, Spacing = 1 / _bpm * 60,
//                                                           FadeLabels = false, FadeLines = false
//                                                       },
//                                                       new LineDefinition()
//                                                       {
//                                                           Label = "", Height = 200,
//                                                           Spacing = 1 / _bpm * 60 / 4, FadeLabels = false,
//                                                           FadeLines = true
//                                                       },               
                                                   },
                             },
                             // 4
                             new ScaleFraction()
                             {
                                 ScaleMin = _bpm / 200,
                                 ScaleMax = _bpm / 100,
                                 LineDefinitions = new List<LineDefinition>
                                                   {
                                                       new LineDefinition()
                                                       {
                                                           Label = "b", Height = 200, Spacing = 64 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = false
                                                       },
                                                       new LineDefinition()
                                                       {
                                                           Label = "b", Height = 200, Spacing = 16 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = false
                                                       },
                                                       new LineDefinition()
                                                       {
                                                           Label = "", Height = 100, Spacing = 4 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = true
                                                       },
//                                                       new LineDefinition()
//                                                       {
//                                                           Label = "b.", Height = 50, Spacing = 1 / _bpm * 60,
//                                                           FadeLabels = false, FadeLines = false
//                                                       },
//                                                       new LineDefinition()
//                                                       {
//                                                           Label = "", Height = 200,
//                                                           Spacing = 1 / _bpm * 60 / 4, FadeLabels = false,
//                                                           FadeLines = true
//                                                       },               
                                                   },
                             },
                             // 5
                             new ScaleFraction()
                             {
                                 ScaleMin = _bpm / 100,
                                 ScaleMax = _bpm / 50,
                                 LineDefinitions = new List<LineDefinition>
                                                   {
                                                       new LineDefinition()
                                                       {
                                                           Label = "b", Height = 200, Spacing = 64 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = false
                                                       },
                                                       new LineDefinition()
                                                       {
                                                           Label = "b", Height = 200, Spacing = 16 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = false
                                                       },
//                                                       new LineDefinition()
//                                                       {
//                                                           Label = "", Height = 100, Spacing = 4 / _bpm * 60,
//                                                           FadeLabels = false, FadeLines = true
//                                                       },
                                                   }
                             },
                             // 6
                             new ScaleFraction()
                             {
                                 ScaleMin = _bpm / 50,
                                 ScaleMax = _bpm / 20,
                                 LineDefinitions = new List<LineDefinition>
                                                   {
                                                       new LineDefinition()
                                                       {
                                                           Label = "b", Height = 200, Spacing = 64 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = false
                                                       },
                                                       new LineDefinition()
                                                       {
                                                           Label = "b", Height = 200, Spacing = 16 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = false
                                                       },
//                                                       new LineDefinition()
//                                                       {
//                                                           Label = "", Height = 100, Spacing = 4 / _bpm * 60,
//                                                           FadeLabels = false, FadeLines = true
//                                                       },
                                                   }
                             },
                             // 7
                             new ScaleFraction()
                             {
                                 ScaleMin = _bpm / 20,
                                 ScaleMax = _bpm / 8,
                                 LineDefinitions = new List<LineDefinition>
                                                   {
                                                       new LineDefinition()
                                                       {
                                                           Label = "b", Height = 200, Spacing = 64 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = false
                                                       },
                                                       new LineDefinition()
                                                       {
                                                           Label = "b", Height = 200, Spacing = 16 / _bpm * 60,
                                                           FadeLabels = true, FadeLines = true
                                                       },
                                                       //new LineDefinition() { Label="",  Height=100, Spacing= 4/BPM*60,       FadeLabels=false, FadeLines=true  },
                                                       //new LineDefinition() { Label="",  Height=50, Spacing= 1/BPM*60,        FadeLabels=false, FadeLines=true  },
                                                   }
                             },
                             // 8
                             new ScaleFraction()
                             {
                                 ScaleMin = _bpm / 8,
                                 ScaleMax = _bpm / 1,
                                 LineDefinitions = new List<LineDefinition>
                                                   {
                                                       new LineDefinition()
                                                       {
                                                           Label = "b", Height = 200, Spacing = 64 / _bpm * 60,
                                                           FadeLabels = false, FadeLines = false
                                                       },
                                                       //new LineDefinition() { Label="",  Height=200, Spacing= 16/BPM*60,      FadeLabels=false, FadeLines=true  },
                                                       //new LineDefinition() { Label="",  Height=100, Spacing= 4/BPM*60,       FadeLabels=false, FadeLines=true  },
                                                       //new LineDefinition() { Label="",  Height=50, Spacing= 1/BPM*60,        FadeLabels=false, FadeLines=true  },
                                                   }
                             },
                         };
        }

        #endregion


//        private ClipTime _clipTime;

        #region implement snap attractor

        private const double SnapThreshold = 8;

        public SnapResult CheckForSnap(double time)
        {
            foreach (var beatTime in _usedPositions.Values)
            {
                var distanceToTime = Math.Abs(time - beatTime) * TimeLineCanvas.Current.Scale.X;
                if (distanceToTime < SnapThreshold)
                {
                    return new SnapResult(beatTime, SnapThreshold - distanceToTime);
                }
            }

            return null;
        }

        #endregion

        private struct LineDefinition
        {
            public string Label { get; set; }
            public int Height { get; set; }
            public double Spacing { get; set; }
            public bool FadeLabels { get; set; }
            public bool FadeLines { get; set; }
        }

        private class ScaleFraction
        {
            public double ScaleMin { get; set; }
            public double ScaleMax { get; set; }
            public List<LineDefinition> LineDefinitions { get; set; }
        }
    }
}