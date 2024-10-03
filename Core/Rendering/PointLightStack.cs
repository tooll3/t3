using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D11;
using T3.Core.Logging;
using T3.Core.Resource;
using Vector3 = System.Numerics.Vector3;

namespace T3.Core.Rendering
{
    public class PointLightStack
    {
        public const int MaxPointLights = 8;

        public Buffer ConstBuffer
        {
            get
            {
                if (_currentSize == 0)
                    return GetDefaultBuffer();

                if (!_isConstBufferDirty)
                    return _constBuffer;
                
                UpdateConstBuffer(ref _pointLights, ref _constBuffer , _currentSize);
                _isConstBufferDirty = false;
                return _constBuffer;
            }
        }

        public void Clear()
        {
            _currentSize = 0;
            _isConstBufferDirty = true;
        }

        public bool Push(LightDefinition pointLight)
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

        private static void UpdateConstBuffer(ref LightDefinition[] pointLights, ref Buffer constBuffer, int activeLightCount)
        {
            const int size = LightDefinition.StructSize * 4 * MaxPointLights;
            using var data = new DataStream(size, true, true);
            foreach (var light in pointLights)
            {
                data.Write(light);
            }
            data.Write(activeLightCount);
            data.Position = 0;

            if (constBuffer == null)
            {
                var bufferDesc = new BufferDescription
                                     {
                                         Usage = ResourceUsage.Default,
                                         SizeInBytes = size,
                                         BindFlags = BindFlags.ConstantBuffer
                                     };
                constBuffer = new Buffer(ResourceManager.Device, data, bufferDesc) { DebugName = "PointLightsConstBuffer" };
            }
            else
            {
                ResourceManager.Device.ImmediateContext.UpdateSubresource(new DataBox(data.DataPointer, 0, 0), constBuffer, 0);
            }
        }

        private Buffer GetDefaultBuffer()
        {
            if (_defaultConstBuffer == null)
            {
                _defaultPointLights[0] = new LightDefinition
                                             {
                                                 Position = new Vector3(8,20,3),
                                                 Intensity = 4, 
                                                 Color = new System.Numerics.Vector4(1,0.93f,0.95f,1),
                                                 Range = 1,
                                                 Decay = 2
                                             };
                _defaultPointLights[1] = new LightDefinition
                                             {
                                                 Position = new Vector3(-8,-20,-8),
                                                 Intensity = 3f, 
                                                 Color = new System.Numerics.Vector4(0.97f,0.96f,1,1),
                                                 Range = 1,
                                                 Decay = 2,
                                             };
                
                UpdateConstBuffer(ref _defaultPointLights, ref _defaultConstBuffer, 2);
            }

            return _defaultConstBuffer;
        }

        public int Count => _currentSize > 0 ? _currentSize : 2; 
        

        public LightDefinition GetPointLight(int index)
        {
            var useDefaultLights = _currentSize == 0;
            
            if (index >= Count)
            {
                Log.Warning($"Requested light index {index} exceed current count of {Count}");
                index = Count - 1;
            }

            return useDefaultLights
                       ? _defaultPointLights[index]
                       : _pointLights[index];
        }
        
        private LightDefinition[] _pointLights = new LightDefinition[MaxPointLights];
        private int _currentSize = 0;
        private bool _isConstBufferDirty = true;
        private Buffer _constBuffer = null;
        
        private static LightDefinition[] _defaultPointLights = new LightDefinition[MaxPointLights];
        private static Buffer _defaultConstBuffer;
    }
}