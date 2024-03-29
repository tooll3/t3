using System.Drawing;
using ImGuiNET;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.InputUi.CombinedInputs;
using T3.Editor.Gui.Styling;
using Icon = T3.Editor.Gui.Styling.Icon;

namespace T3.Editor.Gui.Interaction;

public static class CurveEditPopup
{
    public static bool DrawPopupIndicator(Instance compositionOp, Symbol.Child.Input input, ref Curve curve, Vector2 keepPositionForIcon, bool cloneIfModified, out InputEditStateFlags result)
    {
        var openPop = false;
        var keepPositionForContentBelow = ImGui.GetCursorPos();
        ImGui.SetCursorPos(keepPositionForIcon);
        var iconSize = ImGui.GetFrameHeight() * Vector2.One;
        ImGui.BeginChild("Icon", iconSize);
        var clicked = CustomComponents.IconButton(Icon.PopUp, iconSize);
        if (clicked)
        {
            openPop = true;
        }

        ImGui.EndChild();
        ImGui.SetCursorPos(keepPositionForContentBelow);
        if (openPop)
        {
            ImGui.OpenPopup(CurvePopupId);
            ImGui.SetNextWindowSize(new Vector2(500, 400));
        }

        var isOpen = ImGui.IsPopupOpen(CurvePopupId);
        
        result= DrawPopup(ref curve, input, cloneIfModified, compositionOp);
        return isOpen;
    }
    
    private static InputEditStateFlags DrawPopup(ref Curve curve, Symbol.Child.Input input,
                                                 bool cloneIfModified, Instance comp)
    {
        
        var edited = InputEditStateFlags.Nothing;
        //var componentId = ImGui.GetID("curveEditor");
        ImGui.SetNextWindowSize(new Vector2(500, 400));
        if (ImGui.BeginPopup(CurvePopupId, ImGuiWindowFlags.Popup))
        {
            // if (_activatedComponentId != componentId)
            // {
            //     _activatedComponentId = componentId;
            // }
            
            edited= CurveInputEditing.DrawCanvasForCurve(ref curve, input, cloneIfModified, comp, T3Ui.EditingFlags.ExpandVertically);
            ImGui.EndPopup();
        }
        else
        {
            // if (_activatedComponentId == componentId)
            // {
            //     _activatedComponentId = 0;
            // }
        }

        return edited;
    }
    
    
    private static readonly CurveInputEditing.CurveInteraction.SingleCurveEditCanvas _singleCurveCanvas = new() { ImGuiTitle = "canvasPopup"};
    private const string CurvePopupId= "##CurvePopup";
    // private static uint _activatedComponentId;
    private static readonly Bitmap _bmp = new(1, 1);
}