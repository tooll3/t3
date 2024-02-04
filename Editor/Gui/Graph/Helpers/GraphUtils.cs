using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using T3.Core.Logging;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Graph.Helpers;

internal static class GraphUtils
{
    public static SyntaxTree GetSyntaxTree(Symbol symbol)
    {
        if (symbol.SymbolPackage is not EditableSymbolProject project)
            return null;

        var sourceCode = symbol.PendingSource; // there's intermediate source, so use this
        if (string.IsNullOrWhiteSpace(sourceCode))
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

    public static bool IsNewSymbolNameValid(SymbolPackage symbolPackage, string newSymbolName)
    {
        return IsIdentifierValid(newSymbolName)
        		&& !ReservedWords.Contains(newSymbolName)
               	&& !symbolPackage.ContainsSymbolName(newSymbolName);
    }

    public static bool IsNewSymbolNameValid(IEnumerable<SymbolPackage> symbolPackages, string newSymbolName)
    {
        return IsIdentifierValid(newSymbolName)
               && !symbolPackages.Any(p => p.ContainsSymbolName(newSymbolName));
    }

    public static bool IsIdentifierValid(string className) => !string.IsNullOrWhiteSpace(className)
                                                              && IdentifierValidator.Value.IsValidIdentifier(className);
    public static bool IsValidProjectName(string userName) => IdentifierValidator.Value.IsValidIdentifier(userName);

    public static bool IsNamespaceValid(string namespaceName) => ValidTypeNameSpacePattern.IsMatch(namespaceName);

    private static readonly Regex ValidTypeNameSpacePattern = new(@"^([A-Za-z][A-Za-z\d]*)(\.([A-Za-z_][A-Za-z_\d]*))*$");
    private static readonly string[] ReservedWords = new string[] { "value", "Var", "instance", "item", "Input", "slot" };
    private static readonly Lazy<CodeDomProvider> IdentifierValidator = new(() => CodeDomProvider.CreateProvider("C#"));
}