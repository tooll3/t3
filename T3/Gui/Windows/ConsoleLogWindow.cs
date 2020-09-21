using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Windows.Forms;
using T3.Core.Logging;
using UiHelpers;

namespace T3.Gui.Windows
{
    /// <summary>
    /// Renders the <see cref="ConsoleLogWindow"/>
    /// </summary>
    public class ConsoleLogWindow : Window, ILogWriter
    {
        public ConsoleLogWindow() : base()
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
                    lock (_logEntries)
                    {
                        foreach (var entry in _logEntries)
                        {
                            if (FilterIsActive && !entry.Message.Contains(_filterString))
                                continue;

                            var colorHoveredElements = T3Ui.HoveredIdsLastFrame.Contains(entry.SourceId) ? 1 : 0.6f;

                            var color = _colorForLogLevel[entry.Level];
                            color.W = colorHoveredElements;
                            ImGui.PushStyleColor(ImGuiCol.Text, color);
                            var timeInSeconds= (entry.TimeStamp - _startTime).Ticks / 10000000f;
                            // Hack to hide ":" render prefix problem
                            ImGui.SetCursorPosX(-2);
                            
                            // FIXME: It should be possible to pass NULL to avoid prefixing. 
                            // This seems to be broken in imgui.net
                            ImGui.Value("", timeInSeconds);    // Print with ImGui to avoid allocation
                            ImGui.SameLine(80);
                            ImGui.Text(entry.Message);

                            if (IsLineHovered())
                            {
                                T3Ui.AddHoveredId(entry.SourceId);
                            }

                            ImGui.PopStyleColor();
                        }
                    }
                    ImGui.TextColored(Color.Gray, "---");    // Indicator for end
                    if (_shouldScrollToBottom)
                    {
                        ImGui.SetScrollY(ImGui.GetScrollMaxY()+ ImGui.GetFrameHeight()  );
                        _shouldScrollToBottom = false;
                    }
                    _isAtBottom = ImGui.GetScrollY() >= ImGui.GetScrollMaxY() - ImGui.GetFrameHeight();

                }
                ImGui.EndChild();
            }
            ImGui.PopStyleVar();
        }

        public override List<Window> GetInstances()
        {
            return new List<Window>();
        }

        private static bool IsLineHovered()
        {
            var min = new Vector2(ImGui.GetWindowPos().X, ImGui.GetItemRectMin().Y);
            var size = new Vector2(ImGui.GetWindowWidth(), ImGui.GetItemRectSize().Y + LinePadding);
            var lineRect = new ImRect(min, min + size);
            return lineRect.Contains(ImGui.GetMousePos());
        }
        
        private readonly Dictionary<LogEntry.EntryLevel, Vector4> _colorForLogLevel = new Dictionary<LogEntry.EntryLevel, Vector4>()
                                                                                          {
                                                                                              {LogEntry.EntryLevel.Debug, new Vector4(1,1,1,0.6f) },
                                                                                              {LogEntry.EntryLevel.Info, new Vector4(1,1,1,0.6f) },
                                                                                              {LogEntry.EntryLevel.Warning, new Vector4(1,0.5f,0.5f,0.9f) },
                                                                                              {LogEntry.EntryLevel.Error, new Vector4(1,0.2f,0.2f,1f) },
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

