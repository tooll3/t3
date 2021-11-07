using System;
using SharpDX;
using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Rendering;
using Buffer = SharpDX.Direct3D11.Buffer;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

namespace T3.Operators.Types.Id_5777a005_bbae_48d6_b633_5e998ca76c91
{
    public class CylinderMesh : Instance<CylinderMesh>
    {
        [Output(Guid = "b4bed6e3-bef5-4601-99bd-f85bf1a956f5")]
        public readonly Slot<MeshBuffers> Data = new Slot<MeshBuffers>();

        public CylinderMesh()
        {
            Data.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            try
            {
                var resourceManager = ResourceManager.Instance();

                var lowerRadius = Radius.GetValue(context);
                var upperRadius = lowerRadius + RadiusOffset.GetValue(context);
                var height = Height.GetValue(context);

                var rows = Rows.GetValue(context).Clamp(1, 10000);
                var columns = Columns.GetValue(context).Clamp(1, 10000);

                var c = Center.GetValue(context);
                var center = new SharpDX.Vector3(c.X, c.Y, c.Z);

                var spinInRad = Spin.GetValue(context) * MathUtils.ToRad;
                var twistInRad = Twist.GetValue(context) * MathUtils.ToRad;
                var basePivot = BasePivot.GetValue(context);

                var fillRatio = Fill.GetValue(context) / 360f;
                var capSegments = CapSegments.GetValue(context).Clamp(0, 1000);
                var addCaps = capSegments > 0;
                //var isHullClosed = true; //Math.Abs(fillRatio - 1) < 0.01f;

                var isFlipped = lowerRadius < 0;

                var vertexHullColumns = columns + 1;

                var hullVerticesCount = (rows + 1) * vertexHullColumns;
                var hullTriangleCount = rows * columns * 2;

                var capsVertexCount = +2 * (capSegments * vertexHullColumns + 1);
                var capsTriangleCount = 2 * ((capSegments - 1) * columns * 2 + columns);

                var totalVertexCount = hullVerticesCount + (addCaps ? capsVertexCount : 0);
                var totalTriangleCount = hullTriangleCount + (addCaps ? capsTriangleCount : 0);

                // Create buffers
                if (_vertexBufferData.Length != totalVertexCount)
                    _vertexBufferData = new PbrVertex[totalVertexCount];

                if (_indexBufferData.Length != totalTriangleCount)
                    _indexBufferData = new SharpDX.Int3[totalTriangleCount];

                // Initialize
                var radiusAngleFraction = fillRatio / (vertexHullColumns - 1) * 2.0 * Math.PI;
                var rowStep = height / rows;

                // Hull
                for (var rowIndex = 0; rowIndex < rows + 1; ++rowIndex)
                {
                    var heightFraction = rowIndex / (float)rows;
                    var rowRadius = MathUtils.Lerp(lowerRadius, upperRadius, heightFraction);
                    var nextRowRadius = MathUtils.Lerp(lowerRadius, upperRadius, (rowIndex + 1) / (float)rows);
                    var rowLevel = height * (heightFraction - basePivot);
                    var rowCenter = new SharpDX.Vector3(0, rowLevel, 0);

                    for (var columnIndex = 0; columnIndex < vertexHullColumns; ++columnIndex)
                    {
                        var columnAngle = columnIndex * radiusAngleFraction + spinInRad + twistInRad * heightFraction + Math.PI;

                        var u0 = columnIndex / (float)columns;
                        var u1 = (columnIndex + 1) / (float)rows;

                        var v0 = addCaps ? ((rowIndex + 1) / (float)rows)/2f
                                         : (rowIndex + 1) / (float)rows;
                        var v1 = addCaps ? (rowIndex / (float)rows) / 2f
                                        : rowIndex / (float)rows;

                        var p = new SharpDX.Vector3((float)Math.Sin(columnAngle) * rowRadius,
                                                    rowLevel,
                                                    (float)Math.Cos(columnAngle) * rowRadius);

                        var p1 = new SharpDX.Vector3((float)Math.Sin(columnAngle) * rowRadius,
                                                     rowLevel + rowStep,
                                                     (float)Math.Cos(columnAngle) * rowRadius
                                                    );

                        var p2 = new SharpDX.Vector3((float)Math.Sin(columnAngle + radiusAngleFraction) * nextRowRadius,
                                                     rowLevel,
                                                     (float)Math.Cos(columnAngle + radiusAngleFraction) * nextRowRadius
                                                    );

                        var uv0 = new SharpDX.Vector2(u0, v1);
                        var uv1 = new SharpDX.Vector2(u1, v1);
                        var uv2 = new SharpDX.Vector2(u1, v0);

                        var normal0 = SharpDX.Vector3.Normalize(p - rowCenter);

                        MeshUtils.CalcTBNSpace(p, uv0, p1, uv1, p2, uv2, normal0, out var tangent0, out var binormal0);

                        var vertexIndex = rowIndex * vertexHullColumns + columnIndex;
                        _vertexBufferData[vertexIndex] = new PbrVertex
                                                             {
                                                                 Position = p + center,
                                                                 Normal = isFlipped ? normal0 * -1 : normal0,
                                                                 Tangent = tangent0,
                                                                 Bitangent = isFlipped ? binormal0 * -1 : binormal0,
                                                                 Texcoord = uv0,
                                                                 Selection = 1
                                                             };

                        var faceIndex = 2 * (rowIndex * (vertexHullColumns - 1) + columnIndex);
                        if (columnIndex < vertexHullColumns - 1 && rowIndex < rows)
                        {
                            if (isFlipped)
                            {
                                _indexBufferData[faceIndex + 0] = new SharpDX.Int3(vertexIndex + vertexHullColumns, vertexIndex + 1, vertexIndex + 0);
                                _indexBufferData[faceIndex + 1] =
                                    new SharpDX.Int3(vertexIndex + vertexHullColumns + 1, vertexIndex + 1, vertexIndex + vertexHullColumns);
                            }
                            else
                            {
                                _indexBufferData[faceIndex + 0] = new SharpDX.Int3(vertexIndex + 0, vertexIndex + 1, vertexIndex + vertexHullColumns);
                                _indexBufferData[faceIndex + 1] =
                                    new SharpDX.Int3(vertexIndex + vertexHullColumns, vertexIndex + 1, vertexIndex + vertexHullColumns + 1);
                            }
                        }
                    }
                }

                // Caps
                if (addCaps)
                {
                    for (var capIndex = 0; capIndex <= 1; capIndex++)
                    {
                        var isLowerCap = capIndex == 0;
                        var capLevel = ((isLowerCap ? 0 : 1) - basePivot) * height;
                        var capRadius = isLowerCap ? lowerRadius : upperRadius;
                        var isReverse = isFlipped ^ isLowerCap;

                        var centerVertexIndex = hullVerticesCount + (capsVertexCount / 2) * (capIndex + 1) - 1;

                        for (var capSegmentIndex = 0; capSegmentIndex < capSegments; ++capSegmentIndex)
                        {
                            var isCenterSegment = capSegmentIndex == capSegments - 1;

                            var capFraction = 1f - (capSegmentIndex) / (float)capSegments;
                            var nextCapFraction = 1f - (capSegmentIndex + 1) / (float)capSegments;
                            var radius = upperRadius * capFraction;
                            var nextRadius = upperRadius * nextCapFraction;

                            
                            for (var columnIndex = 0; columnIndex < vertexHullColumns; ++columnIndex)
                            {
                                var columnAngle = columnIndex * radiusAngleFraction + spinInRad + twistInRad * (isLowerCap ? 0 : 1) + Math.PI;

                                var xx = (float)Math.Sin(columnAngle);
                                var yy = (float)Math.Cos(columnAngle);
                                
                                var p = new SharpDX.Vector3(-xx * radius,
                                                            capLevel,
                                                            -yy * radius);

                                var normal0 = isLowerCap ? SharpDX.Vector3.Down : SharpDX.Vector3.Up;

                                var tangent0 = SharpDX.Vector3.Left;
                                var binormal0 = SharpDX.Vector3.ForwardRH;

                                var vertexIndex = capSegmentIndex * vertexHullColumns + columnIndex
                                                                                      + hullVerticesCount + (capsVertexCount / 2) * capIndex;

                                // Write vertex
                                var capUvOffset = isLowerCap 
                                                      ? new SharpDX.Vector2(-0.25f, -0.25f)
                                                      : new SharpDX.Vector2(0.25f, -0.25f);
                                _vertexBufferData[vertexIndex] = new PbrVertex
                                                                     {
                                                                         Position = p + center,
                                                                         Normal = isFlipped ? normal0 * -1 : normal0,
                                                                         Tangent = tangent0,
                                                                         Bitangent = isFlipped ? binormal0 * -1 : binormal0,
                                                                         Texcoord = new SharpDX.Vector2(-xx * (isLowerCap ? -1 : 1),yy) * capFraction/4 + capUvOffset,
                                                                         Selection = 1
                                                                     };

                                if (isCenterSegment)
                                {
                                    if (columnIndex == 0)
                                    {
                                        _vertexBufferData[centerVertexIndex] = new PbrVertex
                                                                                   {
                                                                                       Position = new SharpDX.Vector3(0, capLevel, 0) + center,
                                                                                       Normal = (isFlipped) ? normal0 * -1 : normal0,
                                                                                       Tangent = tangent0,
                                                                                       Bitangent = (isFlipped ^ isLowerCap) ? binormal0 * -1 : binormal0,
                                                                                       Texcoord =  capUvOffset,
                                                                                       Selection = 1
                                                                                   };
                                    }
                                }

                                if (columnIndex < vertexHullColumns - 1 && capSegmentIndex < capSegments)
                                {
                                    if (isCenterSegment)
                                    {
                                        var faceIndex = (capSegmentIndex * (vertexHullColumns - 1) * 2 + columnIndex)
                                                        + hullTriangleCount + (capsTriangleCount / 2) * capIndex;

                                        var f1 = isReverse
                                                     ? new SharpDX.Int3(vertexIndex + 1, vertexIndex, centerVertexIndex)
                                                     : new SharpDX.Int3(centerVertexIndex, vertexIndex, vertexIndex + 1);
                                        _indexBufferData[faceIndex] = f1;
                                    }
                                    else
                                    {
                                        var faceIndex = (capSegmentIndex * (vertexHullColumns - 1) * 2) + columnIndex * 2
                                                                                                        + hullTriangleCount +
                                                                                                        (capsTriangleCount / 2) * capIndex;

                                        _indexBufferData[faceIndex]
                                            = isReverse
                                                  ? new SharpDX.Int3(vertexIndex + vertexHullColumns, vertexIndex + 1, vertexIndex)
                                                  : new SharpDX.Int3(vertexIndex, vertexIndex + 1, vertexIndex + vertexHullColumns);

                                        _indexBufferData[faceIndex + 1]
                                            = isReverse
                                                  ? new SharpDX.Int3(vertexIndex + vertexHullColumns, vertexIndex + vertexHullColumns + 1, vertexIndex + 1)
                                                  : new SharpDX.Int3(vertexIndex + 1, vertexIndex + vertexHullColumns + 1, vertexIndex + vertexHullColumns);
                                    }
                                }
                            }
                        }
                    }
                }

                // Write Data
                _vertexBufferWithViews.Buffer = _vertexBuffer;
                resourceManager.SetupStructuredBuffer(_vertexBufferData, PbrVertex.Stride * totalVertexCount, PbrVertex.Stride, ref _vertexBuffer);
                resourceManager.CreateStructuredBufferSrv(_vertexBuffer, ref _vertexBufferWithViews.Srv);
                resourceManager.CreateStructuredBufferUav(_vertexBuffer, UnorderedAccessViewBufferFlags.None, ref _vertexBufferWithViews.Uav);

                _indexBufferWithViews.Buffer = _indexBuffer;
                const int stride = 3 * 4;
                resourceManager.SetupStructuredBuffer(_indexBufferData, stride * totalTriangleCount, stride, ref _indexBuffer);
                resourceManager.CreateStructuredBufferSrv(_indexBuffer, ref _indexBufferWithViews.Srv);
                resourceManager.CreateStructuredBufferUav(_indexBuffer, UnorderedAccessViewBufferFlags.None, ref _indexBufferWithViews.Uav);

                _data.VertexBuffer = _vertexBufferWithViews;
                _data.IndicesBuffer = _indexBufferWithViews;
                Data.Value = _data;
                Data.DirtyFlag.Clear();
            }
            catch (Exception e)
            {
                Log.Error("Failed to create torus mesh:" + e.Message);
            }
        }

        private Buffer _vertexBuffer;
        private PbrVertex[] _vertexBufferData = new PbrVertex[0];
        private readonly BufferWithViews _vertexBufferWithViews = new BufferWithViews();

        private Buffer _indexBuffer;
        private SharpDX.Int3[] _indexBufferData = new SharpDX.Int3[0];
        private readonly BufferWithViews _indexBufferWithViews = new BufferWithViews();

        private readonly MeshBuffers _data = new MeshBuffers();

        [Input(Guid = "66332A91-E0C2-442A-99F6-347DEDAED72E")]
        public readonly InputSlot<Vector3> Center = new InputSlot<Vector3>();

        [Input(Guid = "8d290afb-2574-4afa-a545-a0d3588f89f6")]
        public readonly InputSlot<float> Radius = new InputSlot<float>();

        [Input(Guid = "4C91B66C-670D-45FF-94CC-01D1A68CD040")]
        public readonly InputSlot<float> RadiusOffset = new InputSlot<float>();

        [Input(Guid = "57f3310c-6ed2-4a52-af72-43e083f73360")]
        public readonly InputSlot<float> Height = new InputSlot<float>();

        [Input(Guid = "4DD4C4F0-C6B7-4EE8-92E2-CB8DF6131E0A")]
        public readonly InputSlot<int> Rows = new InputSlot<int>();

        [Input(Guid = "321693A5-4E2C-47A0-A42E-95CBDC6EBF80")]
        public readonly InputSlot<int> Columns = new InputSlot<int>();

        [Input(Guid = "C29B5881-85BC-4D29-BC72-6DD36730FA8F")]
        public readonly InputSlot<float> Spin = new InputSlot<float>();

        [Input(Guid = "1D1CE8C4-FD3C-4D69-BE0E-679247A811C9")]
        public readonly InputSlot<float> Twist = new InputSlot<float>();

        [Input(Guid = "91FD4FBF-1CEC-4D89-8014-CEED0021A5EE")]
        public readonly InputSlot<float> Fill = new InputSlot<float>();

        [Input(Guid = "6DDF5966-9140-4BEA-A56B-20690F9F436F")]
        public readonly InputSlot<float> BasePivot = new InputSlot<float>();

        [Input(Guid = "DB5E3C51-5765-44D8-A61B-A7B552FCE5B3")]
        public readonly InputSlot<int> CapSegments = new InputSlot<int>();
    }
}