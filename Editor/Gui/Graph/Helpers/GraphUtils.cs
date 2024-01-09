using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Graph.Helpers;

internal static class GraphUtils
{
    public static SyntaxTree GetSyntaxTree(Symbol symbol)
    {
        if(symbol.SymbolPackage is not EditableSymbolProject project)
            return null;
        
        var sourceCode = symbol.PendingSource; // there's intermediate source, so use this
        if (string.IsNullOrEmpty(sourceCode))
        {
            if (!project.TryGetSourceCodePath(symbol, out var sourceCodePath))
            {
                Log.Error($"Could not find source file for symbol '{symbol.Name}'");
                return null;
            }
            
            try
            {
                sourceCode = File.ReadAllText(sourceCodePath);
            }
            catch (Exception e)
            {
                Log.Error($"Error opening file '{sourceCodePath}");
                Log.Error(e.Message);
                return null;
            }
        }

        if (string.IsNullOrEmpty(sourceCode))
        {
            Log.Info("Source was empty, skip compilation.");
            return null;
        }

        return CSharpSyntaxTree.ParseText(sourceCode);
    }

    public static bool IsNewSymbolNameValid(string newSymbolName)
    {
        return !string.IsNullOrEmpty(newSymbolName)
               && _validTypeNamePattern.IsMatch(newSymbolName)
               && !SymbolRegistry.Entries.Values.Any(value => string.Equals(value.Name, newSymbolName, StringComparison.OrdinalIgnoreCase));
    }

    private static readonly Regex _validTypeNamePattern = new("^[A-Za-z_]+[A-Za-z0-9_]*$");

    public static bool IsNameSpaceValid(string nameSpaceString)
    {
        return !string.IsNullOrEmpty(nameSpaceString)
               && _validTypeNameSpacePattern.IsMatch(nameSpaceString);
    }

    private static readonly Regex _validTypeNameSpacePattern = new(@"^([A-Za-z][A-Za-z\d]*)(\.([A-Za-z_][A-Za-z_\d]*))*$");

    public static bool IsValidUserName(string userName)
    {
        return UsernameValidator.Value.IsValidIdentifier(userName);
    }

    private static readonly Lazy<CodeDomProvider> UsernameValidator = new(() => CodeDomProvider.CreateProvider("C#"));
}