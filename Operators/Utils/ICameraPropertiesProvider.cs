using SharpDX;

namespace T3.Operators.Utils
{
    public interface ICameraPropertiesProvider
    {
        public Matrix CameraToClipSpace { get;  set; }
        public Matrix WorldToCamera { get; set; }
        public Matrix LastObjectToWorld { get; set; }
    }
}