namespace T3.Editor.Gui.Interaction.ParameterCollections;

public static class ParamCollectionActions
{
    public static void SetParamGroupControl(int controllerIndex, float normalizedValue )
    {
        ParameterCollectionHandling.TryApplyControllerChange(controllerIndex, normalizedValue);
    }
}