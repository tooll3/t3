using T3.Core.Compilation;

namespace T3.Core.Model;

public class StaticSymbolData : SymbolData
{
    public StaticSymbolData(AssemblyInformation assembly) : base(assembly)
    {
    }

    public override string Folder => AssemblyInformation.Directory; // todo: symbols will likely be organized in subfolders
}