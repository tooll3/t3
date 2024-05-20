using System.Runtime.InteropServices;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace Lib.point.combine
{
    public class UVsViewer : Instance<UVsViewer>
    {

        [Output(Guid = "6ac1d050-592c-4533-9b5e-c9e62884c992")]
        public readonly Slot<T3.Core.DataTypes.MeshBuffers> BlendedMesh = new();

        [Input(Guid = "4ccfb3fe-5c64-45d6-8b3f-63249c69e146")]
        public readonly InputSlot<T3.Core.DataTypes.MeshBuffers> Mesh = new InputSlot<T3.Core.DataTypes.MeshBuffers>();

        [Input(Guid = "017cf5d0-5e39-480e-9a23-ce3d6e1c0d40")]
        public readonly InputSlot<float> BlendValue = new InputSlot<float>();

              
        
    }
} 

