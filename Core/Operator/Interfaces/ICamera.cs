using System.Numerics;

namespace T3.Core.Operator.Interfaces
{
    public interface ICamera
    {
        Vector3 CameraPosition { get; set; }
        Vector3 CameraTarget { get; set; }
        float CameraRoll { get; set; }
    }
}