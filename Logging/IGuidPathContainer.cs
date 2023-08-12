namespace T3.Core.Logging;

public interface IGuidPathContainer
{
    public IList<Guid> InstancePath { get; }
}