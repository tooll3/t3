using ImGuiNET;
using T3.Core.Operator.Slots;
using T3.Editor.Gui.Interaction.ParameterCollections;
using T3.Editor.Gui.Styling;

namespace T3.Editor.Gui.Windows.Variations;

public  class ParameterGroupUi
{
    public void DrawToolbarContent()
    {
        
    }
    
    public  void DrawContent()
    {
        if(ParameterCollectionHandling.ActiveParamCollection == null)
        {
            CustomComponents.EmptyWindowMessage("No collection defined yet.");
            return;

        }

        foreach (var x in ParameterCollectionHandling.ActiveParamCollection.ParameterDefinitions)
        {
            if (!ParameterCollectionHandling.TryGetInputForParamDef(x, out var input))
            {
                ImGui.Text("Input Not found");
            }
            else if(input is InputSlot<float> floatInput)
            {
                ImGui.Text(input.Input.InputDefinition.Name + " " + floatInput.Value);
            }
        }
    }
}