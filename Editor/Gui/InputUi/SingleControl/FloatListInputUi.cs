using ImGuiNET;
using T3.Core.Operator;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel.InputsAndTypes;

namespace T3.Editor.Gui.InputUi.SingleControl;

internal sealed class FloatListInputUi : InputValueUi<List<float>>
{
    public override IInputUi Clone()
    {
        return new FloatListInputUi
                   {
                       InputDefinition = InputDefinition,
                       Parent = Parent,
                       PosOnCanvas = PosOnCanvas,
                       Relevancy = Relevancy
                   };
    }
    
    protected override InputEditStateFlags DrawEditControl(string name, Symbol.Child.Input input, ref List<float> list, bool readOnly)
    {
        if (list == null)
        {
            //ImGui.TextUnformatted(name + " is null?!");
            if (ImGui.Button("Create"))
            {
                list = [];
                return InputEditStateFlags.Modified | InputEditStateFlags.Finished;
            }
            return InputEditStateFlags.Nothing;
        }

        if (list.Count == 0)
        {
            if(ImGui.Button("+"))
            {
                if (input.IsDefault)
                {
                    list = [];
                    input.IsDefault = false;
                } 
                list.Add(0);
                return InputEditStateFlags.ModifiedAndFinished;
            }

            return InputEditStateFlags.Nothing;
        }
        if(ImGui.Button("Clear all"))
        {
            if (input.IsDefault)
            {
                list = [];
                input.IsDefault = false;
            }
            else
            {
                list.Clear();
            }

            return InputEditStateFlags.ModifiedAndFinished;
        }
        
        var cloneIfModified = input.IsDefault;
        
        var modified = InputEditStateFlags.Nothing;
        for (var index = 0; index < list.Count; index++)
        {
            ImGui.PushID(index);
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted($"{index}.");
            ImGui.SameLine(30 * T3Ui.UiScaleFactor);
            
            var f = list[index];
            var ff = f;
            var r = SingleValueEdit.Draw(ref ff, new Vector2(300 * T3Ui.UiScaleFactor,0));
            
            ImGui.SameLine();
            if(ImGui.Button("×"))
            {
                r |= InputEditStateFlags.ModifiedAndFinished;
                if (cloneIfModified)
                {
                    list = [..list];
                    cloneIfModified = false;
                    input.IsDefault = false;
                } 
                list.RemoveAt(index);
            }
            
            ImGui.SameLine();
            if(ImGui.Button("+"))
            {
                r |= InputEditStateFlags.ModifiedAndFinished;
                if (cloneIfModified)
                {
                    list = [..list];
                    cloneIfModified = false;
                    input.IsDefault = false;
                } 
                list.Insert(index, ff);
            }
            if (r != InputEditStateFlags.Nothing)
            {
                if (cloneIfModified)
                {
                    list = [..list];
                    cloneIfModified = false;
                    input.IsDefault = false;
                }
                modified |= r;
                list[index] = ff;
            }
            ImGui.PopID();
        }
        return modified;
    }
    // protected override bool DrawSingleEditControl(string name, ref List<float> list)
    // {
    //     if (list == null || list.Count == 0) return false;
    //     lock (list)
    //     {
    //         foreach (var i in list)
    //         {
    //             var f = i;
    //             SingleValueEdit.Draw(ref f, Vector2.Zero);
    //         }
    //         string outputString;
    //         outputString = list == null ? "NULL" :  string.Join(", ", list);
    //         ImGui.TextUnformatted($"{outputString}");
    //     }
    //     return false;
    // }

    protected override void DrawReadOnlyControl(string name, ref List<float> list)
    {
        string outputString;
        if (list == null)
        {
            outputString = "NULL";
        }
        else
        {
            lock (list)
            {
                outputString = string.Join(", ", list);
            }
        }
        
        ImGui.TextUnformatted($"{outputString}");
    }
}