#nullable enable
using T3.Core.Operator;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Commands.Graph;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.MagGraph.Interaction;
using T3.Editor.Gui.MagGraph.Model;
using T3.Editor.Gui.MagGraph.Ui;
using MagItemMovement = T3.Editor.Gui.MagGraph.Interaction.MagItemMovement;

namespace T3.Editor.Gui.MagGraph.States;


/// <summary>
/// Overall concept of the graph UI system.
///
/// Preface: The node graph is a central piece of Tooll's user interface and probably
/// one of the most complex: For the implementation, we tried to balance the following:
/// - Code should be well-structured and readable.
/// - There should be no mud-ball side effects that lead to inconsistent states and hard-to-reproduce issues.
/// - All interactions should support combined undo/redo out of the box.
/// - The system should be open to implement new features, tweak the design, or adjust behaviors.
/// - Ideally, rendering should be fast.
/// - Cycle checks should be consistent and bulletproof.
/// - Allocations should be avoided if possible.
///
/// The basic components are:
/// - <see cref="Layout"/> holds an intermediate view model that is updated if required. This view model
/// builds referenceable items, view elements, and connections. This makes it much easier to traverse the
/// graph without dictionary lookups. The layout also precomputes the visibility of input links, which simplifies
/// the layout and rendering of connection lines (one of the most complicated parts of the legacy layout).
/// - <see cref="GraphUiContext"/> holds the current interaction state of the graph. It is passed as a parameter
/// during most processing and makes "graph-global" components and states accessible to all related components.
/// New instances of the context are created when the composition object or window changes.
/// - <see cref="StateMachine"/> the state machine is a very bare-bones (no hierarchy or events) implementation
/// of a state machine that handles activation of <see cref="State"/>s. There can only be one state active.
/// Most of the update interaction is done in State.Update() overrides.
/// - <see cref="MagGraphCanvas"/> is a scalable canvas that handles drawing. The Layout sometimes resets
/// the current state.
/// - <see cref="PlaceholderCreation"/> combines drawing and logic that handles creating new operators (it's the
/// "new" SymbolBrowser).
/// - <see cref="ItemMovement"/> handles dragging, snapping, inserting, and unsnapping operators on the canvas.
/// Grouping is handled on the fly by flood-filling "Snapped" Layout-Connections into HashSets.
/// </summary>
internal sealed class GraphUiContext
{
    internal GraphUiContext(NodeSelection selector, MagGraphCanvas canvas, Instance compositionOp)
    {
        Selector = selector;
        Canvas = canvas;
        CompositionOp = compositionOp;
        ItemMovement = new MagItemMovement(this, canvas, Layout, selector);
        StateMachine = new StateMachine(this);
        Placeholder = new PlaceholderCreation();
    }

    internal readonly MagGraphCanvas Canvas;
    internal readonly MagItemMovement ItemMovement;
    internal readonly PlaceholderCreation Placeholder;
    internal readonly MagGraphLayout Layout = new();

    internal readonly NodeSelection Selector;
    internal readonly Instance CompositionOp;
    internal readonly StateMachine StateMachine;
    internal  MacroCommand? MacroCommand;
    internal  ModifyCanvasElementsCommand? MoveElementsCommand;
    internal MagGraphItem? ActiveItem { get; set; }
    internal Guid ActiveOutputId { get; set; }
    
    // if Imgui's input pointers are safe to reference throughout the duration of the frame, i would add the Imgui input state here.
    // that way states do not need to query them and other sorts of inputs (e.g. native non-imgui inputs for things like touch devices)
    // could be added at some point in the future

    // Picking...
    internal Type? DraggedPrimaryOutputType;
    internal MagGraphItem? ItemForInputSelection;
    internal MagGraphItem? PrimaryOutputItem;
    internal Vector2 PeekAnchorInCanvas;
    internal bool ShouldAttemptToSnapToInput;

    // internal Vector2 PeekAnchorInCanvas => PrimaryOutputItem == null
    //                                            ? Vector2.Zero
    //                                            : new Vector2(PrimaryOutputItem.Area.Max.X - MagGraphItem.GridSize.Y * 0.25f,
    //                                                          PrimaryOutputItem.Area.Min.Y + MagGraphItem.GridSize.Y * 0.5f);

    
    internal readonly List<MagGraphConnection> TempConnections = [];
}