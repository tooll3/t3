namespace T3.Editor.Gui.MagGraph.Ui.Interaction.States;

/// <summary>
/// A state machine that controls the interaction of manipulating the magnetic graph.
/// </summary>
internal sealed class StateMachine
{
    // States defined as fields
    internal readonly DefaultState DefaultState;
    internal readonly HoldingState HoldingState;
    internal readonly DraggingState DraggingState;
    
    public StateMachine(GraphUiContext context)
    {
        // Instantiate states
        DefaultState = new DefaultState(this);
        DraggingState = new DraggingState(this);
        HoldingState = new HoldingState(this);

        _currentState = DefaultState;
        _currentState.Enter(context);
    }

    public void UpdateAfterDraw(GraphUiContext c)
    {
        _currentState.Update(c);
        c.ItemMovement.CompleteFrame();
    }

    internal void SetState(State newState, GraphUiContext context)
    {
        Log.Debug($"Exit {_currentState}...");
        _currentState.Exit(context);
        _currentState = newState;
        Log.Debug($"Enter {_currentState}...");
        _currentState.Enter(context);
    }

    private State _currentState;
}