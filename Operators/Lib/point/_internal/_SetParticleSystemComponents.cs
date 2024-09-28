namespace Lib.point._internal;

[Guid("705df4fe-8f91-4b1e-a7d1-432011ffcb3f")]
public class _SetParticleSystemComponents : Instance<_SetParticleSystemComponents>
{
    [Output(Guid = "9d729d46-06e2-4152-a5d7-3368ae5d737a")]
    public readonly Slot<Command> Output = new();

    public _SetParticleSystemComponents()
    {
        Output.UpdateAction += Update;
    }
        
    private void Update(EvaluationContext context)
    {
        //_particleSystem.PointBuffer = PointsBuffer.GetValue(context);
        _particleSystem.ParticleBuffer = PointsSimBuffer.GetValue(context);
        _particleSystem.SpeedFactor = SpeedFactor.GetValue(context);
        _particleSystem.InitializeVelocityFactor = InitializeVelocityFactor.GetValue(context);
            
        var effects = Effects.CollectedInputs;
        var keep = context.ParticleSystem;
        if (effects != null)
        {
            context.ParticleSystem = _particleSystem;
                
            // execute commands
            for (int i = 0; i < effects.Count; i++)
            {
                effects[i].GetValue(context);
            }
        }

        context.ParticleSystem = keep;
            
        Effects.DirtyFlag.Clear();
    }

    private readonly ParticleSystem _particleSystem = new();
        
    // [Input(Guid = "8B47EC1F-8537-47BE-B31B-641BCEA3BDFE")]
    // public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> PointsBuffer = new();

    [Input(Guid = "13583F72-3F77-4BE0-B596-B8DBD27CA19C")]
    public readonly InputSlot<BufferWithViews> PointsSimBuffer = new();
        
    [Input(Guid = "083C9379-FC0A-4D35-B056-1A639F739321")]
    public readonly InputSlot<float> SpeedFactor = new();
        
    [Input(Guid = "F3AB1099-3A0D-409E-AA18-89219E85E01F")]
    public readonly InputSlot<float> InitializeVelocityFactor = new();
        
    [Input(Guid = "73128257-D731-4065-B19A-C8FA21803CD4")]
    public readonly MultiInputSlot<ParticleSystem> Effects = new();
}