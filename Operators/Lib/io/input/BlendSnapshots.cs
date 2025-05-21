using T3.Core.IO;

namespace Lib.io.input;

[Guid("0a8497a1-8740-4561-b258-51475c44473b")]
internal sealed class BlendSnapshots : Instance<BlendSnapshots>
{
    [Output(Guid = "7d83da79-94a9-4ac8-bb30-c1efa5a0a205")]
    public readonly Slot<Command> Result = new();

    public BlendSnapshots()
    {
        Result.UpdateAction += Update;
    }

    private void Update(EvaluationContext context)
    {
        if (Math.Abs(context.LocalTime - _lastUpdateTime) < 0.001f)
            return;

        _lastUpdateTime = context.LocalTime;

        if (!TriggerSet.GetValue(context))
            return;

        if (Parent == null)
            return;

        _weights.Clear();
        var weights = Weights.GetValue(context);
        if (weights != null)
        {
            _weights.AddRange(weights);
        }

        _indices.Clear();
        var indices = Indices.GetValue(context);
        if (indices != null)
        {
            foreach (var floatIndex in indices)
            {
                _indices.Add((int)(floatIndex + 0.5f));
            }
        }

        SnapShotBlendingData.CompositionBlendRequests[Parent.Symbol.Id] = new SnapShotBlendingData.BlendRequest
                                                                              {
                                                                                  BlendWeights = _weights,
                                                                                  BlendIndices = _indices,
                                                                                  SymbolId = Parent.Symbol.Id,
                                                                              };
    }

    private double _lastUpdateTime;
    private readonly List<int> _indices = [];
    private readonly List<float> _weights = [];

    [Input(Guid = "5bfede9d-6a06-44ed-922f-5c712070940c")]
    public readonly InputSlot<bool> TriggerSet = new();

    [Input(Guid = "F1C3BF1F-8275-410C-AF07-44DF5BD90D6B")]
    public readonly InputSlot<List<float>> Weights = new();

    /// <summary>
    /// Yes, this should be List<int> eventually...
    /// </summary>
    [Input(Guid = "8A5B056B-D0BC-4F66-BE12-5F56439D9C6E")]
    public readonly InputSlot<List<float>> Indices = new();
}