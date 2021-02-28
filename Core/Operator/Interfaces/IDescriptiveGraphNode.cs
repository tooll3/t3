namespace T3.Core.Operator.Interfaces
{
    /// <summary>
    /// Provides information required to render additional information in Graph nodes 
    /// </summary>
    public interface IDescriptiveGraphNode
    {
        string GetDescriptiveString();
    }
}