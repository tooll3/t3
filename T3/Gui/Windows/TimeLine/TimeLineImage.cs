using System;
using System.IO;
using System.Numerics;
using Core.Audio;
using ImGuiNET;
using T3.Core;
using T3.Core.Animation;
using T3.Core.IO;
using T3.Core.Logging;
using t3.Gui.Audio;
using T3.Gui.UiHelpers;

namespace T3.Gui.Windows.TimeLine
{
    public class TimeLineImage
    {
        public void Draw(ImDrawListPtr drawList, AudioClip soundTrack)
        {
            UpdateSoundTexture(soundTrack);
            if (_loadedImagePath == null)
                return;

            var contentRegionMin = ImGui.GetWindowContentRegionMin();
            var contentRegionMax = ImGui.GetWindowContentRegionMax();
            var windowPos = ImGui.GetWindowPos();
            
            var size = contentRegionMax - contentRegionMin;
            var yMin = (contentRegionMin + windowPos).Y;
            
            // drawlist.AddRectFilled(contentRegionMin + windowPos, 
            //                        contentRegionMax + windowPos, new Color(0,0,0,0.3f));
            
            var songDurationInBars = (float)(soundTrack.LengthInSeconds * soundTrack.Bpm / 240);
            var xMin = TimeLineCanvas.Current.TransformX((float) soundTrack.StartTime);
            var xMax = TimeLineCanvas.Current.TransformX(songDurationInBars + (float)soundTrack.StartTime);
            
            var resourceManager = ResourceManager.Instance();
            if (ResourceManager.ResourcesById.TryGetValue(_srvResId, out var resource2) && resource2 is ShaderResourceViewResource srvResource)
            {
                drawList.AddImage((IntPtr)srvResource.ShaderResourceView, 
                                  new Vector2(xMin, yMin), 
                                  new Vector2(xMax, yMin + size.Y));
            }
        }

        private static void UpdateSoundTexture(AudioClip soundtrack)
        {
            var imagePath = AudioImageFactory.GetOrCreateImagePathForClip(soundtrack);
            if (imagePath == null  || !File.Exists(imagePath))
            {
                _loadedImagePath = null;
                return;
            }
                
            if (imagePath == _loadedImagePath)
                return;
            
            var resourceManager = ResourceManager.Instance();
            if (resourceManager == null)
                return;

            (_, _srvResId) = resourceManager.CreateTextureFromFile(imagePath, () => { });
            _loadedImagePath = imagePath;
        }

        private static string _loadedImagePath;
        private static uint _srvResId;
    }
}