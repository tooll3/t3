using System;
using T3.Core.Operator.Slots;

namespace T3.Core.Operator.Interfaces;

public interface ITransformable
{
    // // Type Type { get; } // c#8 default interface impl would be nice for this
    // System.Numerics.Vector3 Translation { get; set; }
    // System.Numerics.Vector3 Rotation { get; set; }
    // System.Numerics.Vector3 Scale { get; set; }
        
    IInputSlot TranslationInput { get; }
    IInputSlot RotationInput { get;  }
    IInputSlot ScaleInput { get;  }
        
    Action<Instance, EvaluationContext> TransformCallback { get; set; }
}