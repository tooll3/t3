using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.InputUi.CombinedInputs;

public class SceneSetupInputUi : InputValueUi<SceneSetup>
{
    // public override IInputUi Clone()
    // {
    //     return new SceneSetupInputUi
    //            {
    //                InputDefinition = InputDefinition,
    //                Parent = Parent,
    //                PosOnCanvas = PosOnCanvas,
    //                Relevancy = Relevancy,
    //                Size = Size,
    //            };
    // }

    public override IInputUi Clone()
    {
        return new SceneSetupInputUi();
    }

    protected override InputEditStateFlags DrawEditControl(string name, SymbolChild.Input input, ref SceneSetup value, bool readOnly)
    {
        if (ImGui.Button("Edit Scene Setup"))
        {
            ImGui.OpenPopup(SceneSetupPopup.EditSceneStructureId);
            return InputEditStateFlags.Modified;
        }
            
        SceneSetupPopup.DrawPopup(value);

        return InputEditStateFlags.Nothing;
        //throw new System.NotImplementedException();
    }

    protected override void DrawReadOnlyControl(string name, ref SceneSetup value)
    {
        ImGui.NewLine();
    }

        
}

public static class SceneSetupPopup
{
    public const string EditSceneStructureId = "Edit Scene Structure";
    public static void DrawPopup(SceneSetup setup)
    {
        if (setup == null)
            return;

        ImGui.SetNextWindowSize(new Vector2(400, 400));
        if (ImGui.BeginPopup(EditSceneStructureId))
        {
            if (setup.RootNodes == null)
            {
                ImGui.TextUnformatted("node structure undefined");
            }
            else
            {
                foreach (var node in setup.RootNodes)
                {
                    DrawNode(node, setup);
                }   
            }

            ImGui.EndPopup();
        }
    }

    private static void DrawNode(SceneSetup.SceneNode node, SceneSetup sceneSetup)
    {
        var label = string.IsNullOrEmpty(node.Name) ? "???" : node.Name;

        if (sceneSetup.NodeSettings == null)
        {
            sceneSetup.NodeSettings = new List<SceneSetup.NodeSetting>();
        }
        
        var nodeSettings = sceneSetup.NodeSettings.SingleOrDefault(s => s.NodeHashId == node.Name.GetHashCode());
        
        if (nodeSettings == null)
        {
            
        }
        
        // ImGui.SetNextItemOpen(true);
        var isOpen = ImGui.TreeNodeEx(label, ImGuiTreeNodeFlags.DefaultOpen);
        ImGui.SameLine(1);
        
        var icon = nodeSettings== null || nodeSettings.Visibility == SceneSetup.NodeSetting.NodeVisibilities.Visible
            ? Icon.Camera
            : Icon.Flame;
        
        if (CustomComponents.IconButton(icon, new Vector2(16, 16), CustomComponents.ButtonStates.Dimmed))
        {
            if (nodeSettings == null)
            {
                nodeSettings = new SceneSetup.NodeSetting()
                                   {
                                       NodeHashId = label.GetHashCode(),
                                       Visibility = SceneSetup.NodeSetting.NodeVisibilities.HiddenBranch,
                                   };
                sceneSetup.NodeSettings.Add(nodeSettings);
            }
            else
            {
                nodeSettings.Visibility = nodeSettings.Visibility == SceneSetup.NodeSetting.NodeVisibilities.Visible 
                                              ? SceneSetup.NodeSetting.NodeVisibilities.HiddenBranch 
                                              : SceneSetup.NodeSetting.NodeVisibilities.Visible;
            }
        }
        
        if (!isOpen)
            return;
        
        var meshLabel = string.IsNullOrEmpty(node.MeshName) ? "-" : "Mesh:" + node.MeshName;
            
        ImGui.SameLine(200);
        ImGui.TextUnformatted(meshLabel);
            

        foreach (var child in node.ChildNodes)
        {
            DrawNode(child, sceneSetup);
        }

        ImGui.TreePop();
    }    
}
