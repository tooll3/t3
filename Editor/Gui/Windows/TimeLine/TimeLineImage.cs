using ImGuiNET;
using SharpDX.Direct3D11;
using T3.Core.Audio;
using T3.Core.DataTypes;
using T3.Core.Resource;
using T3.Editor.Gui.Audio;
using Texture2D = T3.Core.DataTypes.Texture2D;

namespace T3.Editor.Gui.Windows.TimeLine;

public class TimeLineImage
{
    public void Draw(ImDrawListPtr drawList, AudioClipInfo? soundTrackInfo)
    {
        if (soundTrackInfo == null)
            return;
            
        var soundTrack = soundTrackInfo.Value;
        UpdateSoundTexture(soundTrack);
        if (_loadedImagePath == null)
            return;
            
        var clip = soundTrack.Clip;

        var contentRegionMin = ImGui.GetWindowContentRegionMin();
        var contentRegionMax = ImGui.GetWindowContentRegionMax();
        var windowPos = ImGui.GetWindowPos();
            
        var size = contentRegionMax - contentRegionMin;
        var yMin = (contentRegionMin + windowPos).Y;
            
        // drawlist.AddRectFilled(contentRegionMin + windowPos, 
        //                        contentRegionMax + windowPos, new Color(0,0,0,0.3f));
            
        var songDurationInBars = (float)(clip.LengthInSeconds * clip.Bpm / 240);
        var xMin = TimeLineCanvas.Current.TransformX((float) clip.StartTime);
        var xMax = TimeLineCanvas.Current.TransformX(songDurationInBars + (float)clip.StartTime);
            
        if (_srv is { IsDisposed: false })
        {
            drawList.AddImage((IntPtr)_srv, 
                              new Vector2(xMin, yMin), 
                              new Vector2(xMax, yMin + size.Y));
        }
    }

    private static void UpdateSoundTexture(AudioClipInfo soundtrack)
    {
        if (!AudioImageFactory.TryGetOrCreateImagePathForClip(soundtrack.Clip, soundtrack.Owner, out var imagePath))
        {
            _loadedImagePath = null;
            return;
        }
                
        if (imagePath == _loadedImagePath)
            return;

        _textureResource?.Dispose();
        var resource = ResourceManager.CreateTextureResource(imagePath, soundtrack.Owner);
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