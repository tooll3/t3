using System.Collections.Generic;

namespace T3.Core.Operator.Slots;

public interface IExtractable
{
    public bool TryExtractInputsFor(IInputSlot inputSlot, out IEnumerable<ExtractedInput> inputParameters);
}

public readonly struct ExtractedInput(Symbol.Child.Input instanceInput, InputValue inputValue)
{
    public readonly Symbol.Child.Input InstanceInput = instanceInput;
    public readonly InputValue InputValue = inputValue;
}