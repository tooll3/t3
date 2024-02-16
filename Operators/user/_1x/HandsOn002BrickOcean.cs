using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user._1x
{
	[Guid("e21c202f-9b93-456d-aeba-7232d9600572")]
    public class HandsOn002BrickOcean : Instance<HandsOn002BrickOcean>
    {
        [Output(Guid = "845ab9b2-acd4-413a-9f46-ba31947e42ae")]
        public readonly Slot<Command> Output = new Slot<Command>();


    }
}

