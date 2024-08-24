using System.Drawing;
using System.Numerics;
using ImGuiNET;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.InputUi.CombinedInputs;
using T3.Editor.Gui.Styling;
using Icon = T3.Editor.Gui.Styling.Icon;

namespace T3.Editor.Gui.Interaction;

public static class CurveEditPopup
{
    public static bool DrawPopupIndicator(SymbolChild.Input input, ref Curve curve, Vector2 keepPositionForIcon, bool cloneIfModified, out InputEditStateFlags result)
    {
        var openPop = false;
        result = InputEditStateFlags.Nothing;
        var keepPositionForContentBelow = ImGui.GetCursorPos();
        ImGui.SetCursorPos(keepPositionForIcon);
        var iconSize = ImGui.GetFrameHeight() * Vector2.One;
        ImGui.BeginChild("Icon", iconSize);
        var clicked = CustomComponents.IconButton(Icon.PopUp, iconSize);
        if (clicked)
            openPop = true;
        
        ImGui.EndChild();

        ImGui.SetCursorPos(keepPositionForContentBelow);

        if (openPop)
        {
            ImGui.SetNextWindowPos(keepPositionForIcon + ImGui.GetWindowPos(), ImGuiCond.Once);
            ImGui.OpenPopup(CurvePopupId);
            _justOpened = true;
        }

        var isOpen = ImGui.IsPopupOpen(CurvePopupId);

        result =  DrawPopup(ref curve, input, cloneIfModified);
        return isOpen;
    }

    private static bool _justOpened = false;
    
    private static InputEditStateFlags DrawPopup(ref Curve curve, SymbolChild.Input input,
                                                 bool cloneIfModified)
    {
        var edited = InputEditStateFlags.Nothing;
        if (_justOpened)
        {
            _justOpened = false;
            ImGui.SetNextWindowSize(new Vector2(500, 400), ImGuiCond.Once);
        }

        var isOpen = true;

        if (!ImGui.BeginPopupModal(CurvePopupId, ref isOpen))
            return edited;
        
        // Close popup if clicked outside
        if(ImGui.IsMouseClicked(ImGuiMouseButton.Left) && !ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows|ImGuiHoveredFlags.RectOnly))
            ImGui.CloseCurrentPopup();
        
        var keepScale = T3Ui.UiScaleFactor;
        T3Ui.UiScaleFactor = 1;

        edited= CurveInputEditing.DrawCanvasForCurve(ref curve, input, cloneIfModified, T3Ui.EditingFlags.ExpandVertically);
        
        T3Ui.UiScaleFactor = keepScale;
        
        ImGui.EndPopup();
        return edited;
    }
    
    
    private static readonly CurveInputEditing.CurveInteraction.SingleCurveEditCanvas _singleCurveCanvas = new() { ImGuiTitle = "canvasPopup"};
    private const string CurvePopupId= "##CurvePopup";
    private static readonly Bitmap _bmp = new(1, 1);
}