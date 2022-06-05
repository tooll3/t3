using SharpDX;

namespace T3.Operators.Types.Id_843c9378_6836_4f39_b676_06fd2828af3e
{
    public interface ICameraPropertiesProvider
    {
        public Matrix CameraToClipSpace { get;  set; }
        public Matrix WorldToCamera { get; set; }
        public Matrix LastObjectToWorld { get; set; }
    }
}