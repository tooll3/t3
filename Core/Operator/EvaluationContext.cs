using System.Diagnostics;
using SharpDX;

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
            ClipSpaceTcamera = Matrix.Identity;
            CameraTworld = Matrix.Identity;
            WorldTobject = Matrix.Identity;
        }

        private static readonly Stopwatch _runTimeWatch = Stopwatch.StartNew();
        public static double RunTimeInSecs => _runTimeWatch.ElapsedMilliseconds / 1000.0;
        public static double GlobalTimeInBars { get; set; }
        //public static double BeatTime { get; set; }
        public  double TimeInBars { get; set; }
        public static double GlobalTimeInSecs { get; set; }
        public Size2 RequestedResolution;

        public Matrix ClipSpaceTcamera { get; set; } = Matrix.Identity;
        public Matrix CameraTworld { get; set; } = Matrix.Identity;
        public Matrix WorldTobject { get; set; } = Matrix.Identity;
    }
}