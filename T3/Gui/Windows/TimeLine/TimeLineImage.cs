using System;
using System.Numerics;
using ImGuiNET;
using T3.Core;
using T3.Core.Animation;
using T3.Core.Logging;
using T3.Gui.UiHelpers;

namespace T3.Gui.Windows.TimeLine
{
    public class TimeLineImage
    {
        public void Draw(ImDrawListPtr drawlist, Playback playback)
        {
            if (!_initialized)
                LoadSoundImage();
            
            var songDurationInBars = (float)(playback.GetSongDurationInSecs() * playback.Bpm / 240);
            var xMin= TimeLineCanvas.Current.TransformGlobalTime(0);
            var xMax = TimeLineCanvas.Current.TransformGlobalTime(songDurationInBars);

            var size = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin();
            var yMin = (ImGui.GetWindowContentRegionMin() + ImGui.GetWindowPos()).Y;

            var resourceManager = ResourceManager.Instance();
            if (resourceManager.Resources.TryGetValue(_srvResId, out var resource2) && resource2 is ShaderResourceViewResource srvResource)
            {
                drawlist.AddImage((IntPtr)srvResource.ShaderResourceView, 
                                  new Vector2(xMin, yMin), 
                                  new Vector2(xMax, yMin + size.Y));
            }
        }

        public static void LoadSoundImage()
        {
            var resourceManager = ResourceManager.Instance();
            if (resourceManager == null)
                return;

            var imagePath = ProjectSettings.Config.SoundtrackFilepath + ".waveform.png";
            
            (_, _srvResId) = resourceManager.CreateTextureFromFile(imagePath, () => { });
            _initialized = true;
        }

        private static bool _initialized;
        private static uint _srvResId;
    }
}