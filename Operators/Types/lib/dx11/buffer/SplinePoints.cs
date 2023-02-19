using System;
using System.Linq;
using System.Runtime.CompilerServices;
using SharpDX;
using SharpDX.Direct3D11;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Core.Utils;
using Buffer = SharpDX.Direct3D11.Buffer;
using Point = T3.Core.DataTypes.Point;
using Quaternion = System.Numerics.Quaternion;
using Vector3 = System.Numerics.Vector3;

namespace T3.Operators.Types.Id_edecd98f_209b_423d_8201_0fd7d590c4cf
{
    public class SplinePoints : Instance<SplinePoints>
    {
        [Output(Guid = "28b45955-1e05-43a9-87b6-44eabc30bea7")]
        public readonly Slot<BufferWithViews> OutBuffer = new();

        [Output(Guid = "1B0A8C95-CF11-4EF6-BDB7-C54D0CD7BEB7")]
        public readonly Slot<StructuredList> SampledPoints = new();
        
        public SplinePoints()
        {
            OutBuffer.UpdateAction = Update;
            SampledPoints.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            
            if (Points.DirtyFlag.IsDirty || SampleCount.DirtyFlag.IsDirty)
            {
                var resultCount = SampleCount.GetValue(context).Clamp(1, 1000);
                try
                {
                    var pointsCollectedInputs = Points.CollectedInputs;

                    var connectedLists = pointsCollectedInputs.Select(c => c.GetValue(context)).Where(c => c != null).ToList();
                    Points.DirtyFlag.Clear();

                    if (connectedLists.Count < 2)
                    {
                        _buffer = null;
                        OutBuffer.Value = null;
                        Log.Warning("Need at least 2 points", this);
                        return;
                    }

                    var sourceItems = connectedLists.Count == 1
                                          ? connectedLists[0].Clone()
                                          : connectedLists[0].Join(connectedLists.GetRange(1, connectedLists.Count - 1).ToArray());
                    
                    if (sourceItems != null
                        && sourceItems.NumElements > 0
                        && sourceItems is StructuredList<Point> sourcePointSet)
                    {
                        var sourcePoints = sourcePointSet.TypedElements;
                        _sampledPoints = BezierSpline.SamplePointsEvenly(resultCount, ref sourcePoints);
                        _sampledPointsList = new StructuredList<Point>(_sampledPoints);

                        // Upload points
                        var totalSizeInBytes = _sampledPointsList.TotalSizeInBytes;

                        using (var data = new DataStream(totalSizeInBytes, true, true))
                        {
                            _sampledPointsList.WriteToStream(data);
                            data.Position = 0;

                            try
                            {
                                ResourceManager.SetupStructuredBuffer(data, totalSizeInBytes, _sampledPointsList.ElementSizeInBytes, ref _buffer);
                            }
                            catch (Exception e)
                            {
                                Log.Error("Failed to setup structured buffer " + e.Message, this);
                                return;
                            }
                        }

                        ResourceManager.CreateStructuredBufferSrv(_buffer, ref _bufferWithViews.Srv);
                        ResourceManager.CreateStructuredBufferUav(_buffer, UnorderedAccessViewBufferFlags.None, ref _bufferWithViews.Uav);

                        _bufferWithViews.Buffer = _buffer;
                        OutBuffer.Value = _bufferWithViews;
                        SampledPoints.Value = _sampledPointsList;
                    }
                }
                catch (Exception e)
                {
                    Log.Warning("Failed to setup point buffer: " + e.Message, this);
                }
            }
        }

        private Buffer _buffer;
        private readonly BufferWithViews _bufferWithViews = new();
        private Point[] _sampledPoints;
        private StructuredList<Point> _sampledPointsList;

        [Input(Guid = "88AB4088-EFA9-42B7-AFE9-D44A2FF6E58A")]
        public readonly InputSlot<int> SampleCount = new();

        // [Input(Guid = "E031F80A-203C-4863-BCBA-1DE1EEBD34A8")]
        // public readonly InputSlot<float> U = new();
        //
        // [Input(Guid = "B28E5BBC-049D-48AC-A7F2-42D265623AAE")]
        // public readonly InputSlot<float> URange = new();

        [Input(Guid = "02968cef-1a5e-4a7f-b451-b692f4c9b6ab")]
        public readonly MultiInputSlot<StructuredList> Points = new();

        
    }

    public class BezierSpline
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Numerics.Vector3 SampleCubicBezier2(float t, ref Point[] points)
        {
            int i;
            if (t >= 1f)
            {
                t = 1f;
                i = points.Length - 4;
            }
            else
            {
                t = t.Clamp(0, 1) * (points.Length - 1) / 3;
                i = (int)t;
                t -= i;
                i = (i * 3).Clamp(0, points.Length - 4);
            }

            return Bezier.GetPoint(points[i].Position, points[i + 1].Position, points[i + 2].Position, points[i + 3].Position, t);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Numerics.Vector3 SampleCubicBezier(float t, ref Point[] points)
        {
            int i;

            if (t >= 1f)
            {
                t = 1f;
                i = points.Length - 1;
            }
            else
            {
                float tt = t * (points.Length - 1);
                i = (int)tt;
                t = tt - i;
            }
            
            
            var pA = points[i].Position;
            var pB = points[i + 1].Position;

            var pNext = pB;
            var pLast = pA;

            if (i > 0)
            {
                pLast = points[i - 1].Position;
            }

            if (i < points.Length - 2)
            {
                pNext = points[i + 2].Position;
            }

            
            return Bezier.GetPoint(pA,
                                   pA - (pLast - pB) / 4,
                                   pB + (pA - pNext) / 4,
                                   pB,
                                   t);
        }

        private const int PreStepCount = 20;
        private static readonly float[] _lengthList = new float[PreStepCount];

        public static Point[] SamplePoints(int count, ref Point[] sourcePoints)
        {
            count.Clamp(1, 1000);
            var result = new Point[count];
            for (var index = 0; index < count; index++)
            {
                var t = (float)index / count;

                result[index].Position = SampleCubicBezier(t, ref sourcePoints);
                result[index].W = 1;
            }

            return result;
        }

        public static Point[] SamplePointsEvenly(int count, ref Point[] sourcePoints)
        {
            count.Clamp(1, 1000);
            var result = new Point[count];

            // Pre-sample bezier curve for even distribution
            var totalLength = 0f;
            var lastPoint = SampleCubicBezier(0, ref sourcePoints);
            for (var preSampleIndex = 1; preSampleIndex < PreStepCount; preSampleIndex++)
            {
                var t = (float)preSampleIndex / PreStepCount;
                var newPoint = SampleCubicBezier(t, ref sourcePoints);
                var stepLength = Vector3.Distance(newPoint, lastPoint);
                lastPoint = newPoint;
                totalLength += stepLength;
                _lengthList[preSampleIndex] = totalLength;
            }

            var walkedIndex = 0;

            Vector3 lastPos = Vector3.One; 
            for (var index = 0; index < count; index++)
            {
                var wantedLength = totalLength * index / (count-1);

                while (wantedLength > _lengthList[walkedIndex +1] && walkedIndex < PreStepCount - 2)
                {
                    walkedIndex++;
                }

                var l0 = _lengthList[walkedIndex];
                var l1 = _lengthList[walkedIndex + 1];

                var deltaL = (l1 - l0);
                
                var fraction = (wantedLength - l0) / (deltaL + 0.0001f);
                
                var t = (walkedIndex + fraction) / (PreStepCount-1);
                var pos = SampleCubicBezier(t, ref sourcePoints);
                result[index].Position = pos;

                var d = pos - lastPos;
                lastPos = pos;

                result[index].W = 1;
                result[index].Orientation = -LookAt(Vector3.Normalize(d), -Vector3.UnitY);
            }

            return result;
        }

        private static Quaternion LookAt(Vector3 forward, Vector3 up)
        {
            var right = Vector3.Normalize(Vector3.Cross(forward, up));
            up = Vector3.Normalize(Vector3.Cross(forward, right));

            float m00 = right.X;
            float m01 = right.Y;
            float m02 = right.Z;
            float m10 = up.X;
            float m11 = up.Y;
            float m12 = up.Z;
            float m20 = forward.X;
            float m21 = forward.Y;
            float m22 = forward.Z;

            float num8 = (m00 + m11) + m22;
            Quaternion q = Quaternion.Identity;
            if (num8 > 0.0)
            {
                float num = MathF.Sqrt(num8 + 1.0f);
                q.W = num * 0.5f;
                num = 0.5f / num;
                q.X = (m12 - m21) * num;
                q.Y = (m20 - m02) * num;
                q.Z = (m01 - m10) * num;
                return q;
            }

            if ((m00 >= m11) && (m00 >= m22))
            {
                float num7 = MathF.Sqrt(((1.0f + m00) - m11) - m22);
                float num4 = 0.5f / num7;
                q.X = 0.5f * num7;
                q.Y = (m01 + m10) * num4;
                q.Z = (m02 + m20) * num4;
                q.W = (m12 - m21) * num4;
                return q;
            }

            if (m11 > m22)
            {
                float num6 = MathF.Sqrt(((1.0f + m11) - m00) - m22);
                float num3 = 0.5f / num6;
                q.X = (m10 + m01) * num3;
                q.Y = 0.5f * num6;
                q.Z = (m21 + m12) * num3;
                q.W = (m20 - m02) * num3;
                return q;
            }

            float num5 = MathF.Sqrt(((1.0f + m22) - m00) - m11);
            float num2 = 0.5f / num5;
            q.X = (m20 + m02) * num2;
            q.Y = (m21 + m12) * num2;
            q.Z = 0.5f * num5;
            q.W = (m01 - m10) * num2;
            return q;
        }
    }

    public static class Bezier
    {
        public static Vector3 GetPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            t = t.Clamp(0, 1);
            float oneMinusT = 1f - t;
            return
                oneMinusT * oneMinusT * p0 +
                2f * oneMinusT * t * p1 +
                t * t * p2;
        }

        public static Vector3 GetFirstDerivative(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            return
                2f * (1f - t) * (p1 - p0) +
                2f * t * (p2 - p1);
        }

        public static Vector3 GetPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            t = t.Clamp(0, 1);
            float OneMinusT = 1f - t;
            return
                OneMinusT * OneMinusT * OneMinusT * p0 +
                3f * OneMinusT * OneMinusT * t * p1 +
                3f * OneMinusT * t * t * p2 +
                t * t * t * p3;
        }

        public static Vector3 GetFirstDerivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            t = t.Clamp(0, 1);
            float oneMinusT = 1f - t;
            return
                3f * oneMinusT * oneMinusT * (p1 - p0) +
                6f * oneMinusT * t * (p2 - p1) +
                3f * t * t * (p3 - p2);
        }
    }
}