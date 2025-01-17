#nullable enable
using ImGuiNET;
using T3.Core.Utils;
using T3.Editor.Gui.Styling;

namespace T3.Editor.Gui.Graph.Interaction;

internal static class ImageBackgroundFading
{
    public static void HandleImageBackgroundFading(GraphImageBackground imageBackground, out float backgroundImageOpacity)
    {
        backgroundImageOpacity = imageBackground.IsActive 
                                     ? (ImGui.GetMousePos().X + 50).Clamp(0, 100) / 100 
                                     : 1;

        if (imageBackground.IsActive && backgroundImageOpacity == 0)
        {
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                imageBackground.ClearBackground();
            }
        }
    }

    public static void HandleGraphFading(GraphImageBackground imageBackground, ImDrawListPtr drawList, out float graphOpacity)
    {
        const float activeBorderWidth = 30;
        
        // Fade and toggle graph on right edge
        var windowPos = Vector2.Zero;
        var windowSize = ImGui.GetIO().DisplaySize;
        var mousePos = ImGui.GetMousePos();
        //var showBackgroundOnly = imageBackground.IsActive && mousePos.X > windowSize.X + windowPos.X - activeBorderWidth;

        
        graphOpacity = (imageBackground.IsActive && !ImGui.IsMouseDown(ImGuiMouseButton.Left))
                           ? (windowSize.X + windowPos.X - mousePos.X - activeBorderWidth).Clamp(0, 100) / 100
                           : 1;

        if (graphOpacity < 1)
        {
            var x = windowPos.X + windowSize.X - activeBorderWidth;
            drawList.AddRectFilled(new Vector2(x, windowPos.Y),
                                   new Vector2(x + 1, windowPos.Y + windowSize.Y),
                                   UiColors.BackgroundFull.Fade((1 - graphOpacity)) * 0.5f);
            drawList.AddRectFilled(new Vector2(x + 1, windowPos.Y),
                                   new Vector2(x + 2, windowPos.Y + windowSize.Y),
                                   UiColors.ForegroundFull.Fade((1 - graphOpacity)) * 0.5f);
        }

        
        if (graphOpacity <=0 && !ImGui.IsAnyItemActive() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            imageBackground.HasInteractionFocus = !imageBackground.HasInteractionFocus;
    }
}