using ImGuiNET;

namespace T3.Editor.Gui.MagGraph.Ui.Interaction.States;

internal abstract class State(StateMachine s)
{
    protected StateMachine Sm => s;
    
    public virtual void Enter(GraphUiContext context)
    {
    }

    public virtual void Exit(GraphUiContext context)
    {
    }

    public abstract void Update(GraphUiContext context);
}

internal sealed class DefaultState(StateMachine s) : State(s)
{
    public override void Update(GraphUiContext c)
    {
        if (c.LastHoveredItem == null)
            return;
        
        var clickedDown = ImGui.IsMouseClicked(ImGuiMouseButton.Left);
        if (clickedDown)
        {
            Sm.SetState(Sm.HoldingState, c);
        }
    }
}

internal sealed class HoldingState(StateMachine sm) : State(sm)
{
    public override void Update(GraphUiContext context)
    {
        if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            Sm.SetState(Sm.DefaultState,context);
            //return;
        }
        
        

        if (ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            Sm.SetState(Sm.DraggingState,context);
        }
    }
}

internal sealed class DraggingState(StateMachine s) : State(s)
{
    public override void Update(GraphUiContext context)
    {
        if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            Sm.SetState(Sm.DefaultState,context);
            return;
        }

        context.ItemMovement.UpdateDragging(context);
    }
}

