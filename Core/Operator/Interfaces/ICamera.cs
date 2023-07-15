using System.Numerics;

namespace T3.Core.Operator.Interfaces
{
    public interface ICamera
    {
        Vector3 CameraPosition { get; set; }
        Vector3 CameraTarget { get; set; }
        float CameraRoll { get; set; }
        
        Matrix4x4 WorldToCamera { get;  }
        Matrix4x4 CameraToClipSpace { get;  }
    }
    
    // Mock view internal fallback camera (if no operator selected)
    // Todo: Find a better location of this class
    public class ViewCamera : ICamera
    {
        public Vector3 CameraPosition { get; set; } = new Vector3(0, 0, 2.416f);
        public Vector3 CameraTarget { get; set; }
        public float CameraRoll { get; set; }
        public Matrix4x4 WorldToCamera { get; }
        public Matrix4x4 CameraToClipSpace { get; }
    }
}