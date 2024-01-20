using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using T3.Core.Logging;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Editor.Compilation;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Graph.Modification;

internal static class InputsAndOutputs
{
    public static void RemoveInputsFromSymbol(Guid[] inputIdsToRemove, Symbol symbol)
    {
        var syntaxTree = GraphUtils.GetSyntaxTree(symbol);
        if (syntaxTree == null)
        {
            Log.Error($"Error getting syntax tree from symbol '{symbol.Name}' source.");
            return;
        }

        var newRoot = RemoveNodesByIdFromTree(inputIdsToRemove, syntaxTree.GetRoot());
        var newSource = newRoot.GetText().ToString();
        Log.Debug(newSource);

        bool success = GraphOperations.UpdateSymbolWithNewSource(symbol, newSource);
        if (!success)
        {
            Log.Error("Compilation after removing inputs failed, aborting the remove.");
        }

        FlagDependentOpsAsModified(symbol);
    }

    private static SyntaxNode RemoveNodesByIdFromTree(Guid[] inputIdsToRemove, SyntaxNode root)
    {
        var nodeFinder = new InputsAndOutputs.NodeByAttributeIdFinder(inputIdsToRemove);
        var newRoot = nodeFinder.Visit(root);

        return SyntaxNodeExtensions.RemoveNodes<SyntaxNode>(newRoot, nodeFinder.NodesToRemove, SyntaxRemoveOptions.KeepNoTrivia);
    }

    public static void RemoveOutputsFromSymbol(Guid[] outputIdsToRemove, Symbol symbol)
    {
        var syntaxTree = GraphUtils.GetSyntaxTree(symbol);
        if (syntaxTree == null)
        {
            Log.Error($"Error getting syntax tree from symbol '{symbol.Name}' source.");
            return;
        }

        var newRoot = RemoveNodesByIdFromTree(outputIdsToRemove, syntaxTree.GetRoot());
        var newSource = newRoot.GetText().ToString();
        Log.Debug(newSource);

        var successful = GraphOperations.UpdateSymbolWithNewSource(symbol, newSource);
        if (!successful)
        {
            Log.Error("Compilation after removing outputs failed, aborting the remove.");
        }
    }

    private static void FlagDependentOpsAsModified(Symbol symbol)
    {
        foreach (var dependent in Structure.CollectDependingSymbols(symbol))
        {
            var symbolUi = SymbolUiRegistry.Entries[dependent.Id];
            symbolUi.FlagAsModified();
        }
    }

    internal class NodeByAttributeIdFinder : CSharpSyntaxRewriter
    {
        public NodeByAttributeIdFinder(Guid[] inputIds)
        {
            _ids = inputIds ?? Array.Empty<Guid>();
        }

        public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            if (node.AttributeLists.Count > 0) {
                var attrList = node.AttributeLists[0];
                var searchedNodes = (from attribute in attrList.Attributes
                                    from id in _ids
                                    where attribute.ToString().ToLower().Contains(id.ToString().ToLower())
                                    select attribute).ToArray();

                if (searchedNodes.Length > 0)
                {
                    NodesToRemove.Add(node);
                }
            }
            return node;
        }

        private readonly Guid[] _ids;
        public List<SyntaxNode> NodesToRemove { get; } = new();
    }

    private class InputNodeByTypeFinder : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            if (!(node.Declaration.Type is GenericNameSyntax nameSyntax))
                return node;

            var idValue = nameSyntax.Identifier.ValueText;
            if (idValue == "InputSlot" || idValue == "MultiInputSlot")
                LastInputNodeFound = node;
            else if (LastInputNodeFound == null && idValue == "Slot")
                LastInputNodeFound = node;

            return node;
        }

        public SyntaxNode LastInputNodeFound { get; private set; }
    }

    private class OutputNodeByTypeFinder : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            if (!(node.Declaration.Type is GenericNameSyntax nameSyntax))
                return node;

            string idValue = nameSyntax.Identifier.ValueText;
            if (idValue == "Slot" || idValue == "TimeClipSlot") // find general way to support all output types here
                LastOutputNodeFound = node;

            return node;
        }

        public SyntaxNode LastOutputNodeFound { get; private set; }
    }

    private class ClassDeclarationFinder : CSharpSyntaxWalker
    {
        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            ClassDeclarationNode = node;
        }

        public ClassDeclarationSyntax ClassDeclarationNode;
    }

    private class AllInputNodesFinder : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            if (!(node.Declaration.Type is GenericNameSyntax nameSyntax))
                return node;

            string idValue = nameSyntax.Identifier.ValueText;
            if (idValue == "InputSlot" || idValue == "MultiInputSlot")
            {
                var first = node.Declaration.Variables[0];
                var id = first.Identifier.ValueText;
                InputNodesFound.Add((id, node));
            }

            return node;
        }

        public List<(string, SyntaxNode)> InputNodesFound { get; } = new();
    }

    private class AllInputNodesReplacer : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            if (node.Declaration.Type is not GenericNameSyntax nameSyntax)
                return node;

            var idValue = nameSyntax.Identifier.ValueText;
            if (idValue == "InputSlot" || idValue == "MultiInputSlot")
            {
                return ReplacementNodes[_index++];
            }

            return node;
        }

        private int _index;
        public SyntaxNode[] ReplacementNodes;
    }

    public static void AddInputToSymbol(string inputName, bool multiInput, Type inputType, Symbol symbol)
    {
        var syntaxTree = GraphUtils.GetSyntaxTree(symbol);
        if (syntaxTree == null)
        {
            Log.Error($"Error getting syntax tree from symbol '{symbol.Name}' source.");
            return;
        }

        var root = syntaxTree.GetRoot();

        var inputNodeFinder = new InputsAndOutputs.InputNodeByTypeFinder();
        var blockFinder = new InputsAndOutputs.ClassDeclarationFinder();
        
        if (inputNodeFinder.LastInputNodeFound == null)
        {
            blockFinder.Visit(root);
            if (blockFinder.ClassDeclarationNode == null)
            {
                Log.Error("Can't find class declaration.");
                return;
            }
        }
        
        root = inputNodeFinder.Visit(root);
        
        var @namespace = inputType.Namespace;
        if (@namespace == "System")
            @namespace = String.Empty;
        else
            @namespace += ".";
        var attributeString = "\n        [Input(Guid = \"" + Guid.NewGuid() + "\")]\n";
        var typeName = TypeNameRegistry.Entries[inputType];
        var slotString = (multiInput ? "MultiInputSlot<" : "InputSlot<") + @namespace + typeName + ">";
        var inputString = "        public readonly " + slotString + " " + inputName + " = new " + slotString + "();\n";

        var inputDeclaration = SyntaxFactory.ParseMemberDeclaration(attributeString + inputString);
        if (inputNodeFinder.LastInputNodeFound != null)
        {
            root = root.InsertNodesAfter(inputNodeFinder.LastInputNodeFound, new[] { inputDeclaration });
        }
        else if (blockFinder.ClassDeclarationNode != null)
        {
            var node = blockFinder.ClassDeclarationNode;
            var classDeclaration = node.AddMembers(inputDeclaration);
            root = SyntaxNodeExtensions.ReplaceNode(root, (SyntaxNode)blockFinder.ClassDeclarationNode, (SyntaxNode)classDeclaration);
        }
        else
        {
            var theClass = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First();
            var classDeclarationSyntax = theClass.AddMembers(inputDeclaration);
            Log.Info($"{classDeclarationSyntax}");
            return;
        }

        var newSource = root.GetText().ToString();
        Log.Debug(newSource);

        var success = GraphOperations.UpdateSymbolWithNewSource(symbol, newSource);
        if (!success)
        {
            Log.Error("Compilation after adding input failed, aborting the add.");
        }
    }

    public static void AddOutputToSymbol(string outputName, bool isTimeClipOutput, Type outputType, Symbol symbol)
    {
        var syntaxTree = GraphUtils.GetSyntaxTree(symbol);
        if (syntaxTree == null)
        {
            Log.Error($"Error getting syntax tree from symbol '{symbol.Name}' source.");
            return;
        }

        var root = syntaxTree.GetRoot();

        var outputNodeFinder = new InputsAndOutputs.OutputNodeByTypeFinder();
        root = outputNodeFinder.Visit(root);
        
        var blockFinder = new InputsAndOutputs.ClassDeclarationFinder();
        if (outputNodeFinder.LastOutputNodeFound == null)
        {
            blockFinder.Visit(root);
            if (blockFinder.ClassDeclarationNode == null)
            {
                Log.Error("Could not add an output as no previous one was found, this case is missing and must be added.");
                return;
            }
        }

        var @namespace = outputType.Namespace;
        if (@namespace == "System")
            @namespace = String.Empty;
        else
            @namespace += ".";
        var attributeString = "\n        [Output(Guid = \"" + Guid.NewGuid() + "\")]\n";
        var typeName = TypeNameRegistry.Entries[outputType];
        var slotString = (isTimeClipOutput ? "TimeClipSlot<" : "Slot<") + @namespace + typeName + ">";
        var inputString = $"        public readonly {slotString} {outputName} = new {slotString}();\n";

        var outputDeclaration = SyntaxFactory.ParseMemberDeclaration(attributeString + inputString);
        if (outputNodeFinder.LastOutputNodeFound != null)
        {
            root = root.InsertNodesAfter(outputNodeFinder.LastOutputNodeFound, new[] { outputDeclaration });
        }
        else if (blockFinder.ClassDeclarationNode != null)
        {
            var node = blockFinder.ClassDeclarationNode;
            var classDeclaration = node.AddMembers(outputDeclaration);
            root = SyntaxNodeExtensions.ReplaceNode(root, (SyntaxNode)blockFinder.ClassDeclarationNode, (SyntaxNode)classDeclaration);
        }
        else
        {
            var theClass = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First();
            var classDeclarationSyntax = theClass.AddMembers(outputDeclaration);
            Log.Info($"{classDeclarationSyntax}");
            return;
        }

        var newSource = root.GetText().ToString();
        Log.Debug(newSource);

        var success = GraphOperations.UpdateSymbolWithNewSource(symbol, newSource);
        if (!success)
        {
            Log.Error("Compilation after adding output failed, aborting the add.");
        }
    }

    public static void AdjustInputOrderOfSymbol(Symbol symbol)
    {
        var syntaxTree = GraphUtils.GetSyntaxTree(symbol);
        if (syntaxTree == null)
        {
            Log.Error($"Error getting syntax tree from symbol '{symbol.Name}' source.");
            return;
        }

        var root = syntaxTree.GetRoot();

        var inputNodeFinder = new InputsAndOutputs.AllInputNodesFinder();
        root = inputNodeFinder.Visit(root);

        // Check if the order in code is the same as in symbol
        Debug.Assert(inputNodeFinder.InputNodesFound.Count == symbol.InputDefinitions.Count);
        var orderIsOk = true;
        for (int i = 0; i < inputNodeFinder.InputNodesFound.Count; i++)
        {
            if (inputNodeFinder.InputNodesFound[i].Item1 != symbol.InputDefinitions[i].Name)
            {
                orderIsOk = false;
                break;
            }
        }

        if (orderIsOk)
            return; // nothing to do

        var inputDeclarations = new List<SyntaxNode>(symbol.InputDefinitions.Count);
        foreach (var inputDef in symbol.InputDefinitions)
        {
            var inputType = inputDef.DefaultValue.ValueType;
            var @namespace = inputType.Namespace;
            if (@namespace == "System")
                @namespace = String.Empty;
            else
                @namespace += ".";

            string foundAttributeString = null;
            foreach (var (nodeName, syntaxNode) in inputNodeFinder.InputNodesFound)
            {
                if (nodeName != inputDef.Name)
                    continue;

                foundAttributeString = Enumerable.First<string>(syntaxNode.ToString().Split("\n"));
                break;
            }

            var fallbackAttributeString = "\n        [Input(Guid = \"" + inputDef.Id + "\")]\n";
            var attributeString = foundAttributeString == null
                                      ? fallbackAttributeString
                                      : $"\n        {foundAttributeString}\n";

            var typeName = TypeNameRegistry.Entries[inputType];
            var slotString = (inputDef.IsMultiInput ? "MultiInputSlot<" : "InputSlot<") + @namespace + typeName + ">";
            var inputString = "        public readonly " + slotString + " " + inputDef.Name + " = new " + slotString + "();\n";

            var inputDeclaration = SyntaxFactory.ParseMemberDeclaration(attributeString + inputString);
            inputDeclarations.Add(inputDeclaration);
        }

        var replacer = new InputsAndOutputs.AllInputNodesReplacer
                           {
                               ReplacementNodes = inputDeclarations.ToArray()
                           };
        root = replacer.Visit(root);

        var newSource = root.GetText().ToString();
        Log.Debug(newSource);

        var success = GraphOperations.UpdateSymbolWithNewSource(symbol, newSource);
        if (!success)
        {
            Log.Error("Compilation after reordering inputs failed, aborting the add.");
        }
    }
    
    
    public static bool RenameInput(Symbol symbol, Guid inputId, string newName, bool dryRun, out string warning)
    {
        warning = null;
        var syntaxTree = GraphUtils.GetSyntaxTree(symbol);
        if (syntaxTree == null)
        {
            warning= $"Error getting syntax tree from symbol '{symbol.Name}' source.";
            return false;
        }

        var root = syntaxTree.GetRoot();

        
        var isValid = GraphUtils.IsNewSymbolNameValid(newName);
        if (!isValid)
        {
            warning= $"{newName} is not a valid input name.";
            return false;
        }
            
        var sourceCode = root.GetText().ToString();
        
        var alreadyExistsResult = Regex.Match(sourceCode, $"\b{newName}\b");
        if (alreadyExistsResult.Success)
        {
            warning= $"{newName} is already used within the operators source code.";
            return false;
        }

        var inputDef = symbol.InputDefinitions.FirstOrDefault(i => i.Id == inputId);
        if (inputDef == null)
        {
            warning= $"{inputId} input definition not found.";
            return false;
        }

        var newSource = Regex.Replace(sourceCode, @$"\b{inputDef.Name}\b", newName);

        if (dryRun)
        {
            var newAssembly = OperatorUpdating.CompileSymbolFromSource(newSource, symbol.Name);
            if (newAssembly != null)
                return true;
            
            warning = "Failed to compile operator";
            return false;
        }
        
        var success = GraphOperations.UpdateSymbolWithNewSource(symbol, newSource);
        if (!success)
        {
            warning= "Compilation after reordering inputs failed, aborting the add.";
            return false;
        }
        
        FlagDependentOpsAsModified(symbol);
        return true;
    }
}