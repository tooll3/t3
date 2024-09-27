namespace lib.exec;

[Guid("e8d2b4ac-0ba2-400f-9c82-e5dd15a23d32")]
public class Once : Instance<Once>
{
    [Output(Guid = "68389552-6d8a-433b-a75f-18e76435519b", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
    public readonly Slot<bool> OutputTrigger = new();

    public Once()
    {
        OutputTrigger.UpdateAction += Update;
    }

    private void Update(EvaluationContext context)
    {
        OutputTrigger.Value = Trigger.DirtyFlag.IsDirty;
        if (Trigger.DirtyFlag.IsDirty)
        {
            Trigger.DirtyFlag.Clear();
        }
    }

    [Input(Guid = "1da5310b-ecad-4f5b-871f-b0321a521ef6")]
    public readonly InputSlot<bool> Trigger = new(true);
}