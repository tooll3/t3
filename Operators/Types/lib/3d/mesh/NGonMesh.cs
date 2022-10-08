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

namespace T3.Operators.Types.Id_1b9977be_70cf_4dbd_8af1_1459596b6527
{
    public class NGonMesh : Instance<NGonMesh>
    {
        [Output(Guid = "9c949c29-9dc1-4ded-94ce-1e86317a5233")]
        public readonly Slot<MeshBuffers> Data = new ();

        public enum TextureModes
        {
            Planar,
            Circular, 
            CircularScaled
        }

        public NGonMesh()
        {
            Data.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            try
            {
                var resourceManager = ResourceManager.Instance();

                var radius = Radius.GetValue(context);
                var stretch = Stretch.GetValue(context);
                var rotation = Rotation.GetValue(context);
                
                var yaw = MathUtil.DegreesToRadians(rotation.Y);
                var pitch = MathUtil.DegreesToRadians(rotation.X);
                var roll = MathUtil.DegreesToRadians(rotation.Z);

                var textureMode = (TextureModes)(int)TextureMode.GetValue(context).Clamp(0, Enum.GetValues(typeof(TextureModes)).Length);


                var rotationMatrix = Matrix.RotationYawPitchRoll(yaw, pitch, roll);
                
                var center = Center.GetValue(context);

                var center2 = new SharpDX.Vector3(center.X, center.Y, center.Z);
                var segments = Segments.GetValue(context).Clamp(1, 10000);
                var verticesCount = segments + 1; // +1 for center

                // Create buffers
                if (_vertexBufferData.Length != verticesCount)
                    _vertexBufferData = new PbrVertex[verticesCount];
                
                if (_indexBufferData.Length != segments)
                    _indexBufferData = new SharpDX.Int3[segments];

                var normal = SharpDX.Vector3.TransformNormal(SharpDX.Vector3.ForwardLH, rotationMatrix);
                var tangent = SharpDX.Vector3.TransformNormal(SharpDX.Vector3.Right, rotationMatrix);
                var binormal = SharpDX.Vector3.TransformNormal(SharpDX.Vector3.Up, rotationMatrix);

                // center
                _vertexBufferData[0] = new PbrVertex
                {
                    Position = center2,
                    Normal = normal,
                    Tangent = tangent,
                    Bitangent = binormal,
                    Texcoord = new SharpDX.Vector2(0, 0),
                    Selection = 1,
                };

                // the other points are on a circle
                for (var segmentIndex = 0; segmentIndex < segments; ++segmentIndex)
                {
                    var phi = 2 * MathF.PI * segmentIndex / segments;
                    var p = new SharpDX.Vector3(radius * MathF.Sin(phi) * stretch.X, // starts at top
                                                radius * MathF.Cos(phi) * stretch.Y,
                                                0);
                    float u0=0f, v0=0f;

                    switch (textureMode) {
                    case TextureModes.Planar:
                        u0 = MathF.Sin(phi);
                        v0 = MathF.Cos(phi);
                        break;
                    case TextureModes.Circular:
                        u0 = phi / (2 * MathF.PI);
                        v0 = 1;
                        break;
                    case TextureModes.CircularScaled:
                        u0 = phi / (2 * MathF.PI);
                        v0 = radius;
                        break;
                    }
                    var uv0 = new SharpDX.Vector2(u0, v0);
                    _vertexBufferData[segmentIndex+1] = new PbrVertex {
                                                                     Position = SharpDX.Vector3.TransformNormal(p, rotationMatrix) + center2,
                                                                     Normal = normal,
                                                                     Tangent = tangent,
                                                                     Bitangent = binormal,
                                                                     Texcoord = uv0,
                                                                     Selection = 1,
                                                                };
                    _indexBufferData[segmentIndex] = new SharpDX.Int3(0,
                                                                      (segmentIndex + 2) > segments ? 1 : segmentIndex + 2,
                                                                      segmentIndex + 1);
                }
                                
                // Write Data
                _vertexBufferWithViews.Buffer = _vertexBuffer;
                ResourceManager.SetupStructuredBuffer(_vertexBufferData, PbrVertex.Stride * verticesCount, PbrVertex.Stride, ref _vertexBuffer);
                ResourceManager.CreateStructuredBufferSrv(_vertexBuffer, ref _vertexBufferWithViews.Srv);
                ResourceManager.CreateStructuredBufferUav(_vertexBuffer, UnorderedAccessViewBufferFlags.None, ref _vertexBufferWithViews.Uav);
                
                _indexBufferWithViews.Buffer = _indexBuffer;
                const int stride = 3 * 4;
                ResourceManager.SetupStructuredBuffer(_indexBufferData, stride * segments, stride, ref _indexBuffer);
                ResourceManager.CreateStructuredBufferSrv(_indexBuffer, ref _indexBufferWithViews.Srv);
                ResourceManager.CreateStructuredBufferUav(_indexBuffer, UnorderedAccessViewBufferFlags.None, ref _indexBufferWithViews.Uav);

                _data.VertexBuffer = _vertexBufferWithViews;
                _data.IndicesBuffer = _indexBufferWithViews;
                Data.Value = _data;
                Data.DirtyFlag.Clear();
            }
            catch (Exception e)
            {
                Log.Error("Failed to create mesh:" + e.Message);
            }
        }

        private Buffer _vertexBuffer;
        private PbrVertex[] _vertexBufferData = new PbrVertex[0];
        private readonly BufferWithViews _vertexBufferWithViews = new();

        private Buffer _indexBuffer;
        private SharpDX.Int3[] _indexBufferData = new SharpDX.Int3[0];
        private readonly BufferWithViews _indexBufferWithViews = new();

        private readonly MeshBuffers _data = new();

        [Input(Guid = "deee0efc-949e-41da-bdb1-d80dbb6ac6e2")]
        public readonly InputSlot<System.Numerics.Vector2> Stretch = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "69a2e8c2-2c88-4969-8beb-66fe8ff4af18")]
        public readonly InputSlot<System.Numerics.Vector3> Center = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "b819ad07-6229-4b8d-b8b6-a2a89b7c81d8")]
        public readonly InputSlot<System.Numerics.Vector3> Rotation = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "33921c65-61bc-4229-af8c-c89db9a874bf")]
        public readonly InputSlot<int> Segments = new InputSlot<int>();

        [Input(Guid = "9dbf0c3d-4762-41f6-94b8-26acbd1531c1")]
        public readonly InputSlot<float> Radius = new InputSlot<float>();

        [Input(Guid = "d85761fb-3c82-4785-a2a2-4b111230e4ee", MappedType = typeof(NGonMesh.TextureModes))]
        public readonly InputSlot<int> TextureMode = new InputSlot<int>();
        
    }
}