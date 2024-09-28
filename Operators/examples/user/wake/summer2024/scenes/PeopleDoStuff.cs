namespace Examples.user.wake.summer2024.scenes;

[Guid("37cb4087-463a-4790-b3f2-07971d3cf6c3")]
public class PeopleDoStuff : Instance<PeopleDoStuff>
{

    [Output(Guid = "b9da6afd-8569-49fc-926f-67005d720da4")]
    public readonly Slot<T3.Core.DataTypes.Command> Scene = new Slot<T3.Core.DataTypes.Command>();

    [Input(Guid = "c068eed3-5cd3-4d33-96f7-8cf4454310f9")]
    public readonly InputSlot<bool> TriggerName = new InputSlot<bool>();

    [Input(Guid = "555dcc25-3991-4f72-8668-0a96566a2137")]
    public readonly InputSlot<bool> TriggerAction = new InputSlot<bool>();

}