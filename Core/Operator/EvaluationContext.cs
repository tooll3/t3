using System.Diagnostics;
using SharpDX;
using T3.Core.Operator.Interfaces;
using Vector3 = SharpDX.Vector3;

namespace T3.Core.Operator
{
    public class EvaluationContext
    {
        public EvaluationContext()
        {
            Reset();
        }

        public void Reset()
        {
            TimeInBars = GlobalTimeInBars;

        }

        public void SetViewFromCamera(ICamera camera)
        {
            var fov = MathUtil.DegreesToRadians(45);
            float aspectRatio = (float)RequestedResolution.Width / RequestedResolution.Height;
            CameraToClipSpace = Matrix.PerspectiveFovRH(fov, aspectRatio, 0.01f, 1000);

            Vector3 eye = new Vector3(camera.CameraPosition.X, camera.CameraPosition.Y, camera.CameraPosition.Z);
            Vector3 target = new Vector3(camera.CameraTarget.X, camera.CameraTarget.Y, camera.CameraTarget.Z);
            Vector3 up = Vector3.Up;
            WorldToCamera = Matrix.LookAtRH(eye, target, up);

            ObjectToWorld = Matrix.Identity;
        }
        
        private static ICamera _defaultCamera = new ViewCamera();

        private static readonly Stopwatch _runTimeWatch = Stopwatch.StartNew();
        public static double RunTimeInSecs => _runTimeWatch.ElapsedMilliseconds / 1000.0;
        public static double GlobalTimeInBars { get; set; }
        public static double BeatTime { get; set; }
        public double TimeInBars { get; set; }
        public static double GlobalTimeInSecs { get; set; }
        public Size2 RequestedResolution { get; set; }

        public Matrix CameraToClipSpace { get; set; } = Matrix.Identity;
        public Matrix WorldToCamera { get; set; } = Matrix.Identity;
        public Matrix ObjectToWorld { get; set; } = Matrix.Identity;
    }
}