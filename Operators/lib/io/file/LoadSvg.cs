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
using T3.Core.Utils;
using Point = T3.Core.DataTypes.Point;

namespace T3.Operators.Types.Id_e8d94dd7_eb54_42fe_a7b1_b43543dd457e
{
    public class LoadSvg : Instance<LoadSvg>
    {
        [Output(Guid = "e21e3843-7d63-4db2-9234-77664e872a0f")]
        public readonly Slot<StructuredList> ResultList = new();

        public LoadSvg()
        {
            ResultList.UpdateAction = Update;
            _pointListWithSeparator.TypedElements[_pointListWithSeparator.NumElements - 1] = Point.Separator();
        }

        private struct GraphicsPathEntry
        {
            public GraphicsPath GraphicsPath;
            public bool NeedsClosing;
        }

        private void Update(EvaluationContext context)
        {
            var filepath = FilePath.GetValue(context);
            if (!File.Exists(filepath))
            {
                Log.Debug($"File {filepath} doesn't exist", this);
                return;
            }
            
            ResourceFileWatcher.AddFileHook(filepath, () => {FilePath.DirtyFlag.Invalidate();});

            var centerToBounds = CenterToBounds.GetValue(context);
            var scaleToBounds = ScaleToBounds.GetValue(context);
            SvgDocument svgDoc;
            try
            {
                svgDoc = SvgDocument.Open<SvgDocument>(filepath, null);
            }
            catch (Exception e)
            {
                Log.Warning($"Failed to load svg document {filepath}:" + e.Message);
                return;
            }

            var bounds = new Vector3(svgDoc.Bounds.Size.Width, svgDoc.Bounds.Size.Height, 0);
            var centerOffset = centerToBounds ? new Vector3(-bounds.X / 2, bounds.Y / 2, 0) : Vector3.Zero;
            var fitBoundsFactor = scaleToBounds ? (2f / bounds.Y) : 1;
            var scale = Scale.GetValue(context) * fitBoundsFactor;
            _importAsLines = ImportAs.GetValue(context) == 0;
            _reduceFactor = ReduceFactor.GetValue(context).Clamp(0.001f, 1f);

            var svgElements = svgDoc.Descendants();
            var pathElements = ConvertAllNodesIntoGraphicPaths(svgElements);

            // Flatten and sum total point count including separators 
            var totalPointCount = 0;
            foreach (var p in pathElements)
            {
                p.GraphicsPath.Flatten(null, _reduceFactor);
                var closePoint = p.NeedsClosing ? 1 : 0;
                totalPointCount += p.GraphicsPath.PointCount + 1 + closePoint;
            }

            if (totalPointCount != _pointListWithSeparator.NumElements)
            {
                _pointListWithSeparator.SetLength(totalPointCount);
            }

            var pointIndex = 0;
            foreach (var pathElement in pathElements)
            {
                var startIndex = pointIndex;

                var path = pathElement.GraphicsPath;
                var pathPointCount = path.PathPoints.Length;
                for (var pathPointIndex = 0; pathPointIndex < pathPointCount; pathPointIndex++)
                {
                    var point = path.PathPoints[pathPointIndex];

                    _pointListWithSeparator.TypedElements[startIndex + pathPointIndex].Position
                        = (new Vector3(point.X, 1 - point.Y, 0) + centerOffset) * scale;
                    _pointListWithSeparator.TypedElements[startIndex + pathPointIndex].W = 1;
                    _pointListWithSeparator.TypedElements[startIndex + pathPointIndex].Orientation = Quaternion.Identity;
                    _pointListWithSeparator.TypedElements[startIndex + pathPointIndex].Color = new Vector4(1.0f); // We need a better fix, maybe with the colors from the SVG file
                }

                // Calculate normals
                if (pathPointCount > 1)
                {
                    for (var pathPointIndex = 0; pathPointIndex < pathPointCount; pathPointIndex++)
                    {
                        if (pathPointIndex == 0)
                        {
                            _pointListWithSeparator.TypedElements[startIndex + pathPointIndex].Orientation =
                                RotationFromTwoPositions(_pointListWithSeparator.TypedElements[0].Position,
                                                         _pointListWithSeparator.TypedElements[1].Position);
                        }
                        else if (pathPointIndex == pathPointCount - 1)
                        {
                            _pointListWithSeparator.TypedElements[startIndex + pathPointIndex].Orientation =
                                RotationFromTwoPositions(_pointListWithSeparator.TypedElements[pathPointCount - 2].Position,
                                                         _pointListWithSeparator.TypedElements[pathPointCount - 1].Position);
                        }
                        else
                        {
                            _pointListWithSeparator.TypedElements[startIndex + pathPointIndex].Orientation =
                                RotationFromTwoPositions(_pointListWithSeparator.TypedElements[startIndex + pathPointIndex].Position,
                                                         _pointListWithSeparator.TypedElements[startIndex + pathPointIndex + 1].Position);
                        }
                    }
                }

                // Close loop?
                if (pathElement.NeedsClosing)
                {
                    _pointListWithSeparator.TypedElements[startIndex + pathPointCount] = _pointListWithSeparator.TypedElements[startIndex];
                    pointIndex++;
                }

                pointIndex += path.PathPoints.Length;

                _pointListWithSeparator.TypedElements[pointIndex] = Point.Separator();
                pointIndex++;
            }

            Log.Debug($"Loaded svg {filepath} with {pointIndex} points", this);

            ResultList.Value = _pointListWithSeparator;
        }

        private static Quaternion RotationFromTwoPositions(Vector3 p1, Vector3 p2)
        {
            return Quaternion.CreateFromAxisAngle(new Vector3(0, 0, 1), (float)(Math.Atan2(p1.X - p2.X, -(p1.Y - p2.Y)) + Math.PI / 2));
        }

        private List<GraphicsPathEntry> ConvertAllNodesIntoGraphicPaths(IEnumerable<SvgElement> nodes)
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
                        if (element is SvgRectangle rect)
                        {
                            //if(element.Transforms.Contains())
                            if (rect.Transforms != null)
                            {
                                foreach (var t in rect.Transforms)
                                {
                                    if (t is not SvgTranslate tr)
                                        continue;

                                    rect.X += tr.X;
                                    rect.Y += tr.Y;
                                }
                            }
                        }

                        var needsClosing = element is SvgRectangle or SvgCircle or SvgEllipse;

                        var graphicsPath = element.Path(_svgRenderer);

                        paths.Add(new GraphicsPathEntry
                                      {
                                          GraphicsPath = graphicsPath,
                                          NeedsClosing = needsClosing && _importAsLines
                                      });
                        break;
                    }
                }
            }

            return paths;
        }

        private readonly StructuredList<Point> _pointListWithSeparator = new(101);
        private bool _importAsLines;
        private float _reduceFactor = 0.5f;

        [Input(Guid = "EF2A461D-C66D-44D8-8B0E-E48A57EC991F")]
        public readonly InputSlot<string> FilePath = new();

        [Input(Guid = "C6692E97-E7F8-4B3F-95BC-5F86C2B399A5")]
        public readonly InputSlot<float> Scale = new();

        [Input(Guid = "4DFCE92E-9282-486F-A274-E59402696BBB")]
        public readonly InputSlot<bool> CenterToBounds = new();

        [Input(Guid = "221BF10C-B13E-40CF-80AF-769C10A21C5B")]
        public readonly InputSlot<bool> ScaleToBounds = new();

        [Input(Guid = "8D63C134-1257-4331-AE84-F5EB6DD66C13", MappedType = typeof(ImportModes))]
        public readonly InputSlot<int> ImportAs = new();

        [Input(Guid = "2BB64740-ED2F-4295-923D-D585D70197E7")]
        public readonly InputSlot<float> ReduceFactor = new();

        private enum ImportModes
        {
            Lines,
            Points,
        }

        private static ISvgRenderer _svgRenderer;
    }
}