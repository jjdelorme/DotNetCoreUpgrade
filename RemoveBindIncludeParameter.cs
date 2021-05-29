using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AspNetCoreMvcUpgrade
{
    public class RemoveBindIncludeParameter : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitAttributeList(AttributeListSyntax node)
        {
            if (node.DescendantNodes().OfType<IdentifierNameSyntax>().Any(
                    name => name.Identifier.Text == "Bind") && 
                node.DescendantNodes().OfType<IdentifierNameSyntax>().Any(
                    name => name.Identifier.Text == "Include"))
            {
                var id = node.DescendantNodes().OfType<IdentifierNameSyntax>().Single(
                    name => name.Identifier.Text == "Include");
                
                return node.RemoveNode(id.Parent, SyntaxRemoveOptions.KeepNoTrivia);
            }            

            return base.VisitAttributeList(node);
        }
    }
}