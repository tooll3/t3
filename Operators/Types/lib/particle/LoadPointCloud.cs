using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using SharpDX;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace T3.Operators.Types.Id_2d09d163_4aa3_4a58_a93c_86dfb7963070
{
    public class LoadPointCloud : Instance<LoadPointCloud>
    {
        [Output(Guid = "0ad7c23b-10ab-4df9-935c-0a7edd96a677")]
        public readonly Slot<SharpDX.Direct3D11.ShaderResourceView> PointCloudSrv = new Slot<SharpDX.Direct3D11.ShaderResourceView>();

        public LoadPointCloud()
        {
            PointCloudSrv.UpdateAction = Update;
        }

        [StructLayout(LayoutKind.Explicit, Size = 32)]
        struct BufferEntry
        {
            [FieldOffset(0)]
            public SharpDX.Vector4 Pos;

            [FieldOffset(16)]
            public SharpDX.Vector4 Color;
        }

        public Buffer Buffer;

        private void Update(EvaluationContext context)
        {
            var resourceManager = ResourceManager.Instance();
            string path = Path.GetValue(context);
            if (string.IsNullOrEmpty(path) || !(new FileInfo(path).Exists))
                return;

            var numEntries = File.ReadLines(path).Count();
            var bufferData = new BufferEntry[numEntries];

            using (var stream = new StreamReader(path))
            {
                try
                {
                    string line;
                    int index = 0;
                    while ((line = stream.ReadLine()) != null)
                    {
                        var values = line.Split(' ');
                        float x = float.Parse(values[0], CultureInfo.InvariantCulture);
                        float y = float.Parse(values[1], CultureInfo.InvariantCulture);
                        float z = float.Parse(values[2], CultureInfo.InvariantCulture);
                        float r = float.Parse(values[3]) / 255.0f;
                        float g = float.Parse(values[4]) / 255.0f;
                        float b = float.Parse(values[5]) / 255.0f;
                        bufferData[index].Pos = new Vector4(x, y, z, 1.0f);
                        bufferData[index].Color = new Vector4(r, g, b, 1.0f);
                        index++;
                    }
                }
                catch(Exception e)
                {
                    Log.Error("Failed to load point cloud:" + e.Message);
                }
            }

            int stride = 32;
            resourceManager.SetupStructuredBuffer(bufferData, stride * numEntries, stride, ref Buffer);
            resourceManager.CreateStructuredBufferSrv(Buffer, ref PointCloudSrv.Value);
        }

        [Input(Guid = "e0479d39-38bb-4802-9a97-3e8991797fc9")]
        public readonly InputSlot<string> Path = new InputSlot<string>();
    }
}