using T3.Core.Compilation;
using T3.Core.Model;
using T3.Core.Operator;

namespace T3.Player;

public class PlayerSymbolData : SymbolData
{
    public PlayerSymbolData(AssemblyInformation assembly) : base(assembly)
    {
    }

    public override string Folder => AssemblyInformation.Directory; // todo: symbols will likely be organized in subfolders
}