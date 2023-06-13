using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Resource;

namespace T3.Editor.Gui.Graph.Helpers;

internal static class GraphUtils
{
    public static SyntaxTree GetSyntaxTree(Symbol symbol)
    {
        var pendingSource = symbol.PendingSource; // there's intermediate source, so use this
        if (string.IsNullOrEmpty(pendingSource))
        {
            //var path = @"Operators\Types\" + symbol.Name + ".cs";
            var path = Model.BuildFilepathForSymbol(symbol, Model.SourceExtension);
            try
            {
                pendingSource = File.ReadAllText(path);
            }
            catch (Exception e)
            {
                Log.Error($"Error opening file '{path}");
                Log.Error(e.Message);
                return null;
            }
        }

        if (string.IsNullOrEmpty(pendingSource))
        {
            Log.Info("Source was empty, skip compilation.");
            return null;
        }

        return CSharpSyntaxTree.ParseText(pendingSource);
    }

    public static bool IsNewSymbolNameValid(string newSymbolName)
    {
        return !string.IsNullOrEmpty(newSymbolName)
               && _validTypeNamePattern.IsMatch(newSymbolName)
               && !SymbolRegistry.Entries.Values.Any(value => string.Equals(value.Name, newSymbolName, StringComparison.OrdinalIgnoreCase));
    }

    private static readonly Regex _validTypeNamePattern = new Regex("^[A-Za-z_]+[A-Za-z0-9_]*$");

    public static bool IsNameSpaceValid(string nameSpaceString)
    {
        return !string.IsNullOrEmpty(nameSpaceString)
               && _validTypeNameSpacePattern.IsMatch(nameSpaceString);
    }

    private static readonly Regex _validTypeNameSpacePattern = new Regex(@"^([A-Za-z][A-Za-z\d]*)(\.([A-Za-z_][A-Za-z_\d]*))*$");

    public static bool IsValidUserName(string userName)
    {
        return _validUserNamePattern.IsMatch(userName);
    }

    private static readonly Regex _validUserNamePattern = new Regex("^[A-Za-z0-9_]+$");
}