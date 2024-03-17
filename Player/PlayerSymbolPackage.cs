using System.IO;
using T3.Core.Compilation;
using T3.Core.Model;

namespace T3.Player;

public sealed class PlayerSymbolPackage(AssemblyInformation assembly) : SymbolPackage(assembly)
{
}