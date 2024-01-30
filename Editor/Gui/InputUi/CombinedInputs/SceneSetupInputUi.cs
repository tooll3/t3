using System.Numerics;
using ImGuiNET;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.InputUi.CombinedInputs
{
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
                ImGui.OpenPopup(EditSceneStructure);
                DrawPopup(value);
                return InputEditStateFlags.Modified;
            }

            return InputEditStateFlags.Nothing;
            //throw new System.NotImplementedException();
        }

        protected override void DrawReadOnlyControl(string name, ref SceneSetup value)
        {
            ImGui.NewLine();
        }

        private const string EditSceneStructure = "Edit Scene Structure";

        void DrawPopup(SceneSetup setup)
        {
            if (setup == null)
                return;

            if (ImGui.BeginPopup(EditSceneStructure))
            {
                if (setup.Nodes == null)
                {
                    ImGui.TextUnformatted("node structure undefined");
                }
                else
                {
                    foreach (var node in setup.Nodes)
                    {
                        DrawNode(node);
                    }   
                }

                ImGui.EndPopup();
            }
        }

        private void DrawNode(SceneSetup.SceneNode node)
        {
            var label = string.IsNullOrEmpty(node.Name) ? "???" : node.Name;
            //ImGui.SetNextItemOpen(true);

            if (ImGui.TreeNode(label))
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
}