using System.Numerics;

namespace T3.Core.IO
{
    /// <summary>
    /// 
    /// </summary>
    public class MouseInput
    {
        /// <summary>
        /// This needs to be called from Imgui or Program 
        /// </summary>
        public static void Set(Vector2 newPosition, bool isLeftButtonDown)
        {
            _lastPosition = newPosition;
            _isLeftButtonDown = isLeftButtonDown;
        }
        
        public static Vector2 LastPosition => _lastPosition;
        public static bool IsLeftButtonDown => _isLeftButtonDown;

        private static Vector2 _lastPosition = Vector2.Zero;
        private static bool _isLeftButtonDown;       
    }
}