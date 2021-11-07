using System;
using SharpDX;
using SharpDX.Direct2D1;
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

namespace T3.Operators.Types.Id_c47ab830_aae7_4f8f_b67c_9119bcbaf7df
{
    public class CubeMesh : Instance<CubeMesh>
    {
        [Output(Guid = "35660e2b-5005-44a2-bf57-db9a3f1b791d")]
        public readonly Slot<MeshBuffers> Data = new Slot<MeshBuffers>();

        public CubeMesh()
        {
            Data.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            try
            {
                var resourceManager = ResourceManager.Instance();

                var scale = Scale.GetValue(context);
                var stretch = Stretch.GetValue(context);
                var stretchDX = new SharpDX.Vector3(stretch.X, stretch.Y, stretch.Z);
                var pivot = Pivot.GetValue(context);
                var pivotDX = new SharpDX.Vector3(pivot.X, pivot.Y, pivot.Z);
                var rotation = Rotation.GetValue(context);
                var cubeRotationMatrix = Matrix.RotationYawPitchRoll(MathUtil.DegreesToRadians(rotation.Y),
                                                                     MathUtil.DegreesToRadians(rotation.X),
                                                                     MathUtil.DegreesToRadians(rotation.Z));

                var center = Center.GetValue(context);
                // var offset = new SharpDX.Vector3(stretch.X * scale * (pivot.X - 0.5f),
                //                                  stretch.Y * scale * (pivot.Y - 0.5f),
                //                                  stretch.Z * scale * (pivot.Z - 0.5f));

                var offset = -SharpDX.Vector3.One * 0.5f;

                var center2 = new SharpDX.Vector3(center.X, center.Y, center.Z);

                var segments = Segments.GetValue(context);
                _xSegments = segments.X.Clamp(1, 10000) + 1;
                _ySegments = segments.Y.Clamp(1, 10000) + 1;
                _zSegments = segments.Z.Clamp(1, 10000) + 1;

                var faceCount = (_ySegments - 1) * (_xSegments - 1) * 2 * 2 // front / back
                                + (_ySegments - 1) * (_zSegments - 1) * 2 * 2 // top / bottom
                                + (_xSegments - 1) * (_zSegments - 1) * 2 * 2; // left / right

                var verticesCount = (_ySegments * _xSegments + _ySegments * _zSegments + _xSegments * _zSegments) * 2;

                // Create buffers
                if (_vertexBufferData.Length != verticesCount)
                    _vertexBufferData = new PbrVertex[verticesCount];

                if (_indexBufferData.Length != faceCount)
                    _indexBufferData = new SharpDX.Int3[faceCount];

                int sideFaceIndex = 0;
                int sideVertexIndex = 0;

                for (var sideIndex = 0; sideIndex < _sides.Length; sideIndex++)
                {
                    var side = _sides[sideIndex];

                    var sideRotationMatrix = Matrix.RotationYawPitchRoll(side.SideRotation.Y,
                                                                         side.SideRotation.X,
                                                                         side.SideRotation.Z);

                    var rotationMatrix = Matrix.Multiply(sideRotationMatrix, cubeRotationMatrix);

                    var columnCount = GetSegmentCountForAxis(side.ColumnAxis);
                    var rowCount = GetSegmentCountForAxis(side.RowAxis);
                    var columnStretch = GetComponentForAxis(side.ColumnAxis, stretch);
                    var rowStretch = GetComponentForAxis(side.RowAxis, stretch);
                    var depthStretch = GetComponentForAxis(side.DepthAxis, stretch);

                    double columnStep = 1.0 / (columnCount - 1);
                    double rowStep = 1.0 / (rowCount - 1);
                    float depthScale = 1f;

                    var normal = SharpDX.Vector3.TransformNormal(SharpDX.Vector3.ForwardLH, rotationMatrix);
                    var tangent = SharpDX.Vector3.TransformNormal(SharpDX.Vector3.Right, rotationMatrix);
                    var binormal = SharpDX.Vector3.TransformNormal(SharpDX.Vector3.Up, rotationMatrix);

                    // Initialize
                    for (int columnIndex = 0; columnIndex < columnCount; ++columnIndex)
                    {
                        var columnFragment = (float)(columnIndex * columnStep);

                        var u0 = columnIndex / ((float)columnCount - 1);

                        for (int rowIndex = 0; rowIndex < rowCount; ++rowIndex)
                        {
                            var rowFragment = (float)(rowIndex * rowStep);
                            var vertexIndex = rowIndex + columnIndex * rowCount + sideVertexIndex;
                            var faceIndex = 2 * (rowIndex + columnIndex * (rowCount - 1)) + sideFaceIndex;

                            var p = new SharpDX.Vector3(columnFragment,
                                                        rowFragment,
                                                        depthScale);

                            var v0 = (rowIndex) / ((float)rowCount - 1);
                            var uv0 = new SharpDX.Vector2(u0, v0);
                            var position = (SharpDX.Vector3.TransformNormal(p + offset, sideRotationMatrix) + pivotDX) * stretchDX * scale;
                            position = SharpDX.Vector3.TransformNormal(position, cubeRotationMatrix);

                            _vertexBufferData[vertexIndex + 0] = new PbrVertex
                                                                     {
                                                                         Position = position + center2,
                                                                         Normal = normal,
                                                                         Tangent = tangent,
                                                                         Bitangent = binormal,
                                                                         Texcoord = uv0,
                                                                         Selection = 1,
                                                                     };

                            if (columnIndex >= columnCount - 1 || rowIndex >= rowCount - 1)
                                continue;

                            _indexBufferData[faceIndex + 0] = new SharpDX.Int3(vertexIndex, vertexIndex + rowCount, vertexIndex + 1);
                            _indexBufferData[faceIndex + 1] = new SharpDX.Int3(vertexIndex + rowCount, vertexIndex + rowCount + 1, vertexIndex + 1);
                        }
                    }

                    sideVertexIndex += columnCount * rowCount;
                    sideFaceIndex += (columnCount - 1) * (rowCount - 1) * 2;
                }

                // Write Data
                _vertexBufferWithViews.Buffer = _vertexBuffer;
                resourceManager.SetupStructuredBuffer(_vertexBufferData, PbrVertex.Stride * verticesCount, PbrVertex.Stride, ref _vertexBuffer);
                resourceManager.CreateStructuredBufferSrv(_vertexBuffer, ref _vertexBufferWithViews.Srv);
                resourceManager.CreateStructuredBufferUav(_vertexBuffer, UnorderedAccessViewBufferFlags.None, ref _vertexBufferWithViews.Uav);

                _indexBufferWithViews.Buffer = _indexBuffer;
                const int stride = 3 * 4;
                resourceManager.SetupStructuredBuffer(_indexBufferData, stride * faceCount, stride, ref _indexBuffer);
                resourceManager.CreateStructuredBufferSrv(_indexBuffer, ref _indexBufferWithViews.Srv);
                resourceManager.CreateStructuredBufferUav(_indexBuffer, UnorderedAccessViewBufferFlags.None, ref _indexBufferWithViews.Uav);

                _data.VertexBuffer = _vertexBufferWithViews;
                _data.IndicesBuffer = _indexBufferWithViews;
                Data.Value = _data;
                Data.DirtyFlag.Clear();
            }
            catch (Exception e)
            {
                Log.Error("Failed to create cube mesh:" + e.Message);
            }
        }

        private int GetSegmentCountForAxis(SegmentAxis axis)
        {
            return axis switch
                       {
                           SegmentAxis.X => _xSegments,
                           SegmentAxis.Y => _ySegments,
                           _             => _zSegments
                       };
        }

        private float GetComponentForAxis(SegmentAxis axis, Vector3 vector)
        {
            return axis switch
                       {
                           SegmentAxis.X => vector.X,
                           SegmentAxis.Y => vector.Y,
                           _             => vector.Z
                       };
        }

        private enum SegmentAxis
        {
            X,
            Y,
            Z,
        }

        private int _xSegments;
        private int _ySegments;
        private int _zSegments;

        private struct Side
        {
            public SharpDX.Vector3 Normal;
            public SharpDX.Vector3 Tangent;
            public SharpDX.Vector3 Binormal;
            public Vector2 UvScale;
            public Vector2 UvOffset;
            public SegmentAxis ColumnAxis;
            public SegmentAxis RowAxis;
            public SegmentAxis DepthAxis;
            public SharpDX.Vector3 SideRotation;
        }

        static private Side[] _sides =
            {
                // Front
                new Side
                    {
                        Normal = SharpDX.Vector3.ForwardLH,
                        Tangent = SharpDX.Vector3.Right,
                        Binormal = SharpDX.Vector3.Up,
                        UvScale = Vector2.One,
                        UvOffset = Vector2.Zero,
                        ColumnAxis = SegmentAxis.X,
                        RowAxis = SegmentAxis.Y,
                        DepthAxis = SegmentAxis.Z,
                        SideRotation = new SharpDX.Vector3(0, 0, 0),
                    },
                // Right
                new Side
                    {
                        Normal = default,
                        Tangent = default,
                        Binormal = default,
                        UvScale = Vector2.One,
                        UvOffset = Vector2.Zero,
                        ColumnAxis = SegmentAxis.Z,
                        RowAxis = SegmentAxis.Y,
                        DepthAxis = SegmentAxis.X,
                        SideRotation = new SharpDX.Vector3(0, (float)(Math.PI * 0.5), 0),
                    },
                // Back
                new Side
                    {
                        Normal = -SharpDX.Vector3.ForwardLH,
                        Tangent = -SharpDX.Vector3.Right,
                        Binormal = -SharpDX.Vector3.Up,
                        UvScale = default,
                        UvOffset = default,
                        ColumnAxis = SegmentAxis.X,
                        RowAxis = SegmentAxis.Y,
                        DepthAxis = SegmentAxis.Z,
                        SideRotation = new SharpDX.Vector3(0, (float)Math.PI, 0),
                    },
                // Left
                new Side
                    {
                        Normal = default,
                        Tangent = default,
                        Binormal = default,
                        UvScale = Vector2.One,
                        UvOffset = Vector2.Zero,
                        ColumnAxis = SegmentAxis.Z,
                        RowAxis = SegmentAxis.Y,
                        DepthAxis = SegmentAxis.X,
                        SideRotation = new SharpDX.Vector3(0, (float)(Math.PI * 1.5), 0),
                    },
                // Top
                new Side
                    {
                        Normal = default,
                        Tangent = default,
                        Binormal = default,
                        UvScale = Vector2.One,
                        UvOffset = Vector2.Zero,
                        ColumnAxis = SegmentAxis.X,
                        RowAxis = SegmentAxis.Z,
                        DepthAxis = SegmentAxis.Y,
                        SideRotation = new SharpDX.Vector3((float)(Math.PI * 0.5), 0, 0),
                    },
                // Bottom
                new Side
                    {
                        Normal = default,
                        Tangent = default,
                        Binormal = default,
                        UvScale = Vector2.One,
                        UvOffset = Vector2.Zero,
                        ColumnAxis = SegmentAxis.X,
                        RowAxis = SegmentAxis.Z,
                        DepthAxis = SegmentAxis.Y,
                        SideRotation = new SharpDX.Vector3((float)(Math.PI * 1.5), 0, 0),
                    },
            };

        private Buffer _vertexBuffer;
        private PbrVertex[] _vertexBufferData = new PbrVertex[0];
        private readonly BufferWithViews _vertexBufferWithViews = new BufferWithViews();

        private Buffer _indexBuffer;
        private SharpDX.Int3[] _indexBufferData = new SharpDX.Int3[0];
        private readonly BufferWithViews _indexBufferWithViews = new BufferWithViews();

        private readonly MeshBuffers _data = new MeshBuffers();

        [Input(Guid = "E445A6DA-0B66-46AE-AD2B-650E9CC50798")]
        public readonly InputSlot<Int3> Segments = new InputSlot<Int3>();

        [Input(Guid = "97C9849E-751C-49A9-823D-0AF839FA503E")]
        public readonly InputSlot<Vector3> Stretch = new InputSlot<Vector3>();

        [Input(Guid = "9a7d34a1-ca39-48bc-b977-9a786d23f3b1")]
        public readonly InputSlot<float> Scale = new InputSlot<float>();

        [Input(Guid = "FEBFAE90-13E8-4F0A-8CCF-B8825EA525F8")]
        public readonly InputSlot<Vector3> Pivot = new InputSlot<Vector3>();

        [Input(Guid = "f4a78f77-8d8c-4b7b-8545-ea80947b428d")]
        public readonly InputSlot<Vector3> Center = new InputSlot<Vector3>();

        [Input(Guid = "e641c244-9dc8-444d-8dee-c3e9b710f9db")]
        public readonly InputSlot<Vector3> Rotation = new InputSlot<System.Numerics.Vector3>();
    }
}