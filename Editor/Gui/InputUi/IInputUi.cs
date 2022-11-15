using System;
using Editor.Gui.Selection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.Operator;
using T3.Core.Operator.Slots;

namespace Editor.Gui.InputUi
{
    [Flags]
    public enum InputEditStateFlags
    {
        Nothing = 0x0,
        Started = 0x1,
        Modified = 0x2,
        Finished = 0x4,
        ShowOptions = 0x8,
        ResetToDefault = 0x10,
        ModifiedAndFinished = Modified | Finished
    }

    public enum Relevancy
    {
        Required,
        Relevant,
        Optional
    }

    public interface IInputUi : ISelectableCanvasObject
    {
        SymbolUi Parent { get; set; }
        Symbol.InputDefinition InputDefinition { get; set; }
        Type Type { get; }
        Relevancy Relevancy { get; set; }
        bool IsAnimatable { get; }


        void ApplyValueToAnimation(IInputSlot inputSlot, InputValue inputValue, Animator animator, double time);
        
        InputEditStateFlags DrawInputEdit(IInputSlot input, SymbolUi compositionUi, SymbolChildUi symbolChildUi);
        string GetSlotValue(IInputSlot inputSlot);
        void DrawSettings();

        IInputUi Clone();
        void Write(JsonTextWriter writer);
        void Read(JToken inputToken);
    }
}