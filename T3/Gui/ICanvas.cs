using ImGuiNET;
using imHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using T3.Gui.Selection;

namespace T3.Gui.Graph
{
    /// <summary>
    /// A zoomable canvas that can hold <see cref="ISelectable"/> elements.
    /// </summary>
    public interface ICanvas
    {
        IEnumerable<ISelectable> SelectableChildren { get; }
        SelectionHandler SelectionHandler { get; }

        /// <summary>
        /// Get screen position applying canas zoom and scrolling to graph position (e.g. of an Operator) 
        /// </summary>
        Vector2 TransformPosition(Vector2 posOnCanvas);

        /// <summary>
        /// Get screen position applying canas zoom and scrolling to graph position (e.g. of an Operator) 
        /// </summary>
        Vector2 InverseTransformPosition(Vector2 screenPos);

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

        Vector2 Scale { get; }
        Vector2 Scroll { get; }
        Vector2 WindowSize { get; }
        Vector2 WindowPos { get; }
        //ImDrawListPtr DrawList { get; }
    }
}
