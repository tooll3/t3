#nullable enable
using System.CodeDom.Compiler;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using T3.Core.Operator;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Graph.Helpers;

internal static partial class GraphUtils
{
    public static SyntaxTree? GetSyntaxTree(Symbol symbol)
    {
        var package = (EditorSymbolPackage)symbol.SymbolPackage;
        if (package is not EditableSymbolProject project || !project.TryGetPendingSourceCode(symbol.Id, out var sourceCode))
        {
            // there's intermediate source, so use this
            if (!package.TryGetSourceCodePath(symbol, out var sourceCodePath))
            {
                Log.Error($"Could not find source file for symbol '{symbol.Name}'");
                return null;
            }

            try
            {
                sourceCode = File.ReadAllText(sourceCodePath!);
            }
            catch (Exception e)
            {
                Log.Error($"Error opening file '{sourceCodePath}");
                Log.Error(e.Message);
                return null;
            }
        }

        if (string.IsNullOrWhiteSpace(sourceCode))
        {
            Log.Info("Source was empty, skip compilation.");
            return null;
        }

        return CSharpSyntaxTree.ParseText(sourceCode);
    }

    public static bool IsNewSymbolNameValid(string newSymbolName, Symbol symbol)
    {
        return IsIdentifierValid(newSymbolName)
               && !ReservedWords.Contains(newSymbolName)
               && !symbol.SymbolPackage.ContainsSymbolName(newSymbolName, symbol.Namespace);
    }

    public static bool IsIdentifierValid(string className) => !string.IsNullOrWhiteSpace(className)
                                                              && IdentifierValidator.Value.IsValidIdentifier(className);
    public static bool IsValidProjectName(string userName) => IdentifierValidator.Value.IsValidIdentifier(userName);

    public static bool IsNamespaceValid(string namespaceName) => ValidTypeNameSpacePattern.IsMatch(namespaceName);

    private static readonly Regex ValidTypeNameSpacePattern = NamespaceRegex();
    private static readonly string[] ReservedWords = new string[] { "value", "Var", "instance", "item", "Input", "slot" };
    private static readonly Lazy<CodeDomProvider> IdentifierValidator = new(() => CodeDomProvider.CreateProvider("C#"));

    // FIXME: This lines below are an attempt to replace the following line...
    [GeneratedRegex(@"^([A-Za-z][A-Za-z\d]*)(\.([A-Za-z_][A-Za-z_\d]*))*$")]
    private static partial Regex NamespaceRegex();
}