using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D11;
using T3.Core.Logging;
using Vector3 = System.Numerics.Vector3;

namespace T3.Core.DataTypes
{
    [StructLayout(LayoutKind.Explicit, Size = 3 * 16)]
    public struct PointLight
    {
        public PointLight(System.Numerics.Vector3 position, float intensity, System.Numerics.Vector4 color, float range)
        {
            Position = position;
            Intensity = intensity;
            Color = color;
            Range = range;
        }

        [FieldOffset(0)]
        public System.Numerics.Vector3 Position;

        [FieldOffset(12)]
        public float Intensity;

        [FieldOffset(16)]
        public System.Numerics.Vector4 Color;

        [FieldOffset(32)]
        public float Range;
    }

    public class PointLightStack
    {
        public const int MaxPointLights = 8;

        public Buffer ConstBuffer
        {
            get
            {
                if (_isConstBufferDirty)
                {
                    UpdateConstBuffer();
                }

                return _constBuffer;
            }
        }

        public void Clear()
        {
            _currentSize = 0;
            _isConstBufferDirty = true;
        }

        public bool Push(PointLight pointLight)
        {
            if (_currentSize == MaxPointLights)
            {
                Log.Warning($"Trying to push a new point light, but limit {MaxPointLights} reached");
                return false;
            }

            _pointLights[_currentSize++] = pointLight;
            _isConstBufferDirty = true;

            return true;
        }

        public void Pop()
        {
            if (_currentSize > 0)
            {
                _currentSize--;
                _isConstBufferDirty = true;
            }
        }

        private void UpdateConstBuffer()
        {
            var size = Marshal.SizeOf<PointLight>() * MaxPointLights + 16;
            using (var data = new DataStream(size, true, true))
            {
                foreach (var light in _pointLights)
                {
                    data.Write(light);
                }
                data.Write(_currentSize);
                data.Position = 0;

                if (_constBuffer == null)
                {
                    var bufferDesc = new BufferDescription
                                         {
                                             Usage = ResourceUsage.Default,
                                             SizeInBytes = size,
                                             BindFlags = BindFlags.ConstantBuffer
                                         };
                    _constBuffer = new Buffer(ResourceManager.Instance().Device, data, bufferDesc) { DebugName = "PointLightsConstBuffer" };
                }
                else
                {
                    ResourceManager.Instance().Device.ImmediateContext.UpdateSubresource(new DataBox(data.DataPointer, 0, 0), _constBuffer, 0);
                }
            }

            _isConstBufferDirty = false;
        }

        private readonly PointLight[] _pointLights = new PointLight[MaxPointLights];
        private int _currentSize = 0;
        private bool _isConstBufferDirty = true;
        private Buffer _constBuffer = null;
    }
}