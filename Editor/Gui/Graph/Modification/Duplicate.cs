using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using T3.Core.Operator;
using T3.Editor.Compilation;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Commands.Graph;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Graph.Modification;

internal static class Duplicate
{
    public static Symbol DuplicateAsNewType(SymbolUi compositionUi,EditableSymbolProject project, Guid symbolId, string newTypeName, string nameSpace,
                                            string description, Vector2 posOnCanvas)
    {
        var sourceSymbol = EditorSymbolPackage.AllSymbols.FirstOrDefault(x => x.Id == symbolId);
        if (sourceSymbol == null)
        {
            Log.Warning("Can't find symbol to duplicate");
            return null;
        }
        
        //var sourceSymbol = symbolChildToDuplicate.Symbol;

        var syntaxTree = GraphUtils.GetSyntaxTree(sourceSymbol);
        if (syntaxTree == null)
        {
            Log.Error($"Error getting syntax tree from symbol '{sourceSymbol.Name}' source.");
            return null;
        }

        // create new source on basis of original type
        var root = syntaxTree.GetRoot();
        var classRenamer = new ClassRenameRewriter(newTypeName);
        root = classRenamer.Visit(root);
        
        var memberRewriter = new Duplicate.MemberDuplicateRewriter(newTypeName);
        root = memberRewriter.Visit(root);
        var oldToNewIdMap = memberRewriter.OldToNewGuidDict;
        var newSource = root.GetText().ToString();

        var newSymbolId = Guid.NewGuid();
        newSource = newSource.Replace(sourceSymbol.Namespace, nameSpace);
        newSource = ReplaceGuidAttributeWith(newSymbolId, newSource);
        Log.Debug(newSource);

        var success = project.TryCompile(newSource, newTypeName, newSymbolId, nameSpace, out var newSymbol, out _);
        if (!success)
        {
            Log.Error($"Could not compile new symbol '{newTypeName}'");
            return null;
        }

        var sourceSymbolUi = sourceSymbol.GetSymbolUi();
        var newSymbolUi = sourceSymbolUi.CloneForNewSymbol(newSymbol, oldToNewIdMap);
        newSymbolUi.Description = description;
        
        project.ReplaceSymbolUi(newSymbolUi);

        // Apply content to new symbol
        var cmd = new CopySymbolChildrenCommand(sourceSymbolUi,
                                                null,
                                                new List<Annotation>(),
                                                newSymbolUi,
                                                Vector2.One);
        cmd.Do();
        cmd.OldToNewIdDict.ToList().ForEach(x => oldToNewIdMap.Add(x.Key, x.Value));

        // Now copy connection from/to inputs/outputs that are not copied with the command 
        // todo: same code as in Symbol.SetInstanceType, factor out common code
        var connectionsToCopy = sourceSymbol.Connections.FindAll(c => c.IsConnectedToSymbolInput || c.IsConnectedToSymbolOutput);
        var connectionEntriesToReplace = new List<ConnectionEntry>(connectionsToCopy.Count);
        foreach (var con in connectionsToCopy)
        {
            var entry = new Duplicate.ConnectionEntry
                            {
                                Connection = con,
                                MultiInputIndex = sourceSymbol.Connections.FindAll(c => c.TargetParentOrChildId == con.TargetParentOrChildId
                                                                                        && c.TargetSlotId == con.TargetSlotId)
                                                               // ReSharper disable once PossibleUnintendedReferenceComparison
                                                              .FindIndex(cc => cc == con)
                            };
            connectionEntriesToReplace.Add(entry);
        }

        foreach (var conEntry in connectionEntriesToReplace)
        {
            var conToCopy = conEntry.Connection;
            var isInputConnection = conToCopy.IsConnectedToSymbolInput;
            var newSourceSlotId = isInputConnection ? oldToNewIdMap[conToCopy.SourceSlotId] : conToCopy.SourceSlotId;
            var newSourceId = isInputConnection ? conToCopy.SourceParentOrChildId : oldToNewIdMap[conToCopy.SourceParentOrChildId];

            var isOutputConnection = conToCopy.IsConnectedToSymbolOutput;
            var newTargetSlotId = isOutputConnection ? oldToNewIdMap[conToCopy.TargetSlotId] : conToCopy.TargetSlotId;
            var newTargetId = isOutputConnection ? conToCopy.TargetParentOrChildId : oldToNewIdMap[conToCopy.TargetParentOrChildId];

            var newConnection = new Symbol.Connection(newSourceId, newSourceSlotId, newTargetId, newTargetSlotId);
            newSymbol.AddConnection(newConnection, conEntry.MultiInputIndex);
        }

        // Copy the values of the input of the duplicated type: default values of symbol and the ones in composition context
        foreach (var sourceInputDef in sourceSymbol.InputDefinitions)
        {
            Guid newInputId = oldToNewIdMap[sourceInputDef.Id];
            var correspondingInputDef = newSymbol.InputDefinitions.Find(newInputDef => newInputDef.Id == newInputId);
            correspondingInputDef.DefaultValue = sourceInputDef.DefaultValue.Clone();
        }

        // Create instance
        var addCommand = new AddSymbolChildCommand(compositionUi.Symbol, newSymbol.Id) { PosOnCanvas = posOnCanvas };
        UndoRedoStack.AddAndExecute(addCommand);

        // Update the positions
        var sourceSelectables = sourceSymbolUi.GetSelectables().ToArray();
        var newSelectables = newSymbolUi.GetSelectables().ToArray();
        Debug.Assert(sourceSelectables.Length == newSelectables.Length);
        for (int i = 0; i < sourceSelectables.Length && i<newSelectables.Length; i++)
        {
            newSelectables[i].PosOnCanvas = sourceSelectables[i].PosOnCanvas; // todo: check if this is enough or if id check needed
        }
        
        Log.Debug($"Created new symbol '{newTypeName}'");
        
        newSymbolUi.FlagAsModified();
        project.SaveModifiedSymbols();

        return newSymbol;
    }

    private static string ReplaceGuidAttributeWith(Guid newSymbolId, string newSource)
    {
        const string guidTagStart = "Guid(\"";
        int start = newSource.IndexOf(guidTagStart, StringComparison.Ordinal);
        if (start < 0)
            return newSource;

        start += guidTagStart.Length;
        int end = newSource.IndexOf("\")", start, StringComparison.Ordinal);
        if (end < 0)
            return newSource;
        
        var oldGuid = newSource[start..end];
        var newGuid = newSymbolId.ToString();
        return newSource.Replace(oldGuid, newGuid);
    }

    private class ConnectionEntry
    {
        public Symbol.Connection Connection { get; set; }
        public int MultiInputIndex { get; set; }
    }

    private class MemberDuplicateRewriter : CSharpSyntaxRewriter
    {
        private readonly string _newSymbolName;

        public MemberDuplicateRewriter(string newSymbolName)
        {
            _newSymbolName = newSymbolName;
        }

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            return SyntaxFactory.ConstructorDeclaration(_newSymbolName)
                                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                                .NormalizeWhitespace()
                                .WithTrailingTrivia(SyntaxFactory.SyntaxTrivia(SyntaxKind.EndOfLineTrivia, "\r\n"))
                                .WithBody(node.Body)
                                .WithLeadingTrivia(node.GetLeadingTrivia())
                                .WithTrailingTrivia(node.GetTrailingTrivia());
        }

        public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            if (node.Declaration.Type is not GenericNameSyntax nameSyntax)
                return node;

            var idValue = nameSyntax.Identifier.ValueText;
            
            // Todo: Would be great to use nameof() here.
            if (idValue is not ("InputSlot" or "MultiInputSlot" or "Slot" or "TimeClipSlot" or "TransformCallbackSlot"))
                return node; // no input / multi-input / slot / timeClip-slot (output)

            var attrList = node.AttributeLists[0];
            var firstAttribute = attrList.Attributes[0];
            
            var match = _guidRegex.Match(firstAttribute.ToString());
            var oldGuiString = match.Value;
            var oldGuid = Guid.Parse(oldGuiString);
            var newGuid = Guid.NewGuid();
            OldToNewGuidDict[oldGuid] = newGuid;
            if (firstAttribute.ArgumentList == null)
            {
                Log.Debug("Skipping input with inconsistent format: " + node);
                return node;
            }
            
            var attributesWithNewGui = firstAttribute.ArgumentList.ToString().Replace(oldGuiString, newGuid.ToString());
            var argList = SyntaxFactory.ParseAttributeArgumentList(attributesWithNewGui);
            if (argList == null)
            {
                Log.Debug("Skipping input with inconsistent format: " + node);
                return node;
            }
            node = node.ReplaceNode(firstAttribute.ArgumentList, argList);

            return node;
        }

        private readonly Regex _guidRegex = new(@"(\{){0,1}[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}(\}){0,1}",
                                                RegexOptions.IgnoreCase);

        public Dictionary<Guid, Guid> OldToNewGuidDict { get; } = new(10);
    }
}