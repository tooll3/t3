using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_816336a8_e214_4d2c_b8f9_05b1aa3ff2e2
{
    public class ExtrudeCurves : Instance<ExtrudeCurves>
    {

        [Output(Guid = "79ba19e0-13c3-40c7-8e0a-f190b03e95b0")]
        public readonly Slot<T3.Core.DataTypes.MeshBuffers> Output2 = new Slot<T3.Core.DataTypes.MeshBuffers>();

        [Input(Guid = "4d31be7a-3011-4fdf-9c63-425387b9bbfc")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> RailPoints = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "5e2ada8d-10fa-419d-a377-0b504437fd72")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> Points = new InputSlot<T3.Core.DataTypes.BufferWithViews>();


        private enum SampleModes
        {
            StartEnd,
            StartLength,
        }
    }
}

