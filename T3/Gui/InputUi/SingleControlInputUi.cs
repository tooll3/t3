using ImGuiNET;
 
 namespace T3.Gui.InputUi
 {
     public abstract class SingleControlInputUi<T> : InputValueUi<T>
     {
         /// <summary>
         /// Defines how the parameters for specific types should be rendered.
         /// Is the implemented in derived classes like <see cref="Vector3InputUi"/>.
         /// These 
         /// </summary>
         /// <returns></returns>
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