using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
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
using Point = T3.Core.DataTypes.Point;
// ReSharper disable TooWideLocalVariableScope

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
                    //Log.Debug(" xoffset" + glyph.VertOriginX);
                    for (var index = 0; index < glyph.Points.Length; index++)
                    {
                        var p = glyph.Points[index];
                        //p.Position.Y *= -1;
                        p.Position.X -= glyph.VertOriginX;

                        p.Position += cursorPos;
                        p.Position *= 0.01f;
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

        [Input(Guid = "8EE43C95-869E-4390-A567-CB0BB9C31BDD")]
        public readonly InputSlot<string> Text = new();


        private SvgFontDefinition _definition;
    }

    internal class SvgFontDefinition
    {
        public Dictionary<char, SvgFontGlyph> GlyphsForCharacters = new();
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

            var svgElementCollection = svgDoc.Children;
            ParseFontDefinitionsInElements(svgElementCollection, centerOffset, scale, glyphs);

            var newDefinition = new SvgFontDefinition
                                    {
                                        GlyphsForCharacters = glyphs
                                    };
            _definitionsForFilePaths[filepath] = newDefinition;
            return newDefinition;

            Log.Debug("Can't find SvgFont definition in svg file.");
            return null;
        }

        private static void ParseFontDefinitionsInElements(SvgElementCollection svgElementCollection, Vector3 centerOffset, float scale,
                                                           Dictionary<char, SvgFontGlyph> glyphs)
        {
            foreach (var svgDocChild in svgElementCollection)
            {
                if (svgDocChild is SvgFont svgFont)
                {
                    CollectGlyphsFromSvgFont(svgFont, centerOffset, scale, glyphs);
                }
                else if (svgDocChild is SvgGroup svgGroup
                         && !string.IsNullOrEmpty(svgGroup.ID)
                         && svgGroup.ID.EndsWith("-Font"))
                {
                    CollectGlyphsFromPathGroup(svgGroup, glyphs, centerOffset, scale);
                }
                else if (svgDocChild is SvgDefinitionList svgDefinitionList)
                {
                    ParseFontDefinitionsInElements(svgDefinitionList.Children, centerOffset, scale, glyphs);
                }
            }
        }

        private static void CollectGlyphsFromPathGroup(SvgGroup svgGroup, Dictionary<char, SvgFontGlyph> glyphs, Vector3 centerOffset, float scale)
        {
            foreach (var groupChild in svgGroup.Children)
            {
                if (groupChild is not SvgGroup glyphGroup)
                    continue;

                var points = GetPointsFromSvgGroup(glyphGroup, centerOffset, scale);
                if (points == null || points.Length == 0)
                    continue;

                var svgGroupId = glyphGroup.ID;
                if (string.IsNullOrEmpty(svgGroupId))
                {
                    Log.Warning("Skipping svg group with missing or invalid single character ID" + svgGroupId);
                    continue;
                }

                if (svgGroupId.Length > 1)
                {
                    svgGroupId = System.Net.WebUtility.HtmlDecode(svgGroupId);
                }

                if (svgGroupId.Length != 1)
                {
                    Log.Warning("Skipping svg invalid single character ID" + svgGroupId);
                    continue;
                }

                glyphs[svgGroupId[0]] = new SvgFontGlyph
                                            {
                                                Points = points,
                                                UniCode = svgGroupId,
                                                Name = svgGroupId,
                                                AdvanceX = glyphGroup.Bounds.Width,
                                            };
            }
        }

        private static void CollectGlyphsFromSvgFont(SvgFont svgFont, Vector3 centerOffset, float scale, Dictionary<char, SvgFontGlyph> glyphs)
        {
            foreach (var svgFontChild in svgFont.Children)
            {
                if (svgFontChild is not SvgGlyph svgGlyph)
                    continue;

                var points = GetPointsFromSvgGroup(svgGlyph, centerOffset, scale);
                if (points == null || points.Length == 0)
                    continue;

                if (string.IsNullOrEmpty(svgGlyph.Unicode) || svgGlyph.Unicode.Length != 1)
                {
                    Log.Warning("Skipping svg glyph with missing or invalid unicode:" + svgGlyph.GlyphName);
                    continue;
                }

                var uniCode = svgGlyph.Unicode[0];
                var glyphValue = (int)uniCode;
                if (glyphValue > 1000 && svgGlyph.GlyphName.Length == 1)
                {
                    uniCode = svgGlyph.GlyphName[0];
                }

                glyphs[uniCode] = new SvgFontGlyph
                                      {
                                          Points = points,
                                          UniCode = "" + uniCode,
                                          Name = svgGlyph.GlyphName,
                                          AdvanceX = svgGlyph.HorizAdvX,
                                          VertOriginX = svgGlyph.VertOriginX,
                                          VertOriginY = svgGlyph.VertOriginY,
                                      };
            }
        }

        private static List<Point> _pointCollection = new List<Point>(100);

        private static Point[] GetPointsFromSvgGroup(SvgElement svgGlyph, Vector3 centerOffset, float scale)
        {
            var svgElements = new List<SvgElement>() { svgGlyph };
            var pathElements = SvgHelper.ConvertAllNodesIntoGraphicPaths(svgElements, true);
            if (pathElements.Count == 0)
                return null;

            // Flatten and sum total point count including separators 
            Point newPoint;

            _pointCollection.Clear();
            foreach (var pathElement in pathElements)
            {
                try
                {
                    pathElement.GraphicsPath.Flatten(null, 0.1f);
                    _ = pathElement.GraphicsPath.PathPoints.Length; // Access path points to see if result is valid. 
                }
                catch (Exception e)
                {
                    Log.Debug("Can't flatten element" + e.Message);
                    continue;
                }


                var path = pathElement.GraphicsPath;

                var pathPointCount = path.PathPoints.Length;

                var loopStartIndex = _pointCollection.Count;
                Vector3 lastPos = new Vector3(-9999f, 0f, 0f);
                
                for (var pathPointIndex = 0; pathPointIndex < pathPointCount; pathPointIndex++)
                {
                    var point = path.PathPoints[pathPointIndex];

                    var position = (new Vector3(point.X, 1 - point.Y, 0) + centerOffset) * scale;
                    var length = (lastPos - position).LengthSquared();
                    lastPos = position;
                    var tooClose = length < 0.000001f;
                    if (tooClose)
                    {
                        continue;
                    }

                    newPoint.Position = position;
                    newPoint.W = 1;
                    newPoint.Orientation = Quaternion.Identity;
                    _pointCollection.Add(newPoint);
                }

                // Close loop?
                if (pathElement.NeedsClosing)
                {
                    _pointCollection.Add(_pointCollection[loopStartIndex]);
                }
                _pointCollection.Add(Point.Separator());
            }

            return _pointCollection.ToArray();
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
                        ConvertPathDataElements(svgPath.PathData, newPath, paths);
                        break;
                    }

                    case SvgGlyph svgGlyph:
                    {
                        ConvertPathDataElements(svgGlyph.PathData, newPath, paths);
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

        private static void ConvertPathDataElements(SvgPathSegmentList svgPathSegmentList, GraphicsPath newPath, List<GraphicsPathEntry> paths)
        {
            if (svgPathSegmentList == null)
                return;

            foreach (var s in svgPathSegmentList)
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
        }
    }
}