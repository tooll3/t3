using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ImGuiNET;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using T3.Compilation;
using T3.Core;
using T3.Core.Animation;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Gui.Commands;
using T3.Gui.Commands.Annotations;
using t3.Gui.Commands.Graph;
using T3.Gui.Windows;
using UiHelpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Vector2 = System.Numerics.Vector2;
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator

// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator

namespace T3.Gui.Graph.Interaction
{
    internal static class NodeOperations
    {
        /// <summary>
        /// This is slow and should be refactored into something else
        /// </summary>
        public static IEnumerable<ITimeClip> GetAllTimeClips(Instance compositionOp)
        {
            foreach (var child in compositionOp.Children)
            {
                foreach (var clipProvider in child.Outputs.OfType<ITimeClipProvider>())
                {
                    yield return clipProvider.TimeClip;
                }
            }
        }

        public static ITimeClip GetCompositionTimeClip(Instance compositionOp)
        {
            foreach (var clipProvider in compositionOp.Outputs.OfType<ITimeClipProvider>())
            {
                return clipProvider.TimeClip;
            }

            return null;
        }

        public static Instance GetInstanceFromIdPath(IReadOnlyCollection<Guid> childPath)
        {
            if (childPath == null || childPath.Count == 0)
                return null;

            var instance = T3Ui.UiModel.RootInstance;
            foreach (var childId in childPath)
            {
                // Ignore root
                if (childId == T3Ui.UiModel.RootInstance.SymbolChildId)
                    continue;

                instance = instance.Children.SingleOrDefault(child => child.SymbolChildId == childId);
                if (instance == null)
                    return null;
            }

            return instance;
        }

        public static List<Guid> BuildIdPathForInstance(Instance instance)
        {
            var result = new List<Guid>(6);
            do
            {
                result.Insert(0, instance.SymbolChildId);
                instance = instance.Parent;
            }
            while (instance != null);

            return result;
        }

        public static void CombineAsNewType(SymbolUi parentCompositionSymbolUi,
                                            List<SymbolChildUi> selectedChildUis,
                                            List<Annotation> selectedAnnotations,
                                            string newSymbolName,
                                            string nameSpace, string description, bool shouldBeTimeClip)
        {
            var executedCommands = new List<ICommand>();

            Dictionary<Guid, Guid> oldToNewIdMap = new Dictionary<Guid, Guid>();
            Dictionary<Symbol.Connection, Guid> connectionToNewSlotIdMap = new Dictionary<Symbol.Connection, Guid>();

            // get all the connections that go into the selection (selected ops as target)
            var parentCompositionSymbol = parentCompositionSymbolUi.Symbol;
            var potentialTargetIds = from child in selectedChildUis select child.Id;
            var inputConnections = (from con in parentCompositionSymbol.Connections
                                    from id in potentialTargetIds
                                    where con.TargetParentOrChildId == id
                                    where potentialTargetIds.All(potId => potId != con.SourceParentOrChildId)
                                    select con).ToArray();
            var inputsToGenerate = (from con in inputConnections
                                    from child in parentCompositionSymbol.Children
                                    where child.Id == con.TargetParentOrChildId
                                    from input in child.Symbol.InputDefinitions
                                    where input.Id == con.TargetSlotId
                                    select (child, input, con)).ToList().Distinct().ToArray();
            var usingStringBuilder = new StringBuilder();
            var inputStringBuilder = new StringBuilder();
            var outputStringBuilder = new StringBuilder();
            var connectionsFromNewInputs = new List<Symbol.Connection>(inputConnections.Length);
            int inputNameCounter = 2;
            var inputNameHashSet = new HashSet<string>();
            foreach (var (child, input, origConnection) in inputsToGenerate)
            {
                var inputValueType = input.DefaultValue.ValueType;
                if (TypeNameRegistry.Entries.TryGetValue(inputValueType, out var typeName))
                {
                    var @namespace = input.DefaultValue.ValueType.Namespace;
                    usingStringBuilder.AppendLine("using " + @namespace + ";");
                    Guid newInputGuid = Guid.NewGuid();
                    connectionToNewSlotIdMap.Add(origConnection, newInputGuid);
                    var attributeString = "        [Input(Guid = \"" + newInputGuid + "\")]";
                    inputStringBuilder.AppendLine(attributeString);
                    var newInputName = inputNameHashSet.Contains(input.Name) ? (input.Name + inputNameCounter++) : input.Name;
                    inputNameHashSet.Add(newInputName);
                    var slotString = (input.IsMultiInput ? "MultiInputSlot<" : "InputSlot<") + typeName + ">";
                    var inputString = "        public readonly " + slotString + " " + newInputName + " = new " + slotString + "();";
                    inputStringBuilder.AppendLine(inputString);
                    inputStringBuilder.AppendLine("");

                    var newConnection = new Symbol.Connection(Guid.Empty, newInputGuid, child.Id, input.Id);
                    connectionsFromNewInputs.Add(newConnection);
                }
                else
                {
                    Log.Error($"Error, no registered name found for typename: {input.DefaultValue.ValueType.Name}");
                }
            }

            var outputConnections = (from con in parentCompositionSymbol.Connections
                                     from id in potentialTargetIds
                                     where con.SourceParentOrChildId == id
                                     where potentialTargetIds.All(potId => potId != con.TargetParentOrChildId)
                                     select con).ToArray();
            var outputsToGenerate = (from con in outputConnections
                                     from child in parentCompositionSymbol.Children
                                     where child.Id == con.SourceParentOrChildId
                                     from output in child.Symbol.OutputDefinitions
                                     where output.Id == con.SourceSlotId
                                     select (child, output, con)).ToList().Distinct();
            var connectionsToNewOutputs = new List<Symbol.Connection>(outputConnections.Length);
            int outputNameCounter = 2;
            var outputNameHashSet = new HashSet<string>();
            foreach (var (child, output, origConnection) in outputsToGenerate)
            {
                var outputValueType = output.ValueType;
                if (TypeNameRegistry.Entries.TryGetValue(outputValueType, out var typeName))
                {
                    var @namespace = outputValueType.Namespace;
                    usingStringBuilder.AppendLine("using " + @namespace + ";");
                    Guid newOutputGuid = Guid.NewGuid();
                    var attributeString = "        [Output(Guid = \"" + newOutputGuid + "\")]";
                    outputStringBuilder.AppendLine(attributeString);
                    var newOutputName = outputNameHashSet.Contains(output.Name) ? (output.Name + outputNameCounter++) : output.Name;
                    outputNameHashSet.Add(newOutputName);
                    var slotString = "Slot<" + typeName + ">";
                    var outputString = "        public readonly " + slotString + " " + newOutputName + " = new " + slotString + "();";
                    outputStringBuilder.AppendLine(outputString);
                    outputStringBuilder.AppendLine("");

                    var newConnection = new Symbol.Connection(child.Id, output.Id, Guid.Empty, newOutputGuid);
                    connectionsToNewOutputs.Add(newConnection);
                    connectionToNewSlotIdMap.Add(origConnection, newOutputGuid);
                }
                else
                {
                    Log.Error($"Error, no registered name found for typename: {output.ValueType.Name}");
                }
            }

            usingStringBuilder.AppendLine("using T3.Core.Operator;");
            usingStringBuilder.AppendLine("using T3.Core.Operator.Attributes;");
            usingStringBuilder.AppendLine("using T3.Core.Operator.Slots;");

            Guid newSymbolId = Guid.NewGuid();

            var classStringBuilder = new StringBuilder(usingStringBuilder.ToString());
            classStringBuilder.AppendLine("");
            classStringBuilder.AppendLine("namespace T3.Operators.Types.Id_" + newSymbolId.ToString().ToLower().Replace('-', '_'));
            classStringBuilder.AppendLine("{");
            classStringBuilder.AppendFormat("    public class {0} : Instance<{0}>\n", newSymbolName);
            classStringBuilder.AppendLine("    {");
            classStringBuilder.Append(outputStringBuilder);
            classStringBuilder.AppendLine("");
            classStringBuilder.Append(inputStringBuilder);
            classStringBuilder.AppendLine("    }");
            classStringBuilder.AppendLine("}");
            classStringBuilder.AppendLine("");
            var newSource = classStringBuilder.ToString();
            Log.Debug(newSource);

            // compile new instance type
            var newAssembly = OperatorUpdating.CompileSymbolFromSource(newSource, newSymbolName);
            if (newAssembly == null)
            {
                Log.Error("Error compiling combining type, aborting combine.");
                return;
            }

            var type = newAssembly.ExportedTypes.FirstOrDefault();
            if (type == null)
            {
                Log.Error("Error, new symbol has no compiled instance type");
                return;
            }

            // Create new symbol and its UI
            var newSymbol = new Symbol(type, newSymbolId);
            newSymbol.PendingSource = newSource;
            SymbolRegistry.Entries.Add(newSymbol.Id, newSymbol);
            var newSymbolUi = new SymbolUi(newSymbol)
                                  {
                                      Description = description
                                  };
            newSymbolUi.FlagAsModified();

            SymbolUiRegistry.Entries.Add(newSymbol.Id, newSymbolUi);
            newSymbol.Namespace = nameSpace;

            // Apply content to new symbol
            var copyCmd = new CopySymbolChildrenCommand(parentCompositionSymbolUi, selectedChildUis, selectedAnnotations, newSymbolUi, Vector2.Zero);
            copyCmd.Do();
            executedCommands.Add(copyCmd);

            var newChildrenArea = GetAreaFromChildren(newSymbolUi.ChildUis);

            // Initialize output positions
            if (newSymbolUi.OutputUis.Count > 0)
            {
                var firstOutputPosition = new Vector2(newChildrenArea.Max.X + 300, (newChildrenArea.Min.Y + newChildrenArea.Max.Y) / 2);

                foreach (var outputUi in newSymbolUi.OutputUis.Values)
                {
                    outputUi.PosOnCanvas = firstOutputPosition;
                    firstOutputPosition += new Vector2(0, 100);
                }
            }

            copyCmd.OldToNewIdDict.ToList().ForEach(x => oldToNewIdMap.Add(x.Key, x.Value));

            var selectedChildrenIds = (from child in selectedChildUis select child.Id).ToList();
            parentCompositionSymbol.Animator.RemoveAnimationsFromInstances(selectedChildrenIds);

            foreach (var con in connectionsFromNewInputs)
            {
                var sourceId = con.SourceParentOrChildId;
                var sourceSlotId = con.SourceSlotId;
                var targetId = oldToNewIdMap[con.TargetParentOrChildId];
                var targetSlotId = con.TargetSlotId;

                var newConnection = new Symbol.Connection(sourceId, sourceSlotId, targetId, targetSlotId);
                newSymbol.AddConnection(newConnection);
            }

            foreach (var con in connectionsToNewOutputs)
            {
                var sourceId = oldToNewIdMap[con.SourceParentOrChildId];
                var sourceSlotId = con.SourceSlotId;
                var targetId = con.TargetParentOrChildId;
                var targetSlotId = con.TargetSlotId;

                var newConnection = new Symbol.Connection(sourceId, sourceSlotId, targetId, targetSlotId);
                newSymbol.AddConnection(newConnection);
            }

            // Insert instance of new symbol
            var originalChildrenArea = GetAreaFromChildren(selectedChildUis);
            var addCommand = new AddSymbolChildCommand(parentCompositionSymbolUi.Symbol, newSymbol.Id)
                                 { PosOnCanvas = originalChildrenArea.GetCenter() };

            addCommand.Do();
            executedCommands.Add(addCommand);

            var newSymbolChildId = addCommand.AddedChildId;

            foreach (var con in inputConnections.Reverse()) // reverse for multi input order preservation
            {
                var sourceId = con.SourceParentOrChildId;
                var sourceSlotId = con.SourceSlotId;
                var targetId = newSymbolChildId;
                var targetSlotId = connectionToNewSlotIdMap[con];

                var newConnection = new Symbol.Connection(sourceId, sourceSlotId, targetId, targetSlotId);
                parentCompositionSymbol.AddConnection(newConnection);
            }

            foreach (var con in outputConnections.Reverse()) // reverse for multi input order preservation
            {
                var sourceId = newSymbolChildId;
                var sourceSlotId = connectionToNewSlotIdMap[con];
                var targetId = con.TargetParentOrChildId;
                var targetSlotId = con.TargetSlotId;

                var newConnection = new Symbol.Connection(sourceId, sourceSlotId, targetId, targetSlotId);
                parentCompositionSymbol.AddConnection(newConnection);
            }

            var deleteCmd = new DeleteSymbolChildrenCommand(parentCompositionSymbolUi, selectedChildUis);
            deleteCmd.Do();
            executedCommands.Add(deleteCmd);

            // Delete original annotations
            foreach (var annotation in selectedAnnotations)
            {
                var deleteAnnotationCommand = new DeleteAnnotationCommand(parentCompositionSymbolUi, annotation);
                deleteAnnotationCommand.Do();
                executedCommands.Add(deleteAnnotationCommand);
            }

            UndoRedoStack.Add(new MacroCommand("Combine into symbol", executedCommands));

            // Sadly saving in background does not save the source files.
            // This needs to be fixed.
            //T3Ui.SaveInBackground(false);
            T3Ui.SaveAll();
        }

        private static ImRect GetAreaFromChildren(List<SymbolChildUi> childUis)
        {
            if (childUis.Count == 0)
            {
                return new ImRect(new Vector2(-100, -100),
                                  new Vector2(100, 100));
            }

            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

            foreach (var childUi in childUis)
            {
                min = Vector2.Min(min, childUi.PosOnCanvas);
                max = Vector2.Max(max, childUi.PosOnCanvas + childUi.Size);
            }

            return new ImRect(min, max);
        }

        class ClassRenameRewriter : CSharpSyntaxRewriter
        {
            private readonly string _newSymbolName;

            public ClassRenameRewriter(string newSymbolName)
            {
                _newSymbolName = newSymbolName;
            }

            public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                var identifier = ParseToken(_newSymbolName).WithTrailingTrivia(SyntaxTrivia(SyntaxKind.WhitespaceTrivia, " "));
                var classDeclaration = ClassDeclaration(node.AttributeLists, node.Modifiers, node.Keyword, identifier, node.TypeParameterList,
                                                        null, node.ConstraintClauses, node.OpenBraceToken, node.Members, node.CloseBraceToken,
                                                        node.SemicolonToken);
                var genericName = GenericName(Identifier("Instance"))
                   .WithTypeArgumentList(TypeArgumentList(SingletonSeparatedList<TypeSyntax>(IdentifierName(_newSymbolName)))
                                            .WithGreaterThanToken(Token(TriviaList(), SyntaxKind.GreaterThanToken, TriviaList(LineFeed))));

                var baseInterfaces = node.BaseList?.Types.RemoveAt(0).Select((e) => e).ToArray();
                var baseList = BaseList(SingletonSeparatedList<BaseTypeSyntax>(SimpleBaseType(genericName)));
                baseList = baseList.AddTypes(baseInterfaces);
                baseList = baseList.WithColonToken(Token(TriviaList(), SyntaxKind.ColonToken, TriviaList(Space)));
                classDeclaration = classDeclaration.WithBaseList(baseList);
                return classDeclaration;
            }
        }

        class ConstructorRewriter : CSharpSyntaxRewriter
        {
            private readonly string _newSymbolName;

            public ConstructorRewriter(string newSymbolName)
            {
                _newSymbolName = newSymbolName;
            }

            public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
            {
                return ConstructorDeclaration(_newSymbolName)
                      .AddModifiers(Token(SyntaxKind.PublicKeyword))
                      .NormalizeWhitespace()
                      .WithTrailingTrivia(SyntaxTrivia(SyntaxKind.EndOfLineTrivia, "\r\n"))
                      .WithBody(node.Body)
                      .WithLeadingTrivia(node.GetLeadingTrivia())
                      .WithTrailingTrivia(node.GetTrailingTrivia());
            }
        }

        class MemberDuplicateRewriter : CSharpSyntaxRewriter
        {
            private readonly string _newSymbolName;

            public MemberDuplicateRewriter(string newSymbolName)
            {
                _newSymbolName = newSymbolName;
            }

            public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
            {
                return ConstructorDeclaration(_newSymbolName)
                      .AddModifiers(Token(SyntaxKind.PublicKeyword))
                      .NormalizeWhitespace()
                      .WithTrailingTrivia(SyntaxTrivia(SyntaxKind.EndOfLineTrivia, "\r\n"))
                      .WithBody(node.Body)
                      .WithLeadingTrivia(node.GetLeadingTrivia())
                      .WithTrailingTrivia(node.GetTrailingTrivia());
            }

            public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
            {
                if (!(node.Declaration.Type is GenericNameSyntax nameSyntax))
                    return node;

                var idValue = nameSyntax.Identifier.ValueText;
                if (!(idValue == "InputSlot" || idValue == "MultiInputSlot" || idValue == "Slot" || idValue == "TimeClipSlot" ||
                      idValue == "TransformCallbackSlot"))
                    return node; // no input / multi-input / slot / timeClip-slot (output)

                var attrList = node.AttributeLists[0];
                var attribute = attrList.Attributes[0];
                var match = _guidRegex.Match(attribute.ToString());
                var oldGuid = Guid.Parse(match.Value);
                var newGuid = Guid.NewGuid();
                OldToNewGuidDict[oldGuid] = newGuid;
                var attributeArg = "(Guid = \"" + newGuid + "\")";
                var argList = ParseAttributeArgumentList(attributeArg);

                node = node.ReplaceNode(attribute.ArgumentList, argList);

                return node;
            }

            private readonly Regex _guidRegex = new Regex(@"(\{){0,1}[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}(\}){0,1}",
                                                          RegexOptions.IgnoreCase);

            public Dictionary<Guid, Guid> OldToNewGuidDict { get; } = new Dictionary<Guid, Guid>(10);
        }

        private class ConnectionEntry
        {
            public Symbol.Connection Connection { get; set; }
            public int MultiInputIndex { get; set; }
        }

        public static Symbol DuplicateAsNewType(SymbolUi compositionUi, SymbolChild symbolChildToDuplicate, string newTypeName, string nameSpace,
                                                string description)
        {
            var sourceSymbol = symbolChildToDuplicate.Symbol;

            var syntaxTree = GetSyntaxTree(sourceSymbol);
            if (syntaxTree == null)
            {
                Log.Error($"Error getting syntax tree from symbol '{sourceSymbol.Name}' source.");
                return null;
            }

            // create new source on basis of original type
            var root = syntaxTree.GetRoot();
            var classRenamer = new ClassRenameRewriter(newTypeName);
            root = classRenamer.Visit(root);
            var memberRewriter = new MemberDuplicateRewriter(newTypeName);
            root = memberRewriter.Visit(root);
            var oldToNewIdMap = memberRewriter.OldToNewGuidDict;
            var newSource = root.GetText().ToString();

            var newSymbolId = Guid.NewGuid();
            // patch the symbol id in namespace
            var oldSymbolNamespace = sourceSymbol.Id.ToString().ToLower().Replace('-', '_');
            var newSymbolNamespace = newSymbolId.ToString().ToLower().Replace('-', '_');
            newSource = newSource.Replace(oldSymbolNamespace, newSymbolNamespace);
            Log.Debug(newSource);

            var newAssembly = OperatorUpdating.CompileSymbolFromSource(newSource, newTypeName);
            if (newAssembly == null)
            {
                Log.Error("Error compiling duplicated type, aborting duplication.");
                return null;
            }

            var type = newAssembly.ExportedTypes.FirstOrDefault();
            if (type == null)
            {
                Log.Error("Error, new symbol has no compiled instance type");
                return null;
            }

            // Create and register the new symbol
            var newSymbol = new Symbol(type, newSymbolId);
            newSymbol.PendingSource = newSource;
            SymbolRegistry.Entries.Add(newSymbol.Id, newSymbol);
            var sourceSymbolUi = SymbolUiRegistry.Entries[sourceSymbol.Id];
            var newSymbolUi = sourceSymbolUi.CloneForNewSymbol(newSymbol, oldToNewIdMap);
            newSymbolUi.Description = description;

            SymbolUiRegistry.Entries.Add(newSymbol.Id, newSymbolUi);
            newSymbol.Namespace = nameSpace;

            // Apply content to new symbol
            var cmd = new CopySymbolChildrenCommand(sourceSymbolUi,
                                                    null,
                                                    sourceSymbolUi.Annotations.Values.ToList(),
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
                var entry = new ConnectionEntry
                                {
                                    Connection = con,
                                    MultiInputIndex = sourceSymbol.Connections.FindAll(c => c.TargetParentOrChildId == con.TargetParentOrChildId
                                                                                            && c.TargetSlotId == con.TargetSlotId)
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
            var mousePos = GraphCanvas.Current.InverseTransformPositionFloat(ImGui.GetMousePos());
            var addCommand = new AddSymbolChildCommand(compositionUi.Symbol, newSymbol.Id) { PosOnCanvas = mousePos };
            UndoRedoStack.AddAndExecute(addCommand);

            var newSymbolChild = compositionUi.Symbol.Children.Find(child => child.Id == addCommand.AddedChildId);
            var newSymbolInputValues = newSymbolChild.InputValues;

            foreach (var (id, input) in symbolChildToDuplicate.InputValues)
            {
                var newInput = newSymbolInputValues[oldToNewIdMap[id]];
                newInput.Value.Assign(input.Value.Clone());
                newInput.IsDefault = input.IsDefault;
            }

            // Update the positions
            var sourceSelectables = sourceSymbolUi.GetSelectables().ToArray();
            var newSelectables = newSymbolUi.GetSelectables().ToArray();
            Debug.Assert(sourceSelectables.Length == newSelectables.Length);
            for (int i = 0; i < sourceSelectables.Length; i++)
            {
                newSelectables[i].PosOnCanvas = sourceSelectables[i].PosOnCanvas; // todo: check if this is enough or if id check needed
            }

            // Copy names of instances
            for (var index = 0; index < sourceSymbol.Children.Count; index++)
            {
                newSymbol.Children[index].Name = sourceSymbol.Children[index].Name;
            }
            
            //T3Ui.SaveAll();

            return newSymbol;
        }

        public static void RenameSymbol(Symbol symbol, string newName)
        {
            var syntaxTree = GetSyntaxTree(symbol);
            if (syntaxTree == null)
            {
                Log.Error($"Error getting syntax tree from symbol '{symbol.Name}' source.");
                return;
            }

            // Create new source on basis of original type
            var root = syntaxTree.GetRoot();
            var classRenamer = new ClassRenameRewriter(newName);
            root = classRenamer.Visit(root);

            var memberRewriter = new ConstructorRewriter(newName);
            root = memberRewriter.Visit(root);

            var newSource = root.GetText().ToString();
            //Log.Debug(newSource);

            var newAssembly = OperatorUpdating.CompileSymbolFromSource(newSource, newName);
            if (newAssembly != null)
            {
                //string originalPath = @"Operators\Types\" + symbol.Name + ".cs";
                var originalSourcePath = Model.BuildFilepathForSymbol(symbol, Model.SourceExtension);
                var operatorResource = ResourceManager.Instance().GetOperatorFileResource(originalSourcePath);
                if (operatorResource != null)
                {
                    operatorResource.OperatorAssembly = newAssembly;
                    operatorResource.Updated = true;
                    symbol.PendingSource = newSource;
                    symbol.DeprecatedSourcePath = originalSourcePath;
                    
                    UpdateChangedOperators();
                    T3Ui.SaveAll();
                    return;
                }
            }

            Log.Error($"Could not update symbol '{symbol.Name}' because its file resource couldn't be found.");
        }

        public static SymbolChildUi CreateInstance(Symbol symbol, Symbol parent, Vector2 positionOnCanvas)
        {
            var addCommand = new AddSymbolChildCommand(parent, symbol.Id) { PosOnCanvas = positionOnCanvas };
            UndoRedoStack.AddAndExecute(addCommand);
            var newSymbolChild = parent.Children.Single(entry => entry.Id == addCommand.AddedChildId);

            // Select new node
            var symbolUi = SymbolUiRegistry.Entries[parent.Id];
            var childUi = symbolUi.ChildUis.Find(s => s.Id == newSymbolChild.Id);

            return childUi;
        }

        public static bool IsNewSymbolNameValid(string newSymbolName)
        {
            return !string.IsNullOrEmpty(newSymbolName)
                   && _validTypeNamePattern.IsMatch(newSymbolName)
                   && !SymbolRegistry.Entries.Values.Any(value => string.Equals(value.Name, newSymbolName, StringComparison.OrdinalIgnoreCase));
        }

        private static readonly Regex _validTypeNamePattern = new Regex("^[A-Za-z_]+[A-Za-z0-9_]*$");
        
        public static bool IsValidUserName(string userName)
        {
            return _validUserNamePattern.IsMatch(userName);
        }

        private static readonly Regex _validUserNamePattern = new Regex("^[A-Za-z0-9_]+$");
        

        private class NodeByAttributeIdFinder : CSharpSyntaxRewriter
        {
            public NodeByAttributeIdFinder(Guid[] inputIds)
            {
                _ids = inputIds ?? Array.Empty<Guid>();
            }

            public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
            {
                var attrList = node.AttributeLists[0];
                var searchedNodes = (from attribute in attrList.Attributes
                                     from id in _ids
                                     where attribute.ToString().ToLower().Contains(id.ToString().ToLower())
                                     select attribute).ToArray();

                if (searchedNodes.Length > 0)
                {
                    NodesToRemove.Add(node);
                }

                return node;
            }

            private readonly Guid[] _ids;
            public List<SyntaxNode> NodesToRemove { get; } = new List<SyntaxNode>();
        }

        public static void RemoveInputsFromSymbol(Guid[] inputIdsToRemove, Symbol symbol)
        {
            var syntaxTree = GetSyntaxTree(symbol);
            if (syntaxTree == null)
            {
                Log.Error($"Error getting syntax tree from symbol '{symbol.Name}' source.");
                return;
            }

            var newRoot = RemoveNodesByIdFromTree(inputIdsToRemove, syntaxTree.GetRoot());
            var newSource = newRoot.GetText().ToString();
            Log.Debug(newSource);

            bool success = UpdateSymbolWithNewSource(symbol, newSource);
            if (!success)
            {
                Log.Error("Compilation after removing inputs failed, aborting the remove.");
            }

            FlagDependentOpsAsModified(symbol);
        }

        private static SyntaxNode RemoveNodesByIdFromTree(Guid[] inputIdsToRemove, SyntaxNode root)
        {
            var nodeFinder = new NodeByAttributeIdFinder(inputIdsToRemove);
            var newRoot = nodeFinder.Visit(root);

            return newRoot.RemoveNodes(nodeFinder.NodesToRemove, SyntaxRemoveOptions.KeepNoTrivia);
        }

        public static void RemoveOutputsFromSymbol(Guid[] outputIdsToRemove, Symbol symbol)
        {
            var syntaxTree = GetSyntaxTree(symbol);
            if (syntaxTree == null)
            {
                Log.Error($"Error getting syntax tree from symbol '{symbol.Name}' source.");
                return;
            }

            var newRoot = RemoveNodesByIdFromTree(outputIdsToRemove, syntaxTree.GetRoot());
            var newSource = newRoot.GetText().ToString();
            Log.Debug(newSource);

            var successful = UpdateSymbolWithNewSource(symbol, newSource);
            if (!successful)
            {
                Log.Error("Compilation after removing outputs failed, aborting the remove.");
            }
        }

        private static bool UpdateSymbolWithNewSource(Symbol symbol, string newSource)
        {
            var newAssembly = OperatorUpdating.CompileSymbolFromSource(newSource, symbol.Name);
            if (newAssembly == null)
                return false;
            
            //string path = @"Operators\Types\" + symbol.Name + ".cs";
            var sourcePath = Model.BuildFilepathForSymbol(symbol, Model.SourceExtension);

            var operatorResource = ResourceManager.Instance().GetOperatorFileResource(sourcePath);
            if (operatorResource != null)
            {
                operatorResource.OperatorAssembly = newAssembly;
                operatorResource.Updated = true;
                symbol.PendingSource = newSource;
                return true;
            }

            Log.Error($"Could not update symbol '{symbol.Name}' because its file resource couldn't be found.");

            return false;
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

            public List<(string, SyntaxNode)> InputNodesFound { get; } = new List<(string, SyntaxNode)>();
        }

        private class AllInputNodesReplacer : CSharpSyntaxRewriter
        {
            public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
            {
                if (!(node.Declaration.Type is GenericNameSyntax nameSyntax))
                    return node;

                string idValue = nameSyntax.Identifier.ValueText;
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
            var syntaxTree = GetSyntaxTree(symbol);
            if (syntaxTree == null)
            {
                Log.Error($"Error getting syntax tree from symbol '{symbol.Name}' source.");
                return;
            }

            var root = syntaxTree.GetRoot();

            var inputNodeFinder = new InputNodeByTypeFinder();
            root = inputNodeFinder.Visit(root);
            if (inputNodeFinder.LastInputNodeFound == null)
            {
                Log.Error("Could not add an input as no previous one was found, this case is missing and must be added.");
                return;
            }

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
            root = root.InsertNodesAfter(inputNodeFinder.LastInputNodeFound, new[] { inputDeclaration });

            var newSource = root.GetText().ToString();
            Log.Debug(newSource);

            var success = UpdateSymbolWithNewSource(symbol, newSource);
            if (!success)
            {
                Log.Error("Compilation after adding input failed, aborting the add.");
            }
        }

        public static void AddOutputToSymbol(string outputName, bool isTimeClipOutput, Type outputType, Symbol symbol)
        {
            var syntaxTree = GetSyntaxTree(symbol);
            if (syntaxTree == null)
            {
                Log.Error($"Error getting syntax tree from symbol '{symbol.Name}' source.");
                return;
            }

            var root = syntaxTree.GetRoot();

            var outputNodeFinder = new OutputNodeByTypeFinder();
            root = outputNodeFinder.Visit(root);
            var blockFinder = new ClassDeclarationFinder();
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
                root = root.ReplaceNode(blockFinder.ClassDeclarationNode, classDeclaration);
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

            var success = UpdateSymbolWithNewSource(symbol, newSource);
            if (!success)
            {
                Log.Error("Compilation after adding output failed, aborting the add.");
            }
        }

        public static void AdjustInputOrderOfSymbol(Symbol symbol)
        {
            var syntaxTree = GetSyntaxTree(symbol);
            if (syntaxTree == null)
            {
                Log.Error($"Error getting syntax tree from symbol '{symbol.Name}' source.");
                return;
            }

            var root = syntaxTree.GetRoot();

            var inputNodeFinder = new AllInputNodesFinder();
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
                var attributeString = "\n        [Input(Guid = \"" + inputDef.Id + "\")]\n";
                var typeName = TypeNameRegistry.Entries[inputType];
                var slotString = (inputDef.IsMultiInput ? "MultiInputSlot<" : "InputSlot<") + @namespace + typeName + ">";
                var inputString = "        public readonly " + slotString + " " + inputDef.Name + " = new " + slotString + "();\n";

                var inputDeclaration = SyntaxFactory.ParseMemberDeclaration(attributeString + inputString);
                inputDeclarations.Add(inputDeclaration);
            }

            var replacer = new AllInputNodesReplacer
                               {
                                   ReplacementNodes = inputDeclarations.ToArray()
                               };
            root = replacer.Visit(root);

            var newSource = root.GetText().ToString();
            Log.Debug(newSource);

            var success = UpdateSymbolWithNewSource(symbol, newSource);
            if (!success)
            {
                Log.Error("Compilation after reordering inputs failed, aborting the add.");
            }
        }

        private static SyntaxTree GetSyntaxTree(Symbol symbol)
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

        public static IEnumerable<Instance> GetParentInstances(Instance compositionOp, bool includeChildInstance = false)
        {
            var parents = new List<Instance>();
            var op = compositionOp;
            if (includeChildInstance)
                parents.Add(op);

            while (op.Parent != null)
            {
                op = op.Parent;
                parents.Insert(0, op);
            }

            return parents;
        }

        /// <summary>
        /// Updates symbol definition, instances and symbolUi if modification to operator source code
        /// was detected by Resource file hook.
        /// </summary>
        public static void UpdateChangedOperators()
        {
            var modifiedSymbols = OperatorResource.UpdateChangedOperatorTypes();
            foreach (var symbol in modifiedSymbols)
            {
                UiModel.UpdateUiEntriesForSymbol(symbol);
            }
        }

        public static bool TryGetUiAndInstanceInComposition(Guid id, Instance compositionOp, out SymbolChildUi childUi, out Instance instance)
        {
            instance = compositionOp.Children.SingleOrDefault(child => child.SymbolChildId == id);
            if (instance == null)
            {
                Log.Assert($"Can't select child with id {id} in composition {compositionOp}");
                childUi = null;
                return false;
            }

            childUi = SymbolUiRegistry.Entries[compositionOp.Symbol.Id].ChildUis.SingleOrDefault(ui => ui.Id == id);
            if (childUi == null)
            {
                Log.Assert($"Can't select child with id {id} in composition {compositionOp}");
                return false;
            }

            return true;
        }

        public static void RenameNameSpaces(NamespaceTreeNode node, string nameSpace)
        {
            var orgNameSpace = node.GetAsString();
            foreach (var symbol in SymbolRegistry.Entries.Values)
            {
                if (!symbol.Namespace.StartsWith(orgNameSpace))
                    continue;

                //var newNameSpace = parent + "."
                var newNameSpace = Regex.Replace(symbol.Namespace, orgNameSpace, nameSpace);
                Log.Debug($" Changing namespace of {symbol.Name}: {symbol.Namespace} -> {newNameSpace}");
                symbol.Namespace = newNameSpace;
            }
        }

        private static void FlagDependentOpsAsModified(Symbol symbol)
        {
            foreach (var dependent in GetDependingSymbols(symbol))
            {
                var symbolUi = SymbolUiRegistry.Entries[dependent.Id];
                symbolUi.FlagAsModified();
            }
        }

        public static IEnumerable<Symbol> GetDependingSymbols(Symbol symbol)
        {
            foreach (var s in SymbolRegistry.Entries.Values)
            {
                foreach (var ss in s.Children)
                {
                    if (ss.Symbol.Id != symbol.Id)
                        continue;

                    yield return s;
                    break;
                }
            }
        }
        
        public static void CollectDependencies(ISlot slot, HashSet<ISlot> all)
        {
            if (slot == null)
            {
                Log.Warning("skipping null slot");
                return;
            }

            if (all.Contains(slot))
                return;
            
            all.Add(slot);
            
            if (slot is IInputSlot)
            {
                if (!slot.IsConnected)
                    return;
                
                CollectDependencies(slot.GetConnection(0), all);
            }
            else if (slot.IsConnected)
            {
                CollectDependencies(slot.GetConnection(0), all);
            }
            else
            {
                var parentInstance = slot.Parent;
                foreach (var input in parentInstance.Inputs)
                {
                    if (input.IsConnected)
                    {
                        if (input.IsMultiInput)
                        {
                            var multiInput = (IMultiInputSlot)input;
                            
                            foreach (var entry in multiInput.GetCollectedInputs())
                            {
                                CollectDependencies(entry, all);
                            }
                        }
                        else
                        {
                            var target = input.GetConnection(0);
                            CollectDependencies(target, all);
                        }
                    }
                    else if ((input.DirtyFlag.Trigger & DirtyFlagTrigger.Animated) == DirtyFlagTrigger.Animated)
                    {
                        input.DirtyFlag.Invalidate();
                    }
                }
            }
        }
    }
}