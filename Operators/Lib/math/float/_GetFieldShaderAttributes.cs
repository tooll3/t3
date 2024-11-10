namespace Lib.math.@float;

[Guid("73c028d1-3de2-4269-b503-97f62bbce320")]
internal sealed class _GetFieldShaderAttributes : Instance<_GetFieldShaderAttributes>, IStatusProvider
{
    [Output(Guid = "A1AB0C16-ED15-4334-A529-10E3C217DF1A")]
    public readonly Slot<string> ShaderCode = new();

    [Output(Guid = "1791B00E-583F-4E3A-BEB9-7C4CA6648935")]
    public readonly Slot<List<float>> FloatParams = new();
    
    public _GetFieldShaderAttributes()
    {
        ShaderCode.UpdateAction += Update;
    }

    private void Update(EvaluationContext context)
    {
        var fieldDef = Field.GetValue(context);
        if (fieldDef == null)
        {
            _lastErrorMessage = "Missing input field";
            return;
            
        }
        
        var templateCode = TemplateCode.GetValue(context);
        if (string.IsNullOrEmpty(templateCode))
        {
            _lastErrorMessage = "Missing input template code";
            return;
        }

        ShaderCode.Value = fieldDef.GenerateShaderCode(templateCode);
        //Log.Debug("Generated shader code: " + ShaderCode.Value);
        FloatParams.Value = fieldDef.FloatBufferValues;
    }
        
    #region Implementation of IStatusProvider
    IStatusProvider.StatusLevel IStatusProvider.GetStatusLevel() =>
        string.IsNullOrEmpty(_lastErrorMessage) ? IStatusProvider.StatusLevel.Success : IStatusProvider.StatusLevel.Warning;

    string IStatusProvider.GetStatusMessage() => _lastErrorMessage;
    private string _lastErrorMessage = string.Empty;
    #endregion
    
    
    
    [Input(Guid = "FFC1C70E-B717-4337-916D-C3A13343E9CC")]
    public readonly InputSlot<FieldShaderGraph> Field = new();
    
    [Input(Guid = "BCF6DE27-1FFD-422C-9F5B-910D89CAD1A4")]
    public readonly InputSlot<string> TemplateCode = new();
}