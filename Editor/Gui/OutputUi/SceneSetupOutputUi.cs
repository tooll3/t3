using ImGuiNET;
using T3.Core.DataTypes;
using T3.Core.Operator.Slots;

namespace T3.Editor.Gui.OutputUi;

public class SceneSetupOutputUi : OutputUi<float>
{
    public override IOutputUi Clone()
    {
        return new SceneSetupOutputUi()
                   {
                       OutputDefinition = OutputDefinition,
                       PosOnCanvas = PosOnCanvas,
                       Size = Size
                   };
    }
    
    protected override void DrawTypedValue(ISlot slot)
    {
        if (slot is not Slot<SceneSetup> sceneSetupSlot)
            return;

        var setup = sceneSetupSlot.Value;
        if (setup == null)
        {
            ImGui.TextUnformatted("Undefined setup");
            return;
        }

        if (setup.Nodes == null)
        {
            ImGui.TextUnformatted("node structure undefined");
            return;
            
        }

        foreach (var node in setup.Nodes)
        {
            DrawNode(node);
        }
    }
    
    private void DrawNode(SceneSetup.SceneNode node)
    {
        var label = string.IsNullOrEmpty(node.Name) ? "???" : node.Name;
        //ImGui.SetNextItemOpen(true);
        
        if(ImGui.TreeNode(label))
        {
            var meshLabel = string.IsNullOrEmpty(node.MeshName) ? "-" : "Mesh:" + node.MeshName;
            ImGui.SameLine(200);
            ImGui.TextUnformatted(meshLabel);
            
            foreach (var child in node.ChildNodes)
            {
                DrawNode(child);
            }
            ImGui.TreePop();
        }
    }
}