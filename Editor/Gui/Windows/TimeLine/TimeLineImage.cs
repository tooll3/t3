using System.IO;
using ImGuiNET;
using SharpDX.Direct3D11;
using T3.Core.Audio;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Resource;
using T3.Editor.Gui.Audio;
using Texture2D = T3.Core.DataTypes.Texture2D;

namespace T3.Editor.Gui.Windows.TimeLine
{
    public class TimeLineImage
    {
        public void Draw(ImDrawListPtr drawList, AudioClip? soundTrack, IResourceConsumer instance)
        {
            if (soundTrack == null)
                return;
            UpdateSoundTexture(soundTrack, instance);
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
            
            if (_srv is { IsDisposed: false })
            {
                drawList.AddImage((IntPtr)_srv, 
                                  new Vector2(xMin, yMin), 
                                  new Vector2(xMax, yMin + size.Y));
            }
        }

        private static void UpdateSoundTexture(AudioClip soundtrack, IResourceConsumer instance)
        {
            if (!AudioImageFactory.TryGetOrCreateImagePathForClip(soundtrack, instance, out var imagePath))
            {
                _loadedImagePath = null;
                return;
            }
                
            if (imagePath == _loadedImagePath)
                return;

            _textureResource?.Dispose();
            var resource = ResourceManager.CreateTextureResource(imagePath, null);
            _textureResource = resource;
            
            if (resource.Value != null)
            {
                _loadedImagePath = imagePath;
                _textureResource.Value.CreateShaderResourceView(ref _srv, imagePath);
            }
            
            _loadedImagePath = imagePath;
        }

        private static string _loadedImagePath;
        private static ShaderResourceView _srv;
        private static Resource<Texture2D> _textureResource;
    }
}