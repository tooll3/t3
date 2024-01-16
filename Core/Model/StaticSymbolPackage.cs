using T3.Core.Compilation;

namespace T3.Core.Model;

public class StaticSymbolPackage : SymbolPackage
{
    
    public StaticSymbolPackage(AssemblyInformation assembly)
    {
        AssemblyInformation = assembly;
    }

    public override string Folder => AssemblyInformation.Directory; // todo: symbols will likely be organized in subfolders
    public override bool IsModifiable => false;

    public override AssemblyInformation AssemblyInformation { get; }
    protected override string ResourcesSubfolder => "Resources";
}