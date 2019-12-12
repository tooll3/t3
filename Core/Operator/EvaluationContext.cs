using System.Diagnostics;
using SharpDX;

namespace T3.Core.Operator
{
    public class EvaluationContext
    {
        public EvaluationContext()
        {
            Time = GlobalTime;
        }

        public void Reset()
        {
            Time = GlobalTime;
            ClipSpaceTcamera = Matrix.Identity;
            CameraTworld = Matrix.Identity;
            WorldTobject = Matrix.Identity;
        }

        private static readonly Stopwatch _runTimeWatch = Stopwatch.StartNew();
        public static double RunTime => _runTimeWatch.ElapsedMilliseconds / 1000.0;
        public static double GlobalTime { get; set; }
        public static double BeatTime { get; set; }
        public double Time { get; set; }

        public Matrix ClipSpaceTcamera { get; set; } = Matrix.Identity;
        public Matrix CameraTworld { get; set; } = Matrix.Identity;
        public Matrix WorldTobject { get; set; } = Matrix.Identity;
    }
}