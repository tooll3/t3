using ImGuiNET;

namespace T3.Editor.Gui.MagGraph.States;

internal abstract class State(StateMachine s)
{
    protected StateMachine Sm => s;
    
    public virtual void Enter(GraphUiContext context)
    {
    }

    public virtual void Exit(GraphUiContext context)
    {
    }

    public double EnterTime;
    public double Time => Sm.Time - EnterTime;
    public abstract void Update(GraphUiContext context);

    public override string ToString()
    {
        return GetType().Name.Replace("State", "" );
    }
}

/// <summary>
/// A state machine that controls the interaction of manipulating the magnetic graph.
/// </summary>
// so far I really like the way the states are implemented, with very few caveats.
// I can say what's awesome about it but you already know that
// as for what might warrant some consideration someday (but doesnt seem to at the moment):
//
// Is there a need for a state to know if it is ending itself or being interrupted by another state? would "cleanup" operations differ?
// ideally not, though it seems like that's an implementation detail of each state
//
// a silly way to detect "foreign" state changes would be adding a "caller" object to the SetState method, checking if caller == CurrentState,
// and adding a boolean to the Exit method to indicate if it was called from an object aside from itself. not a secure way to do it when states are "public",
// but a reliable one in absence of straight up sabotage.

internal sealed class StateMachine
{
    // States defined as fields
    internal readonly DefaultState DefaultState;
    internal readonly HoldBackgroundState HoldBackgroundState;
    internal readonly HoldItemState HoldItemState;
    internal readonly DragItemsState DragItemsState;
    internal readonly HoldItemAfterLongTapState HoldItemAfterLongTapState;
    internal readonly PlaceholderState PlaceholderState;
    internal readonly HoldOutputState HoldOutputState;
    internal readonly DragOutputState DragOutputState;
    internal readonly PickInputState PickInputState;

    public StateMachine(GraphUiContext context)
    {
        // Instantiate states
        DefaultState = new DefaultState(this);
        DragItemsState = new DragItemsState(this);
        HoldBackgroundState = new HoldBackgroundState(this);
        HoldItemState = new HoldItemState(this);
        HoldItemAfterLongTapState = new HoldItemAfterLongTapState(this);
        PlaceholderState = new PlaceholderState(this);
        HoldOutputState = new HoldOutputState(this);
        DragOutputState = new DragOutputState(this);
        PickInputState = new PickInputState(this);

        _currentState = DefaultState;
        _currentState.Enter(context);
    }

    public void UpdateAfterDraw(GraphUiContext c)
    {
        _currentState.Update(c);
    }

    internal void SetState(State newState, GraphUiContext context)
    {
        _currentState.Exit(context);
        _currentState = newState;
        newState.EnterTime = Time;
        Log.Debug($"--> {_currentState}  {context.ActiveItem}");
        _currentState.Enter(context);
    }

    private State _currentState;
    public double Time => ImGui.GetTime();
    public State CurrentState => _currentState;
}