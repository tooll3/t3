#nullable enable
using System.Text;
using ImGuiNET;
using T3.Editor.Gui.MagGraph.States;
using T3.Editor.Gui.Styling;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.MagGraph.Interaction;

internal static class SymbolBrowsing
{
    internal static PlaceHolderUi.UiResults Draw(GraphUiContext context)
    {
        var uiResult = PlaceHolderUi.UiResults.None;

        _libTree = UpdateLibPage(); // only required for hot code reloading

        if (_path.Count == 0)
            return PlaceHolderUi.UiResults.Cancel;

        ImGui.TextUnformatted(GetNamespaceString(_path));
        
        if (ImGui.IsItemClicked())
        {
            Reset();
        }
        
        WindowContentExtend.ExtendToLastItem();
        FormInputs.AddVerticalSpace(5);

        ImGui.GetCursorPosY();

        // Find active page
        _activePage = _path[0];

        for (var index = _path.Count - 1; index >= 0; index--)
        {
            var group = _path[index];
            if (group.Variant == Variants.Page)
                _activePage = group;
        }

        var lastCurrentGroup = _path[^1];

        ColumnLayout.StartLayout();

        if (lastCurrentGroup.Variant == Variants.Page || lastCurrentGroup.Variant == Variants.Project)
        {
            DrawItemTree(_path, _activePage);
        }
        else
        {
            foreach (var symbolUi in EditorSymbolPackage.AllSymbolUis)
            {
                if (symbolUi.Symbol.Namespace != ActiveNamespaceString)
                    continue;
                
                uiResult |= PlaceHolderUi.DrawSymbolUiEntry(context, symbolUi);
                WindowContentExtend.ExtendToLastItem();
            }            
        }
        
        return uiResult;

        void DrawItemTree(List<Group> groupPath, Group group, int levelOnPage = 0)
        {
            switch (group.Variant)
            {
                case Variants.Project:
                {
                    foreach (var subItem in group.Items)
                    {
                        DrawItemTree([group], subItem, levelOnPage + 1);
                    }

                    break;
                }

                case Variants.Page when levelOnPage == 0:
                {
                    foreach (var subItem in group.Items)
                    {
                        DrawItemTree([..groupPath], subItem, levelOnPage + 1);
                    }

                    break;
                }
                case Variants.Page:
                {
                    ImGui.TextUnformatted($"{group.Name}...");
                    if (ImGui.IsItemClicked())
                    {
                        _path.Clear();
                        _path.AddRange([..groupPath, group]);
                        ImGui.SetScrollY(0);
                    }

                    break;
                }
                case Variants.Namespace when levelOnPage == 1:
                {
                    //var groupNamespace = string.Join(".", [..path, group.Name]);
                    var groupNamespace = GetNamespaceString([..groupPath, group]);

                    var matchingSymbolUis = new List<SymbolUi>();
                    foreach (var s in EditorSymbolPackage.AllSymbolUis)
                    {
                        if (s.Symbol.Namespace == groupNamespace && s.Tags.HasFlag(SymbolUi.SymbolTags.Essential))
                            matchingSymbolUis.Add(s);
                    }

                    if (matchingSymbolUis.Count == 0)
                        break;

                    ColumnLayout.StartGroupAndWrapIfRequired(matchingSymbolUis.Count + 1);

                    DrawGroupHeader(group);
                    foreach (var symbolUi in matchingSymbolUis)
                    {
                        ImGui.SetNextItemWidth(100);
                        uiResult |= PlaceHolderUi.DrawSymbolUiEntry(context, symbolUi);
                        // if (result.HasFlag(PlaceHolderUi.UiResults.Create))
                        //     _selectedSymbolUi = symbolUi;

                        WindowContentExtend.ExtendToLastItem();
                        ColumnLayout.ExtendWidth(ImGui.GetItemRectSize().X);
                    }

                    break;
                }
                case Variants.Namespace:
                    ImGui.TextUnformatted(group.Name + "/");
                    WindowContentExtend.ExtendToLastItem();
                    if (ImGui.IsItemClicked())
                    {
                        _path.Clear();
                        _path.AddRange([..groupPath, group]);
                        ImGui.SetScrollY(0);
                    }
                    break;

                case Variants.Grouping:
                case Variants.NamespaceCategory:
                {
                    ColumnLayout.StartGroupAndWrapIfRequired(group.Items.Count + 1);
                    DrawGroupHeader(group);
                    //var maxWidth = 0f;
                    foreach (var subItem in group.Items)
                    {
                        // var subPath = group.Variant == Variants.Grouping 
                        //                   ?groupPath 
                        //                   : [..groupPath, group.Name];
                        
                        DrawItemTree([..groupPath, group], subItem, levelOnPage + 1);
                        WindowContentExtend.ExtendToLastItem();
                        ColumnLayout.ExtendWidth(ImGui.GetItemRectSize().X);
                        //maxWidth = MathF.Max(ImGui.GetItemRectSize().X, maxWidth);
                    }

                    // ImGui.SetCursorPosY(_columnAreaMinY);
                    // ImGui.Indent(maxWidth + 20);
                    break;
                }
            }
        }
    }

    private static string GetNamespaceString(List<Group> groupPath)
    {
        _stringBuilder.Clear();
        foreach (var g in groupPath)
        {
            if (g.Variant == Variants.Grouping)
                continue;

            if (_stringBuilder.Length > 0)
                _stringBuilder.Append(".");

            _stringBuilder.Append(g.Name);
        }

        return _stringBuilder.ToString();
    }

    private static string ActiveNamespaceString => GetNamespaceString(_path); 
    

    private static readonly StringBuilder _stringBuilder = new();

    private static List<Group> _path = [];

    private static void DrawGroupHeader(Group group)
    {
        ImGui.PushFont(Fonts.FontSmall);
        ImGui.PushStyleColor(ImGuiCol.Text, UiColors.TextMuted.Rgba);
        ImGui.TextUnformatted(group.Name + "...");
        ImGui.PopStyleColor();
        ImGui.PopFont();

        WindowContentExtend.ExtendToLastItem();
    }
    
    private static Group UpdateLibPage()
    {
        return new Group(SymbolBrowsing.Variants.Project, "Lib",
            [
                new Group(Variants.NamespaceCategory, "numbers", [
                        new Group(Variants.Namespace, "anim"),
                        new Group(Variants.Page, "float", [
                                new Group(Variants.Namespace, "basic"),
                                new Group(Variants.Namespace, "trigonometry"),
                                new Group(Variants.Namespace, "adjust"),
                                new Group(Variants.Namespace, "process"),
                                new Group(Variants.Namespace, "logic"),
                                new Group(Variants.Namespace, "random"),
                            ]),
                        new Group(Variants.Namespace, "vec2"),
                        new Group(Variants.Namespace, "vec3"),
                        new Group(Variants.Namespace, "color"),
                        new Group(Variants.Namespace, "bool"),
                        new Group(Variants.Namespace, "int")
                    ]),
                new Group(Variants.NamespaceCategory, "render", [
                        new Group(Variants.Namespace, "basic"),
                        new Group(Variants.Namespace, "camera"),
                        new Group(Variants.Namespace, "transform"),
                        new Group(Variants.Namespace, "shading"),
                        new Group(Variants.Namespace, "postfx"),
                        new Group(Variants.Namespace, "gizmo"),
                        new Group(Variants.Namespace, "utils"),
                    ]),
                new Group(Variants.NamespaceCategory, "image", [
                        new Group(Variants.Page, "generate", [
                                new Group(Variants.Namespace, "basic"),
                                new Group(Variants.Namespace, "load"),
                                new Group(Variants.Namespace, "noise"),
                                new Group(Variants.Namespace, "pattern"),
                            ]),
                        new Group(Variants.Page, "fx", [
                                new Group(Variants.Namespace, "blur"),
                                new Group(Variants.Namespace, "distort"),
                                new Group(Variants.Namespace, "glitch"),
                                new Group(Variants.Namespace, "stylize"),
                            ]),
                        new Group(Variants.Namespace, "color"),
                        new Group(Variants.Namespace, "transform"),
                        new Group(Variants.Namespace, "analyze"),
                        new Group(Variants.Namespace, "use"),
                    ]),
                new Group(Variants.Grouping, "misc", [
                        new Group(Variants.Page, "string", [
                                new Group(Variants.Namespace, "combine"),
                                new Group(Variants.Namespace, "convert"),
                                new Group(Variants.Namespace, "search"),
                                new Group(Variants.Namespace, "logic"),
                                new Group(Variants.Namespace, "datetime"),
                                new Group(Variants.Namespace, "list"),
                                new Group(Variants.Namespace, "buffers"),
                            ]),
                        new Group(Variants.Page, "io", [
                                new Group(Variants.Namespace, "audio"),
                                new Group(Variants.Namespace, "file"),
                                new Group(Variants.Namespace, "input"),
                                new Group(Variants.Namespace, "midi"),
                                new Group(Variants.Namespace, "osc"),
                                new Group(Variants.Namespace, "video"),
                            ]),
                        new Group(Variants.Namespace, "flow"),
                    ]),
            ]);
    }

    private static Group _libTree = UpdateLibPage();
    private static Group? _activePage;

    private sealed class Group
    {
        internal Group(SymbolBrowsing.Variants variant, string name, List<Group>? items = null)
        {
            Variant = variant;
            Name = name;
            Items = items?? [];
        }

        internal Variants Variant { get; }
        internal string Name { get; }
        internal List<Group> Items { get; }

        public override string ToString()
        {
            return $"{Variant} {Name}  {Items.Count}";
        }
    }

    private enum Variants
    {
        Project,
        Page,
        NamespaceCategory,
        Namespace,
        Grouping,
    }

    public static void Reset()
    {
        _path = [_libTree];
    }
}

/// <summary>
/// A small helper that helps to layout item blocks in a flexible column grid.
/// </summary>
/// <remarks>
/// Using ImGui's Columns would require a pre-pass to calculate the count and width of columns.
/// This method basically sets the cursor-position to "fake" a similar thing. Obviously this
/// can't be nested.
/// </remarks>
internal static class ColumnLayout
{
    internal static void StartLayout()
    {
        _layoutStartPosY = ImGui.GetCursorPosY();
        _currentColumnWidth = 0;
    }

    internal static void StartGroupAndWrapIfRequired(int lineCountInGroup)
    {
        var y = ImGui.GetCursorPosY() - _layoutStartPosY;
        var isFirstBlock = y < 10;
        if (isFirstBlock)
            return;

        var requiredHeight = y + ImGui.GetFrameHeight() * lineCountInGroup;
        var wrapHeight = _wrapLineCount * ImGui.GetFrameHeight();
        if (requiredHeight < wrapHeight)
            return;

        // Start next column
        ImGui.SetCursorPosY(_layoutStartPosY);
        ImGui.Indent(_currentColumnWidth + Padding);
        _currentColumnWidth = 0;
    }

    internal static void ExtendWidth(float itemWidth)
    {
        _currentColumnWidth = MathF.Max(_currentColumnWidth, itemWidth);
    }

    private static readonly float _wrapLineCount = 10;
    private static float _layoutStartPosY;
    private static float _currentColumnWidth;
    private static float Padding => 30 * T3Ui.UiScaleFactor;
}

internal static class WindowContentExtend
{
    internal static void ExtendToLastItem()
    {
        _currentExtend = Vector2.Max(_currentExtend, ImGui.GetItemRectMax() - ImGui.GetWindowPos()) + new Vector2(ImGui.GetScrollX(), ImGui.GetScrollY());
    }

    internal static Vector2 GetLastAndReset()
    {
        var lastExtend = _currentExtend;
        _currentExtend = Vector2.Zero;
        return lastExtend;
    }

    private static Vector2 _currentExtend = Vector2.Zero;
}