using ImGuiNET;
using T3.Editor.Gui.InputUi;

namespace Editor.Gui.InputUi.SingleControl
 {
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