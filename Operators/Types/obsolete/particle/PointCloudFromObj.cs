using System.Runtime.InteropServices;
using SharpDX;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Rendering;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace T3.Operators.Types.Id_73f152ac_12d9_4ae9_856a_9a74637fd6f6
{
    public class PointCloudFromObj : Instance<PointCloudFromObj>
    {
        [Output(Guid = "90c70f78-7b3a-40a0-98a5-650be09871a4")]
        public readonly Slot<SharpDX.Direct3D11.ShaderResourceView> PointCloudSrv = new Slot<SharpDX.Direct3D11.ShaderResourceView>();
        
        public PointCloudFromObj()
        {
            PointCloudSrv.UpdateAction = Update;
        }

        public Buffer Buffer;

        private void Update(EvaluationContext context)
        {
            var resourceManager = ResourceManager.Instance();
            string path = Path.GetValue(context);
            var mesh = ObjMesh.LoadFromFile(path);
            if (mesh == null)
            {
                Log.Warning($"Could not load {path}");
                return;
            }

            float areaSum = 0.0f;
            
            int numVertexEntries = mesh.Faces.Count;
            var faceBufferData = new FaceEntry[numVertexEntries];
                    
            for (int i = 0, faceIndex = 0; faceIndex < mesh.Faces.Count; faceIndex++)
            {
                var face = mesh.Faces[faceIndex];

                // calc area of triangle
                Vector3 v0 = mesh.Positions[face.V0];
                Vector3 v1 = mesh.Positions[face.V1];
                Vector3 v2 = mesh.Positions[face.V2];
                Vector3 baseDir = (v1 - v0);
                float a = baseDir.Length();
                baseDir.Normalize();

                Vector3 heightStart = v0 + Vector3.Dot(v2 - v0, baseDir) * baseDir;
                float b = (v2 - heightStart).Length();
                float faceArea = a * b * 0.5f;
                areaSum += faceArea;

                faceBufferData[i].Pos0 = v0;
                faceBufferData[i].Pos1 = v1;
                faceBufferData[i].Pos2 = v2;
                faceBufferData[i].Normal0 = mesh.Normals[face.V0n];
                faceBufferData[i].Normal1 = mesh.Normals[face.V1n];
                faceBufferData[i].Normal2 = mesh.Normals[face.V2n];
                faceBufferData[i].TexCoord0 = mesh.TexCoords[face.V0t];
                faceBufferData[i].TexCoord1 = mesh.TexCoords[face.V1t];
                faceBufferData[i].TexCoord2 = mesh.TexCoords[face.V2t];
                faceBufferData[i].FaceArea = faceArea;
                i++;
            }            
            
            // normalize face area to 1
            float sumReci = 1.0f / areaSum;
            float cdf = 0.0f;
            for (int i = 0; i < faceBufferData.Length; i++)
            {
                cdf += faceBufferData[i].FaceArea * sumReci;
                faceBufferData[i].Cdf = cdf;
            }

            int stride = 108;
            resourceManager.SetupStructuredBuffer(faceBufferData, stride * numVertexEntries, stride, ref Buffer);
            Buffer.DebugName = nameof(PointCloudFromObj);
            resourceManager.CreateStructuredBufferSrv(Buffer, ref PointCloudSrv.Value);            
        }
        
        
        
        
        
        [StructLayout(LayoutKind.Explicit, Size = 108)]
        private struct FaceEntry
        {
            [FieldOffset(0)]
            public SharpDX.Vector3 Pos0;

            [FieldOffset(12)]
            public SharpDX.Vector3 Pos1;

            [FieldOffset(24)]
            public SharpDX.Vector3 Pos2;

            [FieldOffset(36)]
            public SharpDX.Vector2 TexCoord0;

            [FieldOffset(44)]
            public SharpDX.Vector2 TexCoord1;

            [FieldOffset(52)]
            public SharpDX.Vector2 TexCoord2;

            [FieldOffset(60)]
            public SharpDX.Vector3 Normal0;

            [FieldOffset(72)]
            public SharpDX.Vector3 Normal1;

            [FieldOffset(84)]
            public SharpDX.Vector3 Normal2;

            [FieldOffset(96)]
            public int EmitterId;

            [FieldOffset(100)]
            public float FaceArea;

            [FieldOffset(104)]
            public float Cdf;
        }
        
        [Input(Guid = "af396e7d-bda8-4c64-a109-b3f4c65f940d")]
        public readonly InputSlot<string> Path = new InputSlot<string>();
    }
}