using System;
using System.IO;
using System.Numerics;
using ImGuiNET;
using T3.Core.Audio;
using T3.Core.Operator;
using T3.Core.Resource;
using T3.Editor.Gui.Audio;

namespace T3.Editor.Gui.Windows.TimeLine
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
            if (imagePath == null)
            {
                _loadedImagePath = null;
                return;
            }
                
            if (imagePath == _loadedImagePath)
                return;
            
            var resourceManager = ResourceManager.Instance();

            (_, _srvResId) = resourceManager.CreateTextureFromFile(imagePath, null, () => { });
            _loadedImagePath = imagePath;
        }

        private static string _loadedImagePath;
        private static uint _srvResId;
    }
}