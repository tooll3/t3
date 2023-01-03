using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Numerics;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using Svg;
using Svg.Pathing;
using Svg.Transforms;
using T3.Core.Logging;
using T3.Core.Resource;
using T3.Core.Utils;
using Point = T3.Core.DataTypes.Point;

namespace T3.Operators.Types.Id_3d862455_6a7b_4bf6_a159_e4f7cdba6062
{
    public class SvgFontText : Instance<SvgFontText>
    {
        [Output(Guid = "618e9151-cd91-4aa6-9d91-4bb51610cc8b")]
        public readonly Slot<StructuredList> ResultList = new Slot<StructuredList>();

        public SvgFontText()
        {
            ResultList.UpdateAction = Update;
            _pointListWithSeparator.TypedElements[_pointListWithSeparator.NumElements - 1] = Point.Separator();
        }



        private void Update(EvaluationContext context)
        {
            var fontNeedsUpdate = FilePath.DirtyFlag.IsDirty;
            var filepath = FilePath.GetValue(context);
            if (!File.Exists(filepath))
            {
                Log.Debug($"File {filepath} doesn't exist");
                return;
            }

            ResourceFileWatcher.AddFileHook(filepath, () => { FilePath.DirtyFlag.Invalidate(); });
            var text = Text.GetValue(context);

            _definition = SvgFontDefinition.CreateFromFilepath(filepath, fontNeedsUpdate);
            if (_definition == null)
            {
                Log.Debug($"Failed to load svg font {filepath}", this);
                ResultList.Value = null;
                return;
            }

            if (string.IsNullOrEmpty(text))
            {
                ResultList.Value = null;
                return;
            }

            float x = 0;

            var cursorPos = Vector3.Zero;
            var points = new List<Point>(); // TODO: optimize without list
            
            foreach (var c in text)
            {
                if (_definition.GlyphsForCharacters.TryGetValue(c, out var glyph))
                {
                    for (var index = 0; index < glyph.Points.Length; index++)
                    {
                        var p = glyph.Points[index];
                        
                        p.Position += cursorPos;
                        points.Add(p);
                    }

                    cursorPos.X += glyph.AdvanceX;
                }
            }

            if (points.Count == 0)
            {
                Log.Warning("No points found");
                ResultList.Value = null;
                return;
            }
            
            if (_pointListWithSeparator.TypedElements.Length != points.Count)
            {
                _pointListWithSeparator = new StructuredList<Point>(points.Count);
            }
            for (int index = 0; index < points.Count; index++)
            {
                _pointListWithSeparator.TypedElements[index] = points[index];
            }
            
            ResultList.Value = _pointListWithSeparator;
        }

        private StructuredList<Point> _pointListWithSeparator = new(1);

        [Input(Guid = "24b82f4f-2381-4c20-8de8-fe9496ffed95")]
        public readonly InputSlot<string> FilePath = new();

        // [Input(Guid = "3b4e3541-f310-4459-839d-6132c2d565a9")]
        // public readonly InputSlot<float> Scale = new();

        [Input(Guid = "8EE43C95-869E-4390-A567-CB0BB9C31BDD")]
        public readonly InputSlot<string> Text = new();

        // [Input(Guid = "d872ed31-c224-4e01-81d1-ba61c654ff8d")]
        // public readonly InputSlot<bool> CenterToBounds = new();
        //
        // [Input(Guid = "3808c6bb-d6f9-46ee-9716-684c54e1f5cd")]
        // public readonly InputSlot<bool> ScaleToBounds = new();
        //
        // [Input(Guid = "28b7539d-52a9-4050-b522-6a7d5c46b4ae")]
        // public readonly InputSlot<float> ReduceFactor = new();

        private SvgFontDefinition _definition;
    }

 
    
    internal class SvgFontDefinition
    {
        public Dictionary<char,SvgFontGlyph> GlyphsForCharacters = new();
        public readonly Dictionary<int, float> KerningForPairs = new();

        public static SvgFontDefinition CreateFromFilepath(string filepath, bool forceUpdate = false)
        {
            if (string.IsNullOrEmpty(filepath) || !File.Exists(filepath))
                return null;

            var centerOffset = Vector3.Zero; // Todo: Implement properly
            var scale = 1f; // Todo: Implement properly

            if (!forceUpdate && _definitionsForFilePaths.TryGetValue(filepath, out var definition))
                return definition;

            SvgDocument svgDoc;
            try
            {
                svgDoc = SvgDocument.Open<SvgDocument>(filepath, null);
            }
            catch (Exception e)
            {
                Log.Warning($"Failed to load svg document {filepath}:" + e.Message);
                return null;
            }

            var glyphs = new Dictionary<char, SvgFontGlyph>();

            if (svgDoc.Children.Count != 1)
            {
                Log.Debug("Unexpected child in SvgFont definition.");
                return null;
            }

            if (svgDoc.Children[0] is not SvgFont svgFont)
            {
                Log.Debug("Can't find SvgFont definition in svg file.");
                return null;
            }

            foreach (var child in svgFont.Children)
            {
                if (child is not SvgGlyph svgGlyph)
                    continue;

                var svgElements = new List<SvgElement>() { svgGlyph };

                var pathElements = SvgHelper.ConvertAllNodesIntoGraphicPaths(svgElements, true);
                if (pathElements.Count == 0)
                    continue;

                // Flatten and sum total point count including separators 
                var totalPointCount = 0;

                foreach (var t in pathElements)
                {
                    var p = t;
                    try
                    {
                        p.GraphicsPath.Flatten(null, 1);
                        _ = p.GraphicsPath.PathPoints.Length; // Access path points to see if result is valid. 
                    }
                    catch (Exception e)
                    {
                        Log.Debug("Can't flatten element" + e.Message);
                        t.Invalid = true;
                        continue;
                    }

                    var closePoint = p.NeedsClosing ? 1 : 0;
                    totalPointCount += p.GraphicsPath.PointCount + 1 + closePoint;
                }

                var pointListWithSeparator = new Point[totalPointCount];

                // Copy points
                var pointIndex = 0;
                foreach (var pathElement in pathElements)
                {
                    if (pathElement.Invalid)
                        continue;

                    var startIndex = pointIndex;

                    var path = pathElement.GraphicsPath;

                    var pathPointCount = path.PathPoints.Length;

                    for (var pathPointIndex = 0; pathPointIndex < pathPointCount; pathPointIndex++)
                    {
                        var point = path.PathPoints[pathPointIndex];

                        pointListWithSeparator[startIndex + pathPointIndex].Position
                            = (new Vector3(point.X, 1 - point.Y, 0) + centerOffset) * scale;
                        pointListWithSeparator[startIndex + pathPointIndex].W = 1;
                        pointListWithSeparator[startIndex + pathPointIndex].Orientation = Quaternion.Identity;
                    }

                    // Calculate normals
                    if (pathPointCount > 1)
                    {
                        for (var pathPointIndex = 0; pathPointIndex < pathPointCount; pathPointIndex++)
                        {
                            if (pathPointIndex == 0)
                            {
                                pointListWithSeparator[startIndex + pathPointIndex].Orientation =
                                    MathUtils.RotationFromTwoPositions(pointListWithSeparator[0].Position,
                                                                       pointListWithSeparator[1].Position);
                            }
                            else if (pathPointIndex == pathPointCount - 1)
                            {
                                pointListWithSeparator[startIndex + pathPointIndex].Orientation =
                                    MathUtils.RotationFromTwoPositions(pointListWithSeparator[pathPointCount - 2].Position,
                                                                       pointListWithSeparator[pathPointCount - 1].Position);
                            }
                            else
                            {
                                pointListWithSeparator[startIndex + pathPointIndex].Orientation =
                                    MathUtils.RotationFromTwoPositions(pointListWithSeparator[startIndex + pathPointIndex].Position,
                                                                       pointListWithSeparator[startIndex + pathPointIndex + 1].Position);
                            }
                        }
                    }

                    // Close loop?
                    if (pathElement.NeedsClosing)
                    {
                        pointListWithSeparator[startIndex + pathPointCount] = pointListWithSeparator[startIndex];
                        pointIndex++;
                    }

                    pointIndex += path.PathPoints.Length;

                    pointListWithSeparator[pointIndex] = Point.Separator();
                    pointIndex++;
                }

                if (string.IsNullOrEmpty(svgGlyph.Unicode) || svgGlyph.Unicode.Length != 1)
                {
                    Log.Warning("Skipping svg glyph with missing or invalid unicode:" + svgGlyph.GlyphName);
                    continue;
                }
                
                glyphs[svgGlyph.Unicode[0]] = new SvgFontGlyph
                                                  {
                                                      Points = pointListWithSeparator,
                                                      UniCode = svgGlyph.Unicode,
                                                      Name = svgGlyph.GlyphName,
                                                      AdvanceX = svgGlyph.HorizAdvX,
                                                      // VertOriginX = svgGlyph.VertOriginX,
                                                      // VertOriginY = svgGlyph.VertOriginY,
                                                  };
            }

            var newDefinition = new SvgFontDefinition
                                    {
                                        GlyphsForCharacters = glyphs
                                    };
            _definitionsForFilePaths[filepath] = newDefinition;
            return newDefinition;
        }

        private static readonly Dictionary<string, SvgFontDefinition> _definitionsForFilePaths = new();
    }

    public class SvgFontGlyph
    {
        public char Char;
        public float AdvanceX = 10;
        public Point[] Points;
        public string UniCode;
        public string Name;
        public float VertOriginX;
        public float VertOriginY;
    }
    
    public class GraphicsPathEntry
    {
        public GraphicsPath GraphicsPath;
        public bool NeedsClosing;
        public bool Invalid;
    }

    public static class SvgHelper
    {
        private static ISvgRenderer _svgRenderer;

        public static List<GraphicsPathEntry> ConvertAllNodesIntoGraphicPaths(IEnumerable<SvgElement> nodes, bool importAsLines)
        {
            var paths = new List<GraphicsPathEntry>();

            _svgRenderer ??= SvgRenderer.FromImage(new Bitmap(1, 1));

            foreach (var node in nodes)
            {
                GraphicsPath newPath = null;
                switch (node)
                {
                    case SvgPath svgPath:
                    {
                        foreach (var s in svgPath.PathData)
                        {
                            var segmentIsJump = s is SvgMoveToSegment or SvgClosePathSegment;
                            if (segmentIsJump)
                            {
                                if (newPath == null)
                                    continue;

                                paths.Add(new GraphicsPathEntry
                                              {
                                                  GraphicsPath = newPath,
                                                  NeedsClosing = false
                                              });
                                newPath = null;
                            }
                            else
                            {
                                newPath ??= new GraphicsPath();
                                s.AddToPath(newPath);
                            }
                        }

                        if (newPath != null)
                        {
                            paths.Add(new GraphicsPathEntry
                                          {
                                              GraphicsPath = newPath,
                                              NeedsClosing = false
                                          });
                        }

                        break;
                    }
                    case SvgGroup:
                        break;

                    case SvgPathBasedElement element:
                    {
                        if (element is SvgRectangle { Transforms: { } } rect)
                            //if(element.Transforms.Contains())
                        {
                            foreach (var t in rect.Transforms)
                            {
                                if (t is not SvgTranslate tr)
                                    continue;

                                rect.X += tr.X;
                                rect.Y += tr.Y;
                            }
                        }

                        var needsClosing = element is SvgRectangle or SvgCircle or SvgEllipse;

                        var graphicsPath = element.Path(_svgRenderer);

                        paths.Add(new GraphicsPathEntry
                                      {
                                          GraphicsPath = graphicsPath,
                                          NeedsClosing = needsClosing && importAsLines
                                      });
                        break;
                    }
                }
            }

            return paths;
        }
    }
}