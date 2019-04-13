using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace T3.Logging
{
    /// <summary>
    /// Renders the <see cref="ConsoleLogWindow"/>
    /// </summary>
    public class ConsoleLogWindow : ILogWriter
    {
        public ConsoleLogWindow()
        {
            Log.AddWriter(this);
        }

        public bool Draw(ref bool isOpen)
        {
            while (_logEntries.Count > 1000)
            {
                _logEntries.RemoveAt(0);
            }

            if (ImGui.Begin("Console", ref isOpen))
            {
                ImGui.SetNextWindowSize(new Vector2(500, 400), ImGuiCond.FirstUseEver);
                if (ImGui.Button("Clear"))
                    _logEntries.Clear();

                ImGui.SameLine();
                ImGui.InputText("##Filter", ref _filterString, 100);

                ImGui.Separator();
                ImGui.BeginChild("scrolling");
                {
                    foreach (var entry in _logEntries)
                    {
                        if (_filterIsActive && !entry.Message.Contains(_filterString))
                            continue;

                        ImGui.PushStyleColor(ImGuiCol.Text, _colorForLogLevel[entry.Level]);
                        ImGui.Text(string.Format("{0:0.000}", (entry.TimeStamp - _startTime).Milliseconds / 1000f));
                        ImGui.SameLine(50);
                        ImGui.Text(entry.Message);
                        ImGui.PopStyleColor();
                    }

                    _isAtBottom = ImGui.GetScrollY() >= ImGui.GetScrollMaxY() - 5;
                    if (_shouldScrollToBottom)
                    {
                        ImGui.SetScrollHereY(1);
                        _shouldScrollToBottom = false;
                    }
                }
                ImGui.EndChild();
            }
            ImGui.End();
            return isOpen;
        }

        private Dictionary<LogEntry.EntryLevel, Vector4> _colorForLogLevel = new Dictionary<LogEntry.EntryLevel, Vector4>()
        {
            {LogEntry.EntryLevel.Debug, new Vector4(1,1,1,0.6f) },
            {LogEntry.EntryLevel.Info, new Vector4(1,1,1,0.6f) },
            {LogEntry.EntryLevel.Warning, new Vector4(1,0.5f,0.5f,0.9f) },
            {LogEntry.EntryLevel.Error, new Vector4(1,0.2f,0.2f,1f) },
        };


        public void ProcessEntry(LogEntry entry)
        {
            _logEntries.Add(entry);
            if (_isAtBottom)
            {
                _shouldScrollToBottom = true;
            }
        }

        public void Dispose()
        {
            _logEntries = null;
        }

        private bool _filterIsActive { get { return !string.IsNullOrEmpty(_filterString); } }

        public LogEntry.EntryLevel Filter { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        private StringBuilder _stringBuilder = new StringBuilder();
        private List<LogEntry> _logEntries = new List<LogEntry>();
        private bool _shouldScrollToBottom = true;
        private string _filterString = "";
        private bool _isAtBottom = true;
        private DateTime _startTime = DateTime.Now;
    }
}

