namespace T3.Core.Logging;

public interface IGuidPathContainer
{
    public IReadOnlyList<Guid> InstancePath { get; }
}