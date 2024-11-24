using System.Diagnostics;
using ImGuiNET;

namespace T3.Editor.Gui.MagGraph.Ui.Interaction.States;

internal sealed class DefaultState(StateMachine s) : State(s)
{
    public override void Update(GraphUiContext c)
    {
        if (c.ActiveItem == null)
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
    public override void Enter(GraphUiContext context)
    {
        var item = context.ActiveItem;
        Debug.Assert(item != null);

        var selector = context.Selector;

        var isPartOfSelection = selector.IsSelected(item);
        if (isPartOfSelection)
        {
            context.ItemMovement.SetDraggedItems(selector.Selection);
        }
        else
        {
            context.ItemMovement.SetDraggedItemIdsToSnappedForItem(item);
        }
        
        //context.ItemMovement.StartDragOperation(composition);
    }

    public override void Update(GraphUiContext context)
    {
        Debug.Assert(context.ActiveItem != null);
        
        if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            MagItemMovement.SelectActiveItem(context);
            Sm.SetState(Sm.DefaultState,context);
            return;
        }
        
        if (ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            Sm.SetState(Sm.DraggingState,context);
            return;
        }

        const float longTapDuration = 0.3f;
        var longTapProgress = (float)(Time / longTapDuration);
        MagItemMovement.UpdateLongPressIndicator(longTapProgress);

        if (!(longTapProgress > 1))
            return;
        
        MagItemMovement.SelectActiveItem(context);
        context.ItemMovement.SetDraggedItemIds([context.ActiveItem.Id]);
        Sm.SetState(Sm.HoldingAfterLongTapState, context);

    }
}

internal sealed class HoldingAfterLongTapState(StateMachine sm) : State(sm)
{
    public override void Update(GraphUiContext context)
    {
        Debug.Assert(context.ActiveItem != null);
        
        if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            MagItemMovement.SelectActiveItem(context);
            Sm.SetState(Sm.DefaultState,context);
            return;
        }
        
        if (ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            Sm.SetState(Sm.DraggingState,context);
        }
    }
}



internal sealed class DraggingState(StateMachine s) : State(s)
{
    public override void Enter(GraphUiContext context)
    {
        context.ItemMovement.PrepareDragInteraction();
        context.ItemMovement.StartDragOperation(context);
    }

    public override void Update(GraphUiContext context)
    {
        if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            context.ItemMovement.CompleteDragOperation(context);
            
            Sm.SetState(Sm.DefaultState,context);
            return;
        }

        context.ItemMovement.UpdateDragging(context);
    }

    public override void Exit(GraphUiContext context)
    {
        context.ItemMovement.StopDragOperation();
    }
}

