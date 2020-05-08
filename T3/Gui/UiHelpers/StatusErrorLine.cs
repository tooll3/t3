using System.CodeDom.Compiler;
using System.Linq;
using System.Text.RegularExpressions;
using ImGuiNET;
using T3.Core;
using T3.Core.Logging;
using T3.Gui.Styling;
using UiHelpers;

namespace T3.Gui.UiHelpers
{
    /// <summary>
    /// Renders the <see cref="ConsoleLogWindow"/>
    /// </summary>
    public class StatusErrorLine : ILogWriter
    {
        public StatusErrorLine() : base()
        {
            Log.AddWriter(this);
        }


        public void Draw()
        {
            if (_lastEntryTime <=0)
                return;

            var shadeFactor = Color.Mix(Color.Red, new Color(0.3f), 
                                        ((float) (ImGui.GetTime() - _lastEntryTime)/3).Clamp(0,1));
            ImGui.PushStyleColor(ImGuiCol.Text, shadeFactor.Rgba);
            
            ImGui.PushFont(Fonts.FontBold);

            var width = ImGui.CalcTextSize(_errorMessage);
            ImGui.SetCursorPosX(ImGui.GetContentRegionAvail().X - width.X);
            
            ImGui.Text(_errorMessage);
            ImGui.PopFont();
            ImGui.PopStyleColor();
        }
        
        public void Dispose()
        {
            
        }

        public LogEntry.EntryLevel Filter { get; set; }
        public void ProcessEntry(LogEntry entry)
        {
            if (entry.Level != LogEntry.EntryLevel.Error)
                return;

            _errorMessage = ExtractMeaningfulMessage(ref entry);
            _lastEntryTime = ImGui.GetTime();
        }

        private string ExtractMeaningfulMessage(ref LogEntry entry)
        {
            var shaderErrorMatch = ShaderErrorPattern.Match(entry.Message);
            if (shaderErrorMatch.Success)
            {
                var shaderName = shaderErrorMatch.Groups[1].Value;
                var lineNumber = shaderErrorMatch.Groups[2].Value;
                var errorMessage = shaderErrorMatch.Groups[3].Value;

                errorMessage = errorMessage.Split('\n').First();
                
                //var errorMessage = shaderErrorMatch.Groups[3].Value;
                return  $"{errorMessage} >>>> {shaderName}:{lineNumber}";
            }

            return entry.Message;
        }

        
        /// <summary>
        /// Matches errors like....
        ///
        ///  Failed to compile shader 'ComputeWobble': C:\Users\pixtur\coding\t3\Resources\compute-ColorGrade.hlsl(32,12-56): warning X3206: implicit truncation of vector type
        /// Failed to compile shader 'ComputeWobble': C:\Users\pixtur\coding\t3\Resources\compute-ColorGrade.hlsl(32,12-56): warning X3206: implicit truncation of vector type
        /// </summary>
        private static Regex ShaderErrorPattern = new Regex(@"Failed to compile shader.*\\(.*)\.hlsl\((.*)\):(.*)");
        

        private string _errorMessage;
        private double _lastEntryTime;
    }
}