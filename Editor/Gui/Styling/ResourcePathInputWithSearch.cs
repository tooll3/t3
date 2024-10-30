using System.Text.RegularExpressions;
using ImGuiNET;
using T3.Core.DataTypes.Vector;
using T3.Editor.Gui.UiHelpers;
using T3.SystemUi;

namespace T3.Editor.Gui.Styling;

/// <summary>
/// Draws a type ahead input 
/// </summary>
/// <remarks>
/// Sadly, the implementation of this component is a single horrible hack.
/// It's probably the single most ugly piece of ImGui code in the whole codebase.
/// also see:
/// https://github.com/ocornut/imgui/issues/718
/// https://github.com/ocornut/imgui/issues/3725
///
/// It should work for now, but it's likely to break with future versions of ImGui.
/// </remarks>
public static class ResourceInputWithTypeAheadSearch
{
    //public readonly record struct Texts(string DisplayText, string SearchText, string? Tooltip);
    public readonly record struct Args(string Label, IEnumerable<string> Items, bool Warning);
        
    public static bool Draw(Args args, ref string filter, out string selected, bool outlineOnly=false)
    {
        var inputId = ImGui.GetID(args.Label); 
        var isSearchResultWindowOpen = inputId == _activeInputId;

        if (isSearchResultWindowOpen)
        {
            if (ImGui.IsKeyPressed((ImGuiKey)Key.CursorDown, true))
            {
                if (_lastTypeAheadResults.Count > 0)
                {
                    _selectedResultIndex++;
                    _selectedResultIndex %= _lastTypeAheadResults.Count;
                }
            }
            else if (ImGui.IsKeyPressed((ImGuiKey)Key.CursorUp, true))
            {
                if (_lastTypeAheadResults.Count > 0)
                {
                    _selectedResultIndex--;
                    if (_selectedResultIndex < 0)
                        _selectedResultIndex = _lastTypeAheadResults.Count - 1;
                }
            }
        }

        if (outlineOnly)
        {
            ImGui.PushStyleColor(ImGuiCol.FrameBg, Color.Transparent.Rgba);
            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, Color.Red.Rgba);
        }
            
        var color = args.Warning ? UiColors.StatusWarning.Rgba : UiColors.Text.Rgba;
        ImGui.PushStyleColor(ImGuiCol.Text, color);
            
        filter ??= string.Empty;
        var wasChanged = ImGui.InputText(args.Label, ref filter, 256);
            
        ImGui.PopStyleColor();
            
            
        if (outlineOnly)
        {
            var drawList = ImGui.GetWindowDrawList();
            drawList.AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), UiColors.BackgroundInputField, 5);
            ImGui.PopStyleColor(2);
        }
            
        if (ImGui.IsItemActivated())
        {
            _lastTypeAheadResults.Clear();
            _selectedResultIndex = -1;
            THelpers.DisableImGuiKeyboardNavigation();
        }

        var isItemDeactivated = ImGui.IsItemDeactivated();

        // We defer exit to get clicks on opened popup list
        var lostFocus = isItemDeactivated || ImGui.IsKeyDown((ImGuiKey)Key.Esc);
        selected = default;
            
        if ( ImGui.IsItemActive() || isSearchResultWindowOpen)
        {
            _activeInputId = inputId;
            var drawList = ImGui.GetWindowDrawList();

            var lastPosition = new Vector2(ImGui.GetItemRectMin().X, ImGui.GetItemRectMax().Y);
            var size = new Vector2(ImGui.GetItemRectSize().X, 320);
            ImGui.SetNextWindowPos(lastPosition);
            ImGui.SetNextWindowSize(size);
            if (ImGui.IsItemFocused() && ImGui.IsKeyPressed((ImGuiKey)Key.Return))
            {
                wasChanged = true;
                _activeInputId = 0;
            }
                
            const ImGuiWindowFlags flags = ImGuiWindowFlags.NoTitleBar
                                           | ImGuiWindowFlags.NoMove
                                           | ImGuiWindowFlags.Tooltip // ugly as f**k. Sadly .PopUp will lead to random crashes.
                                           | ImGuiWindowFlags.NoFocusOnAppearing;
                
            ImGui.SetNextWindowSize(new Vector2(450,300));
            ImGui.PushStyleColor(ImGuiCol.PopupBg, UiColors.BackgroundFull.Rgba);
            if (ImGui.Begin("##typeAheadSearchPopup", ref isSearchResultWindowOpen,flags))
            {
                
                _lastTypeAheadResults.Clear();
                var index = 0;
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, UiColors.Gray.Rgba);
                    
                var matches = new List<string>();
                var others = new List<string>();
                foreach (var path in args.Items)
                {
                    if(string.IsNullOrEmpty(filter) || path.StartsWith(filter, StringComparison.InvariantCultureIgnoreCase))
                    {
                        matches.Add(path);
                    }
                    else
                    {
                        others.Add(path);
                    }
                }
                    
                var listItems = (!string.IsNullOrWhiteSpace(filter) && matches.Count  <=1) ? others : matches;
                
                var lastProjectGroup = string.Empty;
                
                foreach (var path in listItems)
                {
                    var isSelected = index == _selectedResultIndex;

                    // We can't use IsItemHovered because we need to use Tooltip hack 
                    ImGui.PushStyleColor(ImGuiCol.Text, UiColors.Text.Rgba);

                    // TODO: Optimize regex to be static and better readable
                    var testRegex = new Regex(@"^\/(.+?)\/(.*?)\/([^\/]*)$");
                    
                    var match = testRegex.Match(path);
                    var project = match.Success ? match.Groups[1].Value : string.Empty;
                    var pathInProject = match.Success ? match.Groups[2].Value : path;
                    var filename = match.Success ? match.Groups[3].Value : path;
                    
                    if (project != lastProjectGroup)
                    {
                        if (lastProjectGroup != string.Empty)
                            FormInputs.AddVerticalSpace(8);
                        
                        ImGui.PushFont(Fonts.FontSmall);
                        ImGui.PushStyleColor(ImGuiCol.Text, UiColors.TextMuted.Rgba);
                        ImGui.TextUnformatted( $"/{project}/");
                        ImGui.PopStyleColor();
                        ImGui.PopFont();
                        lastProjectGroup = project;
                    }
                    
                    var lastPos = ImGui.GetCursorPos();
                    ImGui.Selectable( $"##{path}" , isSelected, ImGuiSelectableFlags.None);
                    var isItemHovered = new ImRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax()).Contains( ImGui.GetMousePos());
                    var keepNextPos = ImGui.GetCursorPos();
                    
                    ImGui.SetCursorPos(lastPos);
                    
                    ImGui.PushStyleColor(ImGuiCol.Text, UiColors.TextMuted.Rgba);
                    ImGui.TextUnformatted(pathInProject + "/");
                    ImGui.PopStyleColor();
                    ImGui.SameLine();
                    ImGui.TextUnformatted(filename);
                    
                    ImGui.SetCursorPos(keepNextPos);

                    if (isItemHovered && !string.IsNullOrEmpty(path))
                    {
                        ImGui.BeginTooltip();
                        ImGui.TextUnformatted(path);
                        ImGui.EndTooltip();
                    }
                        
                    ImGui.PopStyleColor();
                            
                    if ((ImGui.IsMouseClicked(ImGuiMouseButton.Left) && isItemHovered) 
                        || (isSelected && ImGui.IsKeyPressed((ImGuiKey)Key.Return)))
                    {
                        filter = path;
                        wasChanged = true;
                        _activeInputId = 0;
                        selected = path;
                    }

                    _lastTypeAheadResults.Add(path);
                    if (++index > 100)
                        break;
                }
                    
                var isPopupHovered = new ImRect(ImGui.GetWindowContentRegionMin(), ImGui.GetWindowContentRegionMax()).Contains(ImGui.GetMousePos());

                if (!isPopupHovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    _activeInputId = 0;
                }

                ImGui.PopStyleColor();
            }

            ImGui.End();
            ImGui.PopStyleColor();
        }

        if (lostFocus)
        {
            THelpers.RestoreImGuiKeyboardNavigation();
        }

        return wasChanged;
    }

    private struct ResultWithRelevancy
    {
        public ResultWithRelevancy(string word, float relevancy)
        {
            Word = word;
            Relevancy = relevancy;
        }

        public string Word;
        public float Relevancy;
    }

    private static readonly List<string> _lastTypeAheadResults = new();
    private static int _selectedResultIndex;
    private static uint _activeInputId;
}