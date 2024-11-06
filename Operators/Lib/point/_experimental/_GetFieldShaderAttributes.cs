namespace Lib.math.@float;

[Guid("73c028d1-3de2-4269-b503-97f62bbce320")]
internal sealed class _GetFieldShaderAttributes : Instance<_GetFieldShaderAttributes>
{
    [Output(Guid = "5ddbac03-6d65-48c1-9545-dfc6679f7706")]
    public readonly Slot<float> Result = new();

    public _GetFieldShaderAttributes()
    {
        Result.UpdateAction += Update;
    }

    private void Update(EvaluationContext context)
    {
        var v = Value.GetValue(context);
        var mod2 = ModuloValue.GetValue(context);

        if (mod2 != 0)
        {
            Result.Value = v - mod2 * (float)Math.Floor(v/mod2);
        }
        else
        {
            Log.Debug("Modulo caused division by zero", this);
            Result.Value = 0;
        }
    }
        
    [Input(Guid = "28e1dc97-e133-493b-a04f-f268fd86c378")]
    public readonly InputSlot<float> Value = new();

    [Input(Guid = "561c9435-49b6-465e-b0d7-db7a14c70116")]
    public readonly InputSlot<float> ModuloValue = new();
}