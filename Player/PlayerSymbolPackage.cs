using System.IO;
using T3.Core.Compilation;
using T3.Core.Model;

namespace T3.Player;

public class PlayerSymbolPackage : StaticSymbolPackage
{
    public PlayerSymbolPackage(AssemblyInformation assembly) : base(assembly, true)
    {
    }
    
    public override string Folder => Path.Combine(AssemblyInformation.Directory, "Operators", AssemblyInformation.Name); 
}