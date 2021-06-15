using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AspNetCoreMvcUpgrade
{
    public class MakeTryUpdateModelAsync : CSharpSyntaxRewriter
    {
        private bool IsTryUpdateModel(IdentifierNameSyntax id) =>
            id.Identifier.Text == "TryUpdateModel";

        private bool ContainsTryUpdateModel(SyntaxNode node) =>
            node.DescendantNodes().OfType<IdentifierNameSyntax>().Any(IsTryUpdateModel);

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (ContainsTryUpdateModel(node))
            {
                if (!node.Modifiers.Any(m => m.Text == "async"))
                {
                    node = AddAsyncKeyword(node);
                    node = AddGenericTaskReturnType(node);
                }
                node = AwaitTryUpdateModelAsync(node);
                
                return node;                
            }

            return base.VisitMethodDeclaration(node);
        }

        private MethodDeclarationSyntax AddAsyncKeyword(MethodDeclarationSyntax node)
        {
            return node.WithModifiers(node.Modifiers.Add(
                    SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
                        .WithTrailingTrivia(SyntaxFactory.Space)));
        }

        private MethodDeclarationSyntax AddGenericTaskReturnType(MethodDeclarationSyntax node)
        {
            //
            // TODO: what happens if method currently returns void? 
            //

            var trailingTrivia = node.ReturnType.GetTrailingTrivia();

            var asyncReturnType = SyntaxFactory.GenericName(
                    SyntaxFactory.Identifier("Task"))
                .WithTypeArgumentList(
                    SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            node.ReturnType.WithoutTrailingTrivia())))
                .WithTrailingTrivia(trailingTrivia);

            return node.WithReturnType(asyncReturnType);
        }

        private MethodDeclarationSyntax AwaitTryUpdateModelAsync(MethodDeclarationSyntax node)
        {
            var invocations = node.DescendantNodes().OfType<InvocationExpressionSyntax>()
                .Where(ContainsTryUpdateModel);

            foreach (var invocation in invocations)
            {
                node = AppendAsyncToTryUpdateModel(node, invocation);
                node = RemoveArgumentsFromTryUpdateModelAsync(node, invocation);
                // node = AddAwaitToTryUpdateModelAsync(node, invocation);
            }
            
            return node;
        }

        private MethodDeclarationSyntax AppendAsyncToTryUpdateModel(MethodDeclarationSyntax node, 
            InvocationExpressionSyntax invocation)
        {
            var id = invocation.DescendantNodes().OfType<IdentifierNameSyntax>()
                .Single(IsTryUpdateModel);
            var newId = id.WithIdentifier(SyntaxFactory.Identifier("TryUpdateModelAsync"));

            return node.ReplaceNode(id, newId.WithTriviaFrom(id)); 
        }

        private MethodDeclarationSyntax RemoveArgumentsFromTryUpdateModelAsync(MethodDeclarationSyntax node, 
            InvocationExpressionSyntax invocation)
        {
            var args = invocation.ArgumentList.WithArguments(
                SyntaxFactory.SeparatedList<ArgumentSyntax>(
                    invocation.ArgumentList.Arguments.Take(1))
            );

            return node.ReplaceNode(invocation.ArgumentList, args);
        }

        private MethodDeclarationSyntax AddAwaitToTryUpdateModelAsync(MethodDeclarationSyntax node, 
            InvocationExpressionSyntax invocation)
        {           
            // Return if already part of an await expression.
            if (invocation.Parent is AwaitExpressionSyntax)
                return node;
            
            var awaitInvocation = SyntaxFactory.AwaitExpression(
                    SyntaxFactory.Token(SyntaxKind.AwaitKeyword)
                        .WithTrailingTrivia(SyntaxFactory.Space), 
                        invocation);
            
            return node.ReplaceNode(invocation, 
                awaitInvocation.WithTriviaFrom(invocation));
        }
    }
}