using System.Diagnostics;
using SharpDX;
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

            var fov = MathUtil.DegreesToRadians(45);
            float aspectRatio = (float)RequestedResolution.Width / RequestedResolution.Height;
            CameraToClipSpace = Matrix.PerspectiveFovRH(fov, aspectRatio, 0.01f, 1000);

            Vector3 eye = new Vector3(0, 0, 2.416f);
            Vector3 target = Vector3.Zero;
            Vector3 up = Vector3.Up;
            WorldToCamera = Matrix.LookAtRH(eye, target, up);

            ObjectToWorld = Matrix.Identity;
        }

        private static readonly Stopwatch _runTimeWatch = Stopwatch.StartNew();
        public static double RunTimeInSecs => _runTimeWatch.ElapsedMilliseconds / 1000.0;
        public static double GlobalTimeInBars { get; set; }
        public static double BeatTime { get; set; }
        public double TimeInBars { get; set; }
        public static double GlobalTimeInSecs { get; set; }
        public Size2 RequestedResolution;

        public Matrix CameraToClipSpace { get; set; } = Matrix.Identity;
        public Matrix WorldToCamera { get; set; } = Matrix.Identity;
        public Matrix ObjectToWorld { get; set; } = Matrix.Identity;
    }
}