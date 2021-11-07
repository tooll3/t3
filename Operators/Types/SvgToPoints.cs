using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Numerics;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using Svg;
using Svg.Pathing;
using T3.Core.Logging;
using Point = T3.Core.DataTypes.Point;

namespace T3.Operators.Types.Id_e8d94dd7_eb54_42fe_a7b1_b43543dd457e
{
    public class SvgToPoints : Instance<SvgToPoints>
    {
        [Output(Guid = "e21e3843-7d63-4db2-9234-77664e872a0f")]
        public readonly Slot<StructuredList> ResultList = new Slot<StructuredList>();

        public SvgToPoints()
        {
            ResultList.UpdateAction = Update;
            _pointListWithSeparator.TypedElements[_pointListWithSeparator.NumElements - 1] = Point.Separator();
        }

        private void Update(EvaluationContext context)
        {
            var filepath = FilePath.GetValue(context);
            if (!File.Exists(filepath))
            {
                Log.Debug($"File {filepath} doesn't exist");
                return;
            }

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

            GraphicsPath newPath = new GraphicsPath();

            var paths = new List<GraphicsPath>();
            ConvertAllNodesIntoGraphicPaths(svgDoc.Descendants(), paths);
            newPath.Flatten();

            var totalPointCount = 0;
            foreach (var p in paths)
            {
                p.Flatten();
                totalPointCount += p.PointCount + 1;
            }

            if (totalPointCount != _pointListWithSeparator.NumElements)
            {
                _pointListWithSeparator.SetLength(totalPointCount);
            }

            int pointIndex = 0;
            foreach (var path in paths)
            {
                var startIndex = pointIndex;

                for (var pathPointIndex = 0; pathPointIndex < path.PathPoints.Length; pathPointIndex++)
                {
                    var point = path.PathPoints[pathPointIndex];

                    _pointListWithSeparator.TypedElements[startIndex + pathPointIndex].Position
                        = (new Vector3(point.X, 1 - point.Y, 0) + centerOffset) * scale;
                    _pointListWithSeparator.TypedElements[startIndex + pathPointIndex].W = 1;
                    _pointListWithSeparator.TypedElements[startIndex + pathPointIndex].Orientation = Quaternion.Identity;
                    //pointIndex++;
                }

                // Calculate normals
                if (path.PathPoints.Length > 1)
                {
                    for (var pathPointIndex = 0; pathPointIndex < path.PathPoints.Length; pathPointIndex++)
                    {
                        if (pathPointIndex == 0)
                        {
                            _pointListWithSeparator.TypedElements[startIndex + pathPointIndex].Orientation = 
                                RotationFromTwoPositions(_pointListWithSeparator.TypedElements[0].Position, 
                                                         _pointListWithSeparator.TypedElements[1].Position);
                        }
                        else if (pathPointIndex == path.PathPoints.Length-1)
                        {
                            _pointListWithSeparator.TypedElements[startIndex + pathPointIndex].Orientation = 
                                RotationFromTwoPositions(_pointListWithSeparator.TypedElements[path.PathPoints.Length-2].Position, 
                                                         _pointListWithSeparator.TypedElements[path.PathPoints.Length-1].Position);                            
                        }
                        else
                        {
                            _pointListWithSeparator.TypedElements[startIndex + pathPointIndex].Orientation = 
                                RotationFromTwoPositions(_pointListWithSeparator.TypedElements[startIndex + pathPointIndex].Position, 
                                                         _pointListWithSeparator.TypedElements[startIndex + pathPointIndex+1].Position);
                            
                        }
                        // _pointListWithSeparator.TypedElements[startIndex + pathPointIndex].W = 1;
                        // _pointListWithSeparator.TypedElements[startIndex + pathPointIndex].Orientation = Quaternion.Identity;
                    }
                }

                pointIndex += path.PathPoints.Length;

                _pointListWithSeparator.TypedElements[pointIndex] = Point.Separator();
                pointIndex++;
            }
            Log.Debug($"Loaded svg {filepath} with {pointIndex} points");

            ResultList.Value = _pointListWithSeparator;
        }

        private static Quaternion RotationFromTwoPositions(Vector3 p1, Vector3 p2)
        {
            return Quaternion.CreateFromAxisAngle(new Vector3(0,0,1), (float)(Math.Atan2(p1.X - p2.X, -(p1.Y - p2.Y)) + Math.PI /2));
        }

        private void ConvertAllNodesIntoGraphicPaths(IEnumerable<SvgElement> nodes, List<GraphicsPath> paths)
        {
            GraphicsPath path = null;
            foreach (var node in nodes)
            {
                if (!(node is SvgPath svgPath))
                    continue;

                //Log.Debug($"NODE:{svgPath} pathLength:{svgPath.PathLength}");
                foreach (var s in svgPath.PathData)
                {
                    if (s is SvgMoveToSegment
                        || s is SvgClosePathSegment)
                    {
                        if (path != null)
                            paths.Add(path);
                        path = null;
                    }
                    else
                    {
                        CreateOrAppendToPath(s);
                    }
                }

                if (path != null)
                {
                    Log.Warning("Unclosed svg path?");
                }
            }

            void CreateOrAppendToPath(SvgPathSegment s)
            {
                if (path == null)
                    path = new GraphicsPath();

                s.AddToPath(path);
            }
        }

        private readonly StructuredList<Point> _pointListWithSeparator = new StructuredList<Point>(101);

        [Input(Guid = "EF2A461D-C66D-44D8-8B0E-E48A57EC991F")]
        public readonly InputSlot<string> FilePath = new InputSlot<string>();

        [Input(Guid = "C6692E97-E7F8-4B3F-95BC-5F86C2B399A5")]
        public readonly InputSlot<float> Scale = new InputSlot<float>();

        [Input(Guid = "4DFCE92E-9282-486F-A274-E59402696BBB")]
        public readonly InputSlot<bool> CenterToBounds = new InputSlot<bool>();

        [Input(Guid = "221BF10C-B13E-40CF-80AF-769C10A21C5B")]
        public readonly InputSlot<bool> ScaleToBounds = new InputSlot<bool>();
    }
}