using ImGuiNET;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.Windows;
using T3.Editor.UiModel;
using T3.Editor.UiModel.InputsAndTypes;

namespace T3.Editor.Gui.Graph.Interaction;

internal sealed class UiElements
{
    public static void DrawExampleOperator(SymbolUi symbolUi, string label)
    {
        var color = symbolUi.Symbol.OutputDefinitions.Count > 0
                        ? TypeUiRegistry.GetPropertiesForType(symbolUi.Symbol.OutputDefinitions[0]?.ValueType).Color
                        : UiColors.Gray;

        ImGui.PushStyleColor(ImGuiCol.Button, ColorVariations.OperatorBackground.Apply(color).Rgba);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColorVariations.OperatorBackgroundHover.Apply(color).Rgba);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, ColorVariations.OperatorBackgroundHover.Apply(color).Rgba);
        ImGui.PushStyleColor(ImGuiCol.Text, ColorVariations.OperatorLabel.Apply(color).Rgba);

        ImGui.SameLine();

        var restSpace = ImGui.GetWindowWidth() - ImGui.GetCursorPos().X;
        if (restSpace < 100)
        {
            ImGui.Dummy(new Vector2(10,10));
        }

        ImGui.Button(label);
        SymbolLibrary.HandleDragAndDropForSymbolItem(symbolUi.Symbol);
        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
        }
            
        if (!string.IsNullOrEmpty(symbolUi.Description))
        {
            CustomComponents.TooltipForLastItem(symbolUi.Description);
        }

        ImGui.PopStyleColor(4);
    }
}