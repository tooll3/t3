using System.IO;
using T3.Core.Compilation;
using T3.Core.Model;

namespace T3.Player;

public class PlayerSymbolPackage(AssemblyInformation assembly) : SymbolPackage(assembly)
{
    public override string Folder => Path.Combine(AssemblyInformation.Directory, "Operators", AssemblyInformation.Name);
}