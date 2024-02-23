using T3.Core.Compilation;
using T3.Core.Operator;

namespace T3.Core.Model;

public class StaticSymbolPackage : SymbolPackage
{
    protected StaticSymbolPackage(AssemblyInformation assembly, bool initializeFileWatcher)
    {
        AssemblyInformation = assembly;
        if (initializeFileWatcher)
            InitializeFileWatcher();
    }
    
    protected override void OnSymbolRemoved (Symbol symbol)
    {
        // do nothing
    }

    public override string Folder => AssemblyInformation.Directory; // todo: symbols will likely be organized in subfolders
    public override bool IsModifiable => false;

    public override AssemblyInformation AssemblyInformation { get; }
    protected override string ResourcesSubfolder => "Resources";
}