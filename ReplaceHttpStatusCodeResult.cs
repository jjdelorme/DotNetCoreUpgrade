using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AspNetCoreMvcUpgrade
{
    public class ReplaceHttpStatusCodeResult : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            if (node.DescendantNodes().OfType<IdentifierNameSyntax>().Any(id =>
                id.Identifier.Text == "HttpStatusCodeResult"))
            {
                var statusCodeId = node.DescendantNodes().OfType<IdentifierNameSyntax>().Single(id =>
                    id.Identifier.Text == "HttpStatusCodeResult");               

                // Use ASP.NET Core object equivalent.
                node = node.ReplaceNode(statusCodeId, GetStatusCodeIdentifier(node));

                // Remove arguments.
                node = node.RemoveNodes(node.DescendantNodes().OfType<ArgumentSyntax>(), 
                    SyntaxRemoveOptions.KeepExteriorTrivia);

                return node;
            }
            else
            {
                return base.VisitObjectCreationExpression(node);
            }
        }

        private IdentifierNameSyntax GetStatusCodeIdentifier(SyntaxNode node)
        {
            var args = node.DescendantNodes().OfType<ArgumentSyntax>();

            var statusCode = args.Single().DescendantNodes().OfType<IdentifierNameSyntax>()
                .Last().Identifier.Text;

            string name;

            switch (statusCode)
            {
                case "Conflict":
                    name = "ConflictResult";
                    break;
                case "NoContent":
                    name = "NoContentResult";
                    break;
                case "NotFound":
                    name = "NotFoundResult";
                    break;
                case "OK":
                    name = "OkResult";
                    break;
                case "Unauthorized":
                    name = "UnauthorizedResult";
                    break;
                case "UnprocessableEntity":
                    name = "UnprocessableEntityResult";
                    break;
                case "UnsupportedMediaType":
                    name = "UnsupportedMediaTypeResult";
                    break;
                case "InternalServerError":
                    name = "System.Web.Http.InternalServerErrorResult";
                    break;
                default:
                    name = "BadRequestResult";
                    break;
            }

            return SyntaxFactory.IdentifierName(name);
        }
    }
}