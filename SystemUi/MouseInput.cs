using System.Numerics;

namespace T3.SystemUi;

/// <summary>
/// 
/// </summary>
public static class MouseInput
{
    /// <summary>
    /// This needs to be called from Imgui or Program 
    /// </summary>
    public static void Set(Vector2 newPosition, bool isLeftButtonDown)
    {
        LastPosition = newPosition;
        IsLeftButtonDown = isLeftButtonDown;
    }
        
    public static Vector2 LastPosition { get; private set; } = Vector2.Zero;
    public static bool IsLeftButtonDown { get; private set; }
    public static Guid SelectedChildId = Guid.Empty;
}