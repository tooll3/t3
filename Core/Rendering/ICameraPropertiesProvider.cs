namespace T3.Core.Rendering;

public interface ICameraPropertiesProvider
{
    public Matrix4x4 CameraToClipSpace { get;  set; }
    public Matrix4x4 WorldToCamera { get; set; }
    public Matrix4x4 LastObjectToWorld { get; set; }
}