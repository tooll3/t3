using System.Collections.Generic;

namespace T3.Core.Operator.Slots;

public interface IExtractable
{
    public bool TryExtractInputsFor(IInputSlot inputSlot, out IEnumerable<ExtractedInput> inputParameters);
}

public readonly struct ExtractedInput
{
    public readonly SymbolChild.Input InstanceInput;
    public readonly InputValue InputValue;
        
    public ExtractedInput(SymbolChild.Input instanceInput, InputValue inputValue)
    {
        InstanceInput = instanceInput;
        InputValue = inputValue;
    }
}