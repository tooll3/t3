using System.Collections.Generic;
using ImGuiNET;
using T3.Gui.Windows.Output;

namespace T3.Gui.Windows
{
    public class RenderSequenceWindow : Window
    {
        public RenderSequenceWindow()
        {
            Config.Title = "Render Sequence";
        }
        
        protected override void DrawContent()
        {
            ImGui.Text("Render Sequence");
            var mainTexture = OutputWindow.GetPrimaryOutputWindow()?.GetCurrentTexture();
            if (ImGui.Button("Save Image"))
            {
                ScreenshotWriter.SaveBufferToFile(mainTexture, "output.jpg");
            }

            ImGui.Text($"Saved: {ScreenshotWriter.LastFilename}");
        }
        
        public override List<Window> GetInstances()
        {
            return new List<Window>();
        }
    }
}