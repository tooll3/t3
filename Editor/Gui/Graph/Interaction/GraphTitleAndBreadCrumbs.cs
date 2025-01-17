using System.Diagnostics;
using ImGuiNET;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Editor.Gui.Styling;
using T3.Editor.UiModel.ProjectHandling;

namespace T3.Editor.Gui.Graph.Interaction;

internal static class GraphTitleAndBreadCrumbs
{
    public static void Draw(ProjectView window)
    {
        if (window.Composition == null)
            return;
            
        DrawBreadcrumbs(window);
        DrawNameAndDescription(window.Composition);
    }

    private static void DrawBreadcrumbs(ProjectView components)
    {
        var composition = components.Composition;
        Debug.Assert(composition != null);
        ImGui.SetCursorScreenPos(ImGui.GetWindowPos() + new Vector2(1, 1));
        if (ImGui.Button("Hub"))
        {
            components.Close();
        }
        
        FormInputs.AddVerticalSpace();
        var parents = Structure.CollectParentInstances(composition.Instance);

        ImGui.PushStyleColor(ImGuiCol.Button, Color.Transparent.Rgba);
        ImGui.PushStyleColor(ImGuiCol.Text, UiColors.TextMuted.Rgba);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(1, 1));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));
        {
            var isFirstChild = true;
            foreach (var p in parents)
            {
                if (isFirstChild)
                {
                    isFirstChild=false;
                    ImGui.SameLine(7);
                }
                else
                {
                    ImGui.SameLine(0);
                }
                        
                        

                ImGui.PushID(p.SymbolChildId.GetHashCode());

                ImGui.PushFont(Fonts.FontSmall);
                var clicked = ImGui.Button(p.Symbol.Name);
                ImGui.PopFont();
                        
                if (p.Parent == null && ImGui.BeginItemTooltip())
                {
                    PopulateDependenciesTooltip(p);
                    ImGui.EndTooltip();
                }

                if (clicked)
                {
                    components.TrySetCompositionOpToParent();
                    break;
                }

                ImGui.SameLine();
                ImGui.PopID();
                ImGui.PushFont(Icons.IconFont);
                ImGui.TextUnformatted(_breadCrumbSeparator);
                ImGui.PopFont();
            }
                    
                    
        }
        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor(2);
    }

    private static void PopulateDependenciesTooltip(Instance p)
    {
        var project = p.Symbol.SymbolPackage;
        ImGui.Text("Project: " + project.DisplayName);
        ImGui.NewLine();
        ImGui.Text("Dependencies:");

        foreach (var dependency in project.Dependencies)
        {
            ImGui.Text(dependency.ToString());
        }
    }

    private static void DrawNameAndDescription(Composition compositionOp)
    {
        ImGui.SetCursorPosX(8);
        ImGui.PushFont(Fonts.FontLarge);
        ImGui.TextUnformatted(compositionOp.Symbol.Name);

        if (compositionOp.Instance.Parent == null && ImGui.BeginItemTooltip())
        {
            ImGui.PushFont(Fonts.FontNormal);
            PopulateDependenciesTooltip(compositionOp.Instance);
            ImGui.PopFont();
            ImGui.EndTooltip();
        }
                
        ImGui.SameLine();

        ImGui.PushStyleColor(ImGuiCol.Text, UiColors.ForegroundFull.Fade(0.3f).Rgba);
        ImGui.TextUnformatted("  - " + compositionOp.Symbol.Namespace);
        ImGui.PopFont();
        ImGui.PopStyleColor();
    }
    private static readonly string _breadCrumbSeparator = (char)Icon.ChevronRight + "";
}