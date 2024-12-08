using System.Reflection;
using ImGuiNET;

namespace T3.Editor.Gui.MagGraph.States;

internal readonly record struct State(Action<GraphUiContext> Enter, Action<GraphUiContext> Update, Action<GraphUiContext> Exit);


/// <summary>
/// The state machine is a very bare-bones (no hierarchy or events) implementation
/// of a state machine that handles activation of <see cref="State"/>s. There can only be one state active.
/// Most of the update interaction is done in State.Update() overrides.
/// </summary>
internal sealed class StateMachine
{
    public StateMachine(GraphUiContext context)
    {
        _currentState = GraphStates.Default;
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
        _stateEnterTime = ImGui.GetTime();
        
        Log.Debug($"--> {GetMatchingStateFieldName(typeof( GraphStates), _currentState)}  {context.ActiveItem}");
        _currentState.Enter(context);
    }

    /// <summary>
    /// Sadly, since states are defined as fields, we need to use reflection to infer their short names... 
    /// </summary>
    private static string GetMatchingStateFieldName(Type staticClassType, State state)
    {
        if (!staticClassType.IsClass || !staticClassType.IsAbstract || !staticClassType.IsSealed)
            throw new ArgumentException("Provided type must be a static class.", nameof(staticClassType));

        var fields = staticClassType.GetFields(BindingFlags.Public | BindingFlags.Static);
    
        foreach (var field in fields)
        {
            if (field.FieldType != typeof(State))
                continue;
            
            var fieldValue = (State)field.GetValue(null)!;
            if (fieldValue.Equals(state))
                return field.Name;
        }

        return string.Empty; 
    }
    
    private State _currentState;
    public float StateTime => (float) (ImGui.GetTime() - _stateEnterTime);
    private double _stateEnterTime;
    public State CurrentState => _currentState;
}