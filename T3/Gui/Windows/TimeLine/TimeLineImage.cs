using System;
using System.IO;
using System.Numerics;
using ImGuiNET;
using T3.Core;

namespace T3.Gui.Windows.TimeLine
{
    public class TimeLineImage
    {
        public void Draw(ImDrawListPtr drawlist)
        {
            if (!_initialized)
                Initialize();

            const float songDuration = 177.78f;
            var xMin= TimeLineCanvas.Current.TransformPositionX(0);
            var xMax = TimeLineCanvas.Current.TransformPositionX(songDuration);

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

        private void Initialize()
        {
            var resourceManager = ResourceManager.Instance();
            if (resourceManager == null)
                return;
            
            (_, _srvResId) = resourceManager.CreateTextureFromFile(ImagePath, () => { });
            _initialized = true;
        }

        private bool _initialized = false;
        private static uint _srvResId;
        private static readonly string ImagePath =  Path.Combine(ResourceManager.ResourcesFolder, "soundtrack","lorn-sega-sunset.mp3.waveform.png");
    }
}