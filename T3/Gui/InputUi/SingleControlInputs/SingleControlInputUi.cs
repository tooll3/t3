using ImGuiNET;

namespace T3.Gui.InputUi.SingleControlInputs
 {
     /// <summary>
     /// Defines how the parameters for specific types are rendered by ImGui components.
     /// These are implemented in derived classes like <see cref="Vector3InputUi"/> which
     /// have to implement DrawEditControl.
     /// </summary>
     /// <remarks>
     /// Since <see cref="SingleControlInputUi.DrawSingleEditControl()"/> only returns
     /// a single boolean for the ImGui component and the subsequent use of ImGui.IsItemClicked() we
     /// can't implement these with multiple or complex components.</remarks>
     public abstract class SingleControlInputUi<T> : InputValueUi<T>
     {
         protected abstract bool DrawSingleEditControl(string name, ref T value);
 
         protected override InputEditStateFlags DrawEditControl(string name, ref T value)
         {
             bool valueModified = DrawSingleEditControl(name, ref value);
 
             InputEditStateFlags inputEditStateFlags = InputEditStateFlags.Nothing;
             inputEditStateFlags |= ImGui.IsItemClicked() ? InputEditStateFlags.Started : InputEditStateFlags.Nothing;
             inputEditStateFlags |= valueModified ? InputEditStateFlags.Modified : InputEditStateFlags.Nothing;
             inputEditStateFlags |= ImGui.IsItemDeactivatedAfterEdit() ? InputEditStateFlags.Finished : InputEditStateFlags.Nothing;
 
             return inputEditStateFlags;
         }
     }
 }