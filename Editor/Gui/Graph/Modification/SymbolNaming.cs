using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using T3.Core.Logging;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Core.Resource;
using T3.Editor.Compilation;
using T3.Editor.Gui.Graph.Helpers;

namespace T3.Editor.Gui.Graph.Modification;

internal static class SymbolNaming
{
    private class ConstructorRewriter : CSharpSyntaxRewriter
    {
        private readonly string _newSymbolName;

        public ConstructorRewriter(string newSymbolName)
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
    }

    public static void RenameSymbol(Symbol symbol, string newName)
    {
        var syntaxTree = GraphUtils.GetSyntaxTree(symbol);
        if (syntaxTree == null)
        {
            Log.Error($"Error getting syntax tree from symbol '{symbol.Name}' source.");
            return;
        }

        // Create new source on basis of original type
        var root = syntaxTree.GetRoot();
        var classRenamer = new ClassRenameRewriter(newName);
        root = classRenamer.Visit(root);

        var memberRewriter = new SymbolNaming.ConstructorRewriter(newName);
        root = memberRewriter.Visit(root);

        var newSource = root.GetText().ToString();

        var newAssembly = OperatorUpdating.CompileSymbolFromSource(newSource, newName);
        if (newAssembly != null)
        {
            var originalSourcePath = SymbolData.BuildFilepathForSymbol(symbol, SymbolData.SourceExtension);
            var operatorResource = ResourceManager.Instance().GetOperatorFileResource(originalSourcePath);
            if (operatorResource != null)
            {
                operatorResource.OperatorAssembly = newAssembly;
                operatorResource.Updated = true;
                symbol.PendingSource = newSource;
                symbol.DeprecatedSourcePath = originalSourcePath;

                GraphOperations.UpdateChangedOperators();
                T3Ui.SaveAll();
                return;
            }
        }

        Log.Error($"Could not update symbol '{symbol.Name}' because its file resource couldn't be found.");
    }
}

internal class ClassRenameRewriter : CSharpSyntaxRewriter
{
    private readonly string _newSymbolName;

    public ClassRenameRewriter(string newSymbolName)
    {
        _newSymbolName = newSymbolName;
    }

    public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        var identifier = SyntaxFactory.ParseToken(_newSymbolName).WithTrailingTrivia(SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, " "));
        var classDeclaration = SyntaxFactory.ClassDeclaration(node.AttributeLists, node.Modifiers, node.Keyword, identifier, node.TypeParameterList,
                                                              null, node.ConstraintClauses, node.OpenBraceToken, node.Members, node.CloseBraceToken,
                                                              node.SemicolonToken);
        try
        {
            var genericName = SyntaxFactory.GenericName(SyntaxFactory.Identifier("Instance"))
                                           .WithTypeArgumentList(SyntaxFactory
                                                                .TypeArgumentList(SyntaxFactory
                                                                                     .SingletonSeparatedList<
                                                                                          TypeSyntax>(SyntaxFactory.IdentifierName(_newSymbolName)))
                                                                .WithGreaterThanToken(SyntaxFactory.Token(SyntaxFactory.TriviaList(),
                                                                                                              SyntaxKind.GreaterThanToken,
                                                                                                              SyntaxFactory
                                                                                                                 .TriviaList(SyntaxFactory.LineFeed))));

            var baseInterfaces = node.BaseList?.Types.RemoveAt(0).Select((e) => e).ToArray();
            var baseList = SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(SyntaxFactory.SimpleBaseType(genericName)));
            baseList = baseList.AddTypes(baseInterfaces);
            baseList = baseList.WithColonToken(SyntaxFactory.Token(SyntaxFactory.TriviaList(), SyntaxKind.ColonToken,
                                                                   SyntaxFactory.TriviaList(SyntaxFactory.Space)));
            classDeclaration = classDeclaration.WithBaseList(baseList);
        }
        catch (System.Exception e)
        {
            Log.Error($"Failed to rename to {_newSymbolName} + {e.Message}");
        }

        return classDeclaration;
    }
}