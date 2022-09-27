using SharpDX;
using Vector3 = System.Numerics.Vector3;

namespace T3.Core.Operator.Interfaces
{
    public interface ICamera
    {
        Vector3 CameraPosition { get; set; }
        Vector3 CameraTarget { get; set; }
        float CameraRoll { get; set; }
        
        SharpDX.Matrix WorldToCamera { get;  }
        SharpDX.Matrix CameraToClipSpace { get;  }
    }
    
    // Mock view internal fallback camera (if no operator selected)
    // Todo: Find a better location of this class
    public class ViewCamera : ICamera
    {
        public Vector3 CameraPosition { get; set; } = new Vector3(0, 0, 2.416f);
        public Vector3 CameraTarget { get; set; }
        public float CameraRoll { get; set; }
        public Matrix WorldToCamera { get; }
        public Matrix CameraToClipSpace { get; }
    }
}