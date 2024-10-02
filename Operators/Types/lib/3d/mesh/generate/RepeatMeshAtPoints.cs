using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_ab496711_8b99_4463_aac9_b41fdf46608d
{
    public class RepeatMeshAtPoints : Instance<RepeatMeshAtPoints>
    {
        [Output(Guid = "df775b6c-d4ca-42f2-9ebd-6d5397b13ab0")]
        public readonly Slot<T3.Core.DataTypes.MeshBuffers> Result = new();

        [Input(Guid = "f8fb6e15-00dd-485e-a7fe-fa75c77182c2")]
        public readonly InputSlot<T3.Core.DataTypes.MeshBuffers> InputMesh = new();

        [Input(Guid = "a7960188-ff39-4176-9d22-bc9d7e0cb2b5")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> Points = new();

        [Input(Guid = "abd961af-e76f-415b-a6ac-afb1cf08a1de")]
        public readonly InputSlot<float> Scale = new();

        [Input(Guid = "adfa0cb7-257f-4f03-b847-99a6bb317992")]
        public readonly InputSlot<bool> UseWForSize = new();

        [Input(Guid = "13852947-11aa-4f54-b415-6867421f3bc0")]
        public readonly InputSlot<System.Numerics.Vector3> WStretchAmount = new();

        [Input(Guid = "631a4691-0774-40c7-a8fa-4b9ee76854d6")]
        public readonly InputSlot<bool> UseStretch = new InputSlot<bool>();
        
    }
}

