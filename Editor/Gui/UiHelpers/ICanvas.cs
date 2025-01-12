using T3.Core.Operator;
using T3.Editor.Gui.Selection;

namespace T3.Editor.Gui.UiHelpers;
    
/// <summary>
/// A zoomable canvas that can hold <see cref="ISelectableCanvasObject"/> elements.
/// </summary>
public interface ICanvas
{
    /// <summary>
    /// Get integer-aligned screen position applying canvas zoom and scrolling to graph position (e.g. of an Operator) 
    /// </summary>
    Vector2 TransformPosition(Vector2 posOnCanvas);
        
    /// <summary>
    /// Get screen position applying canvas zoom and scrolling to graph position (e.g. of an Operator) 
    /// </summary>
    Vector2 InverseTransformPositionFloat(Vector2 screenPos);

    float InverseTransformX(float x);
    float TransformX(float x);
        
    float InverseTransformY(float y);
    float TransformY(float y);
        
    /// <summary>
    /// Convert a direction (e.g. MouseDelta) from ScreenSpace to Canvas
    /// </summary>
    Vector2 TransformDirection(Vector2 vectorInCanvas);

    /// <summary>
    /// Convert a direction (e.g. MouseDelta) from ScreenSpace to Canvas
    /// </summary>
    Vector2 InverseTransformDirection(Vector2 vectorInScreen);

    ImRect TransformRect(ImRect canvasRect);

    ImRect InverseTransformRect(ImRect screenRect);

    public enum Transition
    {
        JumpIn,
        JumpOut,
        Undefined,
    }

    /// <summary>
    /// This function is called once when zooming in or out of a time clip canvas
    /// </summary>
    void UpdateScaleAndTranslation(Instance compositionOp, Transition transition);

    Vector2 Scale { get; }
    Vector2 Scroll { get; }
    Vector2 WindowSize { get; }
    Vector2 WindowPos { get; }
}