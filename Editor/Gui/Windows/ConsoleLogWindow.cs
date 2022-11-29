using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Windows.Forms;
using ImGuiNET;
using T3.Core.Logging;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Windows
{
    /// <summary>
    /// Renders the <see cref="ConsoleLogWindow"/>
    /// </summary>
    public class ConsoleLogWindow : Window, ILogWriter
    {
        public ConsoleLogWindow()
        {
            Log.AddWriter(this);
            Config.Title = "Console";
            Config.Visible = true;
        }

        protected override void DrawContent()
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.One * 5);
            {
                lock (_logEntries)
                {
                    while (_logEntries.Count > MaxLineCount)
                    {
                        _logEntries.RemoveAt(0);
                    }
                }

                //ImGui.TextUnformatted(_isAtBottom ? "Bottom!": "Not at bottom");
                CustomComponents.ToggleButton("Scroll", ref _shouldScrollToBottom, Vector2.Zero);
                ImGui.SameLine();

                ImGui.SetNextWindowSize(new Vector2(500, 400), ImGuiCond.FirstUseEver);
                if (ImGui.Button("Clear"))
                {
                    lock (_logEntries)
                    {
                        _logEntries.Clear();
                    }
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
                            sb.Append("\t");
                            sb.Append(entry.Level);
                            sb.Append("\t");
                            sb.Append(entry.Message);
                            sb.Append("\n");
                        }

                        Clipboard.SetText(sb.ToString());
                    }
                }

                ImGui.SameLine();
                ImGui.InputText("##Filter", ref _filterString, 100);
                ImGui.Separator();
                ImGui.BeginChild("scrolling");
                {
                    if (ImGui.IsWindowHovered() && ImGui.GetIO().MouseWheel != 0)
                    {
                        _shouldScrollToBottom = false;
                    }

                    lock (_logEntries)
                    {
                        foreach (var entry in _logEntries)
                        {
                            if (FilterIsActive && !entry.Message.Contains(_filterString))
                                continue;

                            var entryLevel = entry.Level;
                            var color = GetColorForLogLevel(entryLevel)
                               .Fade(T3Ui.HoveredIdsLastFrame.Contains(entry.SourceId) ? 1 : 0.6f);

                            //var timeInSeconds= (entry.TimeStamp - _startTime).Ticks / 10000000f;
                            // Hack to hide ":" render prefix problem
                            ImGui.SetCursorPosX(-2);

                            // FIXME: It should be possible to pass NULL to avoid prefixing. 
                            // This seems to be broken in imgui.net
                            ImGui.Value("", (float)entry.SecondsSinceStart); // Print with ImGui to avoid allocation
                            ImGui.SameLine(80);
                            ImGui.TextColored(color, entry.Message);

                            if (!IsLineHovered() || !(entry.SourceIdPath?.Length > 1))
                                continue;
                            
                            T3Ui.AddHoveredId(entry.SourceId);
                            var childIdPath = entry.SourceIdPath.ToList();
                            var hoveredSourceInstance = NodeOperations.GetInstanceFromIdPath(childIdPath);
                            ImGui.BeginTooltip();
                            {
                                if (hoveredSourceInstance == null)
                                {
                                    ImGui.Text("Source Instance of message not longer valid");
                                }
                                else
                                {
                                    ImGui.TextColored(T3Style.Colors.TextMuted, "from ");
                                    
                                    foreach (var p in NodeOperations.GetReadableInstancePath(childIdPath))
                                    {
                                        ImGui.SameLine();
                                        ImGui.TextColored(T3Style.Colors.TextMuted, " / ");
                                        
                                        ImGui.SameLine();
                                        ImGui.Text(p);
                                    }
                                }
                            }
                            ImGui.EndTooltip();
                            
                            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                            {
                                GraphWindow.GetPrimaryGraphWindow().GraphCanvas.OpenAndFocusInstance(childIdPath);
                            }
                        }
                    }

                    ImGui.TextColored(Color.Gray, "---"); // Indicator for end
                    if (_shouldScrollToBottom)
                    {
                        ImGui.SetScrollY(ImGui.GetScrollMaxY() + ImGui.GetFrameHeight());
                        //_shouldScrollToBottom = false;
                        _isAtBottom = true;
                    }
                    else
                    {
                        _isAtBottom = ImGui.GetScrollY() >= ImGui.GetScrollMaxY() - ImGui.GetWindowHeight();
                    }
                }
                ImGui.EndChild();
            }
            
            ImGui.PopStyleVar();
        }

        public static Color GetColorForLogLevel(LogEntry.EntryLevel entryLevel)
        {
            return _colorForLogLevel.TryGetValue(entryLevel, out var color)
                       ? color
                       : T3Style.Colors.TextMuted;
        }

        public override List<Window> GetInstances()
        {
            return new List<Window>();
        }

        private static bool IsLineHovered()
        {
            if (!ImGui.IsWindowHovered())
                return false;
            
            var min = new Vector2(ImGui.GetWindowPos().X, ImGui.GetItemRectMin().Y);
            var size = new Vector2(ImGui.GetWindowWidth(), ImGui.GetItemRectSize().Y + LinePadding);
            var lineRect = new ImRect(min, min + size);
            return lineRect.Contains(ImGui.GetMousePos());
        }

        private static readonly Dictionary<LogEntry.EntryLevel, Color> _colorForLogLevel = new Dictionary<LogEntry.EntryLevel, Color>()
                                                                                               {
                                                                                                   { LogEntry.EntryLevel.Debug, new Color(1, 1, 1, 0.6f) },
                                                                                                   { LogEntry.EntryLevel.Info, new Color(1, 1, 1, 0.6f) },
                                                                                                   {
                                                                                                       LogEntry.EntryLevel.Warning,
                                                                                                       new Color(1, 0.5f, 0.5f, 0.9f)
                                                                                                   },
                                                                                                   { LogEntry.EntryLevel.Error, new Color(1, 0.2f, 0.2f, 1f) },
                                                                                               };

        public void ProcessEntry(LogEntry entry)
        {
            lock (_logEntries)
            {
                _logEntries.Add(entry);
            }

            if (_isAtBottom)
            {
                _shouldScrollToBottom = true;
            }
        }

        public void Dispose()
        {
            _logEntries = null;
        }

        private const int MaxLineCount = 250;

        public LogEntry.EntryLevel Filter { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        private bool FilterIsActive => !string.IsNullOrEmpty(_filterString);
        private const float LinePadding = 3;
        private List<LogEntry> _logEntries = new List<LogEntry>();
        private bool _shouldScrollToBottom = true;
        private string _filterString = "";
        private bool _isAtBottom = true;
        private readonly DateTime _startTime = DateTime.Now;
    }
}