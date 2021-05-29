using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AspNetPort
{
    public class MakeTryUpdateModelAsync : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (node.DescendantNodes().OfType<IdentifierNameSyntax>().Any(
                id => id.Identifier.Text == "TryUpdateModel"))
            {
                node = AddAsyncKeyword(node);
                node = AddGenericTaskReturnType(node);
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
            //
            // TODO: What if there are mutiple invocations of TryUpdateModel in this method?
            //

            node = AppendAsyncToTryUpdateModel(node);
            node = RemoveArgumentsFromTryUpdateModelAsync(node);
            node = AddAwaitToTryUpdateModelAsync(node);
            
            return node;
        }

        private MethodDeclarationSyntax AppendAsyncToTryUpdateModel(MethodDeclarationSyntax node)
        {
            var invocation = node.DescendantNodes().OfType<IdentifierNameSyntax>().Single(
                id => id.Identifier.Text == "TryUpdateModel");

            var newInvocation = invocation.WithIdentifier(SyntaxFactory.Identifier("TryUpdateModelAsync"));

            return node.ReplaceNode(invocation, newInvocation);
        }

        private MethodDeclarationSyntax RemoveArgumentsFromTryUpdateModelAsync(MethodDeclarationSyntax node) 
        {
            // Remove all but the 1st argument
            var invocation = node.DescendantNodes().OfType<InvocationExpressionSyntax>().Single(
                invocation => invocation.DescendantNodes().OfType<IdentifierNameSyntax>().Any(
                    id => id.Identifier.Text == "TryUpdateModelAsync"));

            var args = invocation.ArgumentList.WithArguments(
                SyntaxFactory.SeparatedList<ArgumentSyntax>(
                    invocation.ArgumentList.Arguments.Take(1))
            );

            return node.ReplaceNode(invocation.ArgumentList, args);
        }

        private MethodDeclarationSyntax AddAwaitToTryUpdateModelAsync(MethodDeclarationSyntax node)
        {
            var invocation = node.DescendantNodes().OfType<InvocationExpressionSyntax>().Single(
                invocation => invocation.DescendantNodes().OfType<IdentifierNameSyntax>().Any(
                    id => id.Identifier.Text == "TryUpdateModelAsync"));            
            
            var awaitInvocation = SyntaxFactory.AwaitExpression(
                    SyntaxFactory.Token(SyntaxKind.AwaitKeyword)
                        .WithTrailingTrivia(SyntaxFactory.Space), 
                        invocation);
            
            return node.ReplaceNode(invocation, awaitInvocation);
        }
    }
}