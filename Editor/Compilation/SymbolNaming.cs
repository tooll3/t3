using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using T3.Core.Operator;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.UiModel;

namespace T3.Editor.Compilation;

internal static class SymbolNaming
{
    private sealed class ConstructorRewriter : CSharpSyntaxRewriter
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

    public static bool RenameSymbol(Symbol symbol, string newName)
    {
        if (symbol.SymbolPackage.IsReadOnly)
            throw new ArgumentException("Symbol is read-only and cannot be renamed");
        
        
        var syntaxTree = GraphUtils.GetSyntaxTree(symbol);
        if (syntaxTree == null)
        {
            Log.Error($"Error getting syntax tree from symbol '{symbol.Name}' source.");
            return false;
        }

        // Create new source on basis of original type
        var root = syntaxTree.GetRoot();
        var classRenamer = new ClassRenameRewriter(newName);
        root = classRenamer.Visit(root);

        var memberRewriter = new ConstructorRewriter(newName);
        root = memberRewriter.Visit(root);

        var newSource = root.GetText().ToString();

        return EditableSymbolProject.RecompileSymbol(symbol, newSource, false, out _);
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
        var genericName = SyntaxFactory.GenericName(SyntaxFactory.Identifier("Instance"))
                                       .WithTypeArgumentList(SyntaxFactory
                                                            .TypeArgumentList(SyntaxFactory
                                                                                 .SingletonSeparatedList<
                                                                                      TypeSyntax>(SyntaxFactory.IdentifierName(_newSymbolName)))
                                                            .WithGreaterThanToken(SyntaxFactory.Token(SyntaxFactory.TriviaList(), SyntaxKind.GreaterThanToken,
                                                                                                      SyntaxFactory.TriviaList(SyntaxFactory.LineFeed))));

        var baseInterfaces = node.BaseList?.Types.RemoveAt(0).Select((e) => e).ToArray();
        var baseList = SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(SyntaxFactory.SimpleBaseType(genericName)));
        baseList = baseList.AddTypes(baseInterfaces);
        baseList =
            baseList.WithColonToken(SyntaxFactory.Token(SyntaxFactory.TriviaList(), SyntaxKind.ColonToken, SyntaxFactory.TriviaList(SyntaxFactory.Space)));
        classDeclaration = classDeclaration.WithBaseList(baseList);
        return classDeclaration;
    }
}