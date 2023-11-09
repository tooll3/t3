using SharpDX;

namespace T3.Core.Rendering
{
    public interface ICameraPropertiesProvider
    {
        public Matrix CameraToClipSpace { get;  set; }
        public Matrix WorldToCamera { get; set; }
        public Matrix LastObjectToWorld { get; set; }
    }
}