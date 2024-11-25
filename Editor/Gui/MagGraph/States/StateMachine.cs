using ImGuiNET;
using T3.Editor.Gui.MagGraph.Ui;

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
    protected double Time => Sm.Time - EnterTime;
    public abstract void Update(GraphUiContext context);

    public override string ToString()
    {
        return this.GetType().Name.Replace("State", "" );
    }
}

/// <summary>
/// A state machine that controls the interaction of manipulating the magnetic graph.
/// </summary>
internal sealed class StateMachine
{
    // States defined as fields
    internal readonly DefaultState DefaultState;
    internal readonly HoldingBackgroundState HoldingBackgroundState;
    internal readonly HoldingItemState HoldingItemState;
    internal readonly DraggingState DraggingState;
    internal readonly HoldingItemAfterLongTapState HoldingItemAfterLongTapState;

    public StateMachine(GraphUiContext context)
    {
        // Instantiate states
        DefaultState = new DefaultState(this);
        DraggingState = new DraggingState(this);
        HoldingBackgroundState = new HoldingBackgroundState(this);
        HoldingItemState = new HoldingItemState(this);
        HoldingItemAfterLongTapState = new HoldingItemAfterLongTapState(this);

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