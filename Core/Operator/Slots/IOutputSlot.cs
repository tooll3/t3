using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace T3.Core.Operator.Slots;

public interface IOutputSlot
{
    Guid Id { get; }
}

public interface IOutputData
{
    Type DataType { get; }

    // serialization
    void ToJson(JsonTextWriter writer);
    void ReadFromJson(JToken json);
    bool Assign(IOutputData outputData);
}