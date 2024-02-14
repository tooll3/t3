using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using ImGuiNET;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Utils;
using T3.Editor.Gui.Graph.Dialogs;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Interaction.StartupCheck;
using T3.Editor.Gui.Styling;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Windows;

public class OperatorHelp
{
    public void DrawHelpIcon(SymbolUi symbolUi)
    {
        // Help indicator
        {
            ImGui.SameLine();
            var w = ImGui.GetFrameHeight();
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 2);
            var toggledToEdit = ImGui.GetIO().KeyCtrl;
            var icon = toggledToEdit ? Icon.PopUp : Icon.Help;
            if (CustomComponents.IconButton(
                                            icon,
                                            new Vector2(w, w),
                                            _isDocumentationActive
                                                ? CustomComponents.ButtonStates.Activated
                                                : CustomComponents.ButtonStates.Dimmed
                                           ))
            {
                if (toggledToEdit)
                {
                    EditDescriptionDialog.ShowNextFrame();
                }
                else
                {
                    _isDocumentationActive = !_isDocumentationActive;
                }
            }

            if (ImGui.IsItemHovered() && !_isDocumentationActive)
            {
                _timeSinceTooltipHovered += ImGui.GetIO().DeltaTime;

                var delayedFadeInToPreventImguiFlickering = (_timeSinceTooltipHovered * 3).Clamp(0.001f, 1);
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, delayedFadeInToPreventImguiFlickering * ImGui.GetStyle().Alpha);
                ImGui.BeginTooltip();
                ImGui.Dummy(new Vector2(500 * T3Ui.UiScaleFactor, 1));

                DrawHelp(symbolUi);
                ImGui.EndTooltip();
                ImGui.PopStyleVar();
            }
            else
            {
                _timeSinceTooltipHovered = 0;
            }
        }
    }

    public void DrawHelpSummary(SymbolUi symbolUi)
    {
        if (string.IsNullOrEmpty(symbolUi.Description))
            return;

        using var reader = new StringReader(symbolUi.Description);
        var firstLine = reader.ReadLine();
        if (string.IsNullOrEmpty(firstLine))
            return;
        
        ImGui.Indent(10);

        ImGui.PushStyleColor(ImGuiCol.Text, UiColors.TextMuted.Rgba);
        ImGui.TextWrapped(firstLine);
        if (ImGui.IsItemHovered())
        {
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                EditDescriptionDialog.ShowNextFrame();
            }
        }        
        ImGui.PopStyleColor();
        
        ImGui.PushStyleColor(ImGuiCol.Text, UiColors.TextMuted.Fade(0.5f).Rgba);
        FormInputs.AddVerticalSpace(5);
        ImGui.TextUnformatted("Read more...");
        if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            _isDocumentationActive = true;
        } 
        FormInputs.AddVerticalSpace();
        ImGui.PopStyleColor();

        ImGui.Unindent(10);
    }
    

    public static void DrawHelp(SymbolUi symbolUi)
    {
        // Title and namespace
        ImGui.Indent(5);
        FormInputs.AddSectionHeader(symbolUi.Symbol.Name);
        ImGui.PushFont(Fonts.FontSmall);
        ImGui.TextUnformatted("in ");
        ImGui.SameLine();
        ImGui.TextUnformatted(symbolUi.Symbol.Namespace);
        ImGui.PopFont();
        ImGui.Unindent(5);
        FormInputs.SetIndentToLeft();
        FormInputs.AddVerticalSpace();

        // Description
        ImGui.PushFont(Fonts.FontNormal);

        ImGui.Indent(10);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(10, 10));

        if (!string.IsNullOrEmpty(symbolUi.Description))
        {
            ImGui.PushStyleColor(ImGuiCol.Text, UiColors.TextMuted.Rgba);
            ImGui.TextWrapped(symbolUi.Description);

            ImGui.PopStyleColor();
            if (ImGui.IsItemHovered())
            {
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    EditDescriptionDialog.ShowNextFrame();
                }
            }

            CustomComponents.TooltipForLastItem("Click to edit description and links");
        }
        else
        {
            FormInputs.AddHint("No description yet.");
            if (ImGui.Button("Edit description... "))
                EditDescriptionDialog.ShowNextFrame();
        }

        _parametersWithDescription.Clear();

        foreach (var inputUi in symbolUi.InputUis.Values)
        {
            if (!string.IsNullOrEmpty(inputUi.Description))
            {
                _parametersWithDescription.Add(inputUi);
            }
        }

        // Parameter descriptions
        if (_parametersWithDescription.Count > 0)
        {
            FormInputs.AddVerticalSpace(5);

            ImGui.PushStyleColor(ImGuiCol.Text, UiColors.TextMuted.Rgba);
            ImGui.PushFont(Fonts.FontNormal);
            ImGui.TextUnformatted("Parameters details");
            ImGui.PopFont();
            ImGui.PopStyleColor();

            var parameterColorWidth = 150f * T3Ui.UiScaleFactor;
            foreach (var p in _parametersWithDescription)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, UiColors.Text.Rgba);

                var parameterNameWidth = ImGui.CalcTextSize(p.InputDefinition.Name).X;
                ImGui.SetCursorPosX(parameterColorWidth - parameterNameWidth);
                ImGui.TextUnformatted(p.InputDefinition.Name);
                ImGui.PopStyleColor();

                ImGui.PushStyleColor(ImGuiCol.Text, UiColors.TextMuted.Rgba);
                ImGui.SameLine(parameterColorWidth + 10);
                ImGui.TextWrapped(p.Description);
                ImGui.PopStyleColor();
            }
        }

        ImGui.Dummy(Vector2.One);

        DrawExamples(symbolUi);

        ImGui.PopStyleVar();
        ImGui.Unindent();
        ImGui.Dummy(new Vector2(10, 10));
        ImGui.PopFont();
    }

    public static void DrawExamples(SymbolUi symbolUi)
    {
        ImGui.Indent();
        ImGui.PushFont(Fonts.FontSmall);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(5, 5));
        FormInputs.SetIndentToLeft();
        // FormInputs.AddHint("Check the documentation in the header");

        // Draw links
        if (symbolUi.Links.Count > 0)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.PushStyleColor(ImGuiCol.Text, UiColors.TextMuted.Rgba);
            ImGui.TextUnformatted("Links:");
            ImGui.PopStyleColor();
            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Button, Color.Transparent.Rgba);
            foreach (var l in symbolUi.Links.Values)
            {
                if (string.IsNullOrEmpty(l.Url))
                    continue;

                ImGui.PushID(l.Id.GetHashCode());
                ImGui.PushStyleColor(ImGuiCol.Text, UiColors.StatusAutomated.Rgba);
                var title = string.IsNullOrEmpty(l.Title) ? l.Type.ToString() : l.Title;
                var clicked = false;
                if (ExternalLink._linkIcons.TryGetValue(l.Type, out var icon))
                {
                    clicked = ImGui.Button("    " + title);
                    Icons.DrawIconOnLastItem(icon, UiColors.StatusAutomated, 0);
                }
                else
                {
                    clicked = ImGui.Button(title);
                }

                ImGui.PopStyleColor();
                CustomComponents.TooltipForLastItem(!string.IsNullOrEmpty(l.Description) ? l.Description : "Open link in browser", l.Url);

                if (clicked)
                    StartupValidation.OpenUrl(l.Url);

                ImGui.PopID();
                ImGui.SameLine();
            }

            ImGui.Dummy(new Vector2(10, 10));
            ImGui.PopStyleColor();
        }

        var groupLabel = "Also see:";
        var groupLabelShown = false;
        if (ExampleSymbolLinking.ExampleSymbols.TryGetValue(symbolUi.Symbol.Id, out var examplesOpIds))
        {
            DrawGroupLabel(groupLabel);
            groupLabelShown = true;

            foreach (var guid in examplesOpIds)
            {
                const string label = "Example";
                SymbolBrowser.DrawExampleOperator(guid, label);
            }
        }

        if (!string.IsNullOrEmpty(symbolUi.Description))
        {
            var alreadyListedSymbolNames = new HashSet<string>();

            var matches = _itemRegex.Matches(symbolUi.Description);
            if (matches.Count > 0)
            {
                if (!groupLabelShown)
                    DrawGroupLabel(groupLabel);

                foreach (Match match in matches)
                {
                    var referencedName = match.Groups[1].Value;

                    if (referencedName == symbolUi.Symbol.Name)
                        continue;

                    if (alreadyListedSymbolNames.Contains(referencedName))
                        continue;

                    // This is slow and could be optimized by dictionary
                    var referencedSymbolUi = SymbolRegistry.Entries.Values.SingleOrDefault(s => s.Name == referencedName);
                    if (referencedSymbolUi != null)
                    {
                        SymbolBrowser.DrawExampleOperator(referencedSymbolUi.Id, referencedName);
                    }

                    alreadyListedSymbolNames.Add(referencedName);
                }
            }
        }

        ImGui.PopFont();
        ImGui.PopStyleVar();
        ImGui.Unindent();
    }

    private static void DrawGroupLabel(string title)
    {
        ImGui.AlignTextToFramePadding();
        ImGui.PushStyleColor(ImGuiCol.Text, UiColors.TextMuted.Rgba);
        ImGui.TextUnformatted(title);
        ImGui.PopStyleColor();
    }

    internal static readonly EditSymbolDescriptionDialog EditDescriptionDialog = new();
    private static readonly Regex _itemRegex = new(@"\[([A-Za-z\d_]+)\]", RegexOptions.Compiled);

    private static readonly List<IInputUi> _parametersWithDescription = new(10);
    public bool IsActive => _isDocumentationActive;
    private bool _isDocumentationActive = false;
    private static float _timeSinceTooltipHovered = 0;
}