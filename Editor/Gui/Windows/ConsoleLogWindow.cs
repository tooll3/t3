#nullable enable
using System.Text;
using ImGuiNET;
using T3.Core.DataTypes.Vector;
using T3.Core.Utils;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.SystemUi;
using T3.Editor.UiModel.ProjectHandling;

namespace T3.Editor.Gui.Windows;

/// <summary>
/// Renders the <see cref="ConsoleLogWindow"/>
/// </summary>
internal sealed class ConsoleLogWindow : Window, ILogWriter
{
    internal ConsoleLogWindow()
    {
        Config.Title = "Console";
        Config.Visible = true;
    }

    private readonly List<ILogEntry> _filteredEntries = new(1000);

    protected override void DrawContent()
    {
        if (FrameStats.Last.UiColorsChanged)
            _colorForLogLevel = UpdateLogLevelColors();

        CustomComponents.ToggleButton("Scroll", ref _shouldScrollToBottom, Vector2.Zero);
        ImGui.SameLine();

        //ImGui.SetNextWindowSize(new Vector2(500, 400), ImGuiCond.FirstUseEver);
        if (ImGui.Button("Clear"))
        {
            lock (_logEntries)
            {
                _logEntries.Clear();
            }

            _shouldScrollToBottom = true;

            Log.Info("Console cleared!");
        }

        ImGui.SameLine();

        if (ImGui.Button("Copy"))
        {
            lock (_logEntries)
            {
                var sb = new StringBuilder();
                foreach (var entry in _logEntries)
                {
                    sb.Append($"{(entry.TimeStamp - _startTime).Ticks / 10000000f:  0.000}");
                    sb.Append('\t');
                    sb.Append(entry.Level);
                    sb.Append('\t');
                    sb.Append(entry.Message);
                    sb.Append('\n');
                }

                EditorUi.Instance.SetClipboardText(sb.ToString());
            }
        }

        ImGui.SameLine();
        CustomComponents.DrawInputFieldWithPlaceholder("Filter", ref _filterString);

        ImGui.Separator();
        var itemIndex = 0;
        ImGui.BeginChild("scrolling");
        {
            lock (_logEntries)
            {
                var items = _logEntries;
                if (FilterIsActive)
                {
                    _filteredEntries.Clear();
                    foreach (var e in _logEntries)
                    {
                        if (!e.Message.Contains(_filterString))
                            continue;

                        _filteredEntries.Add(e);
                    }

                    items = _filteredEntries;
                }

                if (ImGui.IsWindowHovered() && ImGui.GetIO().MouseWheel != 0)
                {
                    _shouldScrollToBottom = false;
                }

                unsafe
                {
                    var clipperData = new ImGuiListClipper();
                    var listClipperPtr = new ImGuiListClipperPtr(&clipperData);

                    listClipperPtr.Begin(items.Count, ImGui.GetTextLineHeightWithSpacing());
                    while (listClipperPtr.Step())
                    {
                        for (itemIndex = listClipperPtr.DisplayStart; itemIndex < listClipperPtr.DisplayEnd; ++itemIndex)
                        {
                            if (itemIndex < 0 || itemIndex >= items.Count)
                                continue;

                            DrawEntry(items[itemIndex]);
                        }
                    }

                    listClipperPtr.End();
                }
            }

            ImGui.TextColored(UiColors.Gray, "---"); // Indicator for end
            if (_shouldScrollToBottom)
            {
                ImGui.SetScrollY(ImGui.GetScrollMaxY() + ImGui.GetFrameHeight());
                _isAtBottom = true;
            }
            else
            {
                _isAtBottom = ImGui.GetScrollY() >= ImGui.GetScrollMaxY() - ImGui.GetWindowHeight();
            }

            if (itemIndex < _logEntries.Count)
            {
                var dl = ImGui.GetWindowDrawList();
                var bottomCenter = ImGui.GetWindowPos() + new Vector2(ImGui.GetWindowWidth() * 0.5f, ImGui.GetWindowHeight());
                var lineHeight = ImGui.GetFrameHeight() * 1.4f;
                var min = ImGui.GetWindowPos() + new Vector2(0, ImGui.GetWindowHeight() - lineHeight);
                var max = ImGui.GetWindowPos() + new Vector2(ImGui.GetWindowWidth(), ImGui.GetWindowHeight());
                dl.AddRectFilledMultiColor(min, max,
                                           UiColors.WindowBackground.Fade(0.0f),
                                           UiColors.WindowBackground.Fade(0.0f),
                                           UiColors.WindowBackground.Fade(1.0f),
                                           UiColors.WindowBackground.Fade(1.0f));

                var label = $"{_logEntries.Count - itemIndex} more lines...";
                var labelSize = ImGui.CalcTextSize(label);
                dl.AddText(bottomCenter - new Vector2(labelSize.X * 0.5f, ImGui.GetFrameHeight()), UiColors.Text, label);
            }
        }

        ImGui.EndChild();
    }

    private static double _lastLimeTime;

    public static void DrawEntry(ILogEntry entry)
    {
        var entryLevel = entry.Level;

        var time = entry.SecondsSinceStart;
        var dt = time - _lastLimeTime;

        var fade = 1f;
        var frameFraction = (float)dt / (1.5f / 60f);
        if (frameFraction < 1)
        {
            fade = MathUtils.RemapAndClamp(frameFraction, 0, 1, 0.2f, 0.8f);
        }

        // Timestamp
        var timeColor = UiColors.Text.Fade(fade);
        var timeLabel = $" {time:0.000}";
        var timeLabelSize = ImGui.CalcTextSize(timeLabel);
        ImGui.SetCursorPosX(80 - timeLabelSize.X);
        ImGui.TextColored(timeColor, timeLabel);
        _lastLimeTime = time;
        ImGui.SameLine(90);

        float opacity = 1f;
        var nodeSelection = ProjectManager.Components?.NodeSelection;
        if (nodeSelection != null)
        {
            if (nodeSelection.HoveredIds.Contains(entry.SourceId))
                opacity = 0.8f;
        }

        var color = GetColorForLogLevel(entryLevel).Fade(opacity);

        var lineBreak = entry.Message.IndexOf('\n');
        var hasMessageWithLineBreaks = lineBreak != -1;
        var firstLine = hasMessageWithLineBreaks ? entry.Message.Substring(0, lineBreak) : entry.Message;

        ImGui.TextColored(color, firstLine);

        var hasInstancePath = entry.SourceIdPath.Count > 0;
        if (!IsLineHovered() || (!hasInstancePath && !hasMessageWithLineBreaks))
            return;

        nodeSelection?.HoveredIds.Add(entry.SourceId);

        ProjectView? owner = null;
        var childIdPath = entry.SourceIdPath.ToList();

        if (hasInstancePath)
        {
            var components = ProjectManager.Components;
            if (components != null)
            {
                //var components = graphWindow.Components;
                var hoveredSourceInstance = components.Structure.GetInstanceFromIdPath(childIdPath);
                if (hoveredSourceInstance == null)
                    return;

                owner = components;
            }
        }

        ImGui.BeginTooltip();
        {
            // Show instance details
            if (hasInstancePath)
            {
                if (owner == null)
                {
                    ImGui.Text("Source Instance of message not longer valid");
                }
                else
                {
                    ImGui.TextColored(UiColors.TextMuted, "from ");

                    foreach (var p in owner.Structure.GetReadableInstancePath(childIdPath))
                    {
                        ImGui.SameLine();
                        ImGui.TextColored(UiColors.TextMuted, " / ");

                        ImGui.SameLine();
                        ImGui.Text(p);
                    }
                }
            }

            if (hasMessageWithLineBreaks)
            {
                ImGui.Text(entry.Message);
            }
        }
        ImGui.EndTooltip();

        if (owner == null)
            return;

        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left) && entry.SourceIdPath.Count > 0)
        {
            owner.GraphCanvas.OpenAndFocusInstance(entry.SourceIdPath.ToList());
            if (!string.IsNullOrEmpty(entry.Message))
                EditorUi.Instance.SetClipboardText(entry.Message);
        }
    }

    public static Color GetColorForLogLevel(ILogEntry.EntryLevel entryLevel)
    {
        return _colorForLogLevel.TryGetValue(entryLevel, out var color)
                   ? color
                   : UiColors.TextMuted;
    }

    internal override List<Window> GetInstances()
    {
        return new List<Window>();
    }

    private static bool IsLineHovered()
    {
        if (!ImGui.IsWindowHovered())
            return false;

        var min = new Vector2(ImGui.GetWindowPos().X, ImGui.GetItemRectMin().Y);
        var size = new Vector2(ImGui.GetWindowWidth() - 10, ImGui.GetItemRectSize().Y + LinePadding - 2);
        var lineRect = new ImRect(min, min + size);
        return lineRect.Contains(ImGui.GetMousePos());
    }

    private static Dictionary<ILogEntry.EntryLevel, Color> _colorForLogLevel
        = UpdateLogLevelColors();

    private static Dictionary<ILogEntry.EntryLevel, Color> UpdateLogLevelColors()
    {
        return new()
                   {
                       { ILogEntry.EntryLevel.Debug, UiColors.Text },
                       { ILogEntry.EntryLevel.Info, UiColors.Text },
                       { ILogEntry.EntryLevel.Warning, UiColors.StatusWarning },
                       { ILogEntry.EntryLevel.Error, UiColors.StatusError },
                   };
    }

    public void ProcessEntry(ILogEntry entry)
    {
        lock (_logEntries)
        {
            _logEntries.Add(entry);
        }

        if (_isAtBottom)
        {
            //_shouldScrollToBottom = true;
        }
    }

    public void Dispose()
    {
        _logEntries.Clear();
    }

    public ILogEntry.EntryLevel Filter { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    private bool FilterIsActive => !string.IsNullOrEmpty(_filterString);
    private const float LinePadding = 3;
    private readonly List<ILogEntry> _logEntries = [];
    private bool _shouldScrollToBottom = true;
    private string _filterString = "";
    private bool _isAtBottom = true;
    private readonly DateTime _startTime = DateTime.Now;
}