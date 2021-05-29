using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AspNetPort
{
    public class ReplaceHttpNotFoundMethod : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (node.DescendantNodes().OfType<IdentifierNameSyntax>().Any(
                id => id.Identifier.Text == "HttpNotFound"))
            {
                var resultType = SyntaxFactory.ObjectCreationExpression(
                    SyntaxFactory.ParseTypeName("NotFoundResult")).NormalizeWhitespace();

                var methodName = node.DescendantNodes().OfType<IdentifierNameSyntax>().Single(
                    id => id.Identifier.Text == "HttpNotFound"); 

                return node.ReplaceNode(methodName, resultType);
            }

            return base.VisitInvocationExpression(node);
        }
    }
}