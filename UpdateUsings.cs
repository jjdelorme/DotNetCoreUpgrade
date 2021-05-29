using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AspNetCoreMvcUpgrade
{
    class UpdateUsings
    {
        public SyntaxNode Update(SyntaxNode root)
        {
            root = ReplacePagedListUsing(root);
            root = AddMvcCoreUsings(root);
            root = AddTasksUsing(root);     
            root = AddMvcRenderingUsing(root);

            return root;
        }

        public SyntaxNode ReplacePagedListUsing(SyntaxNode root)
        {
            var pagedUsings = root.DescendantNodes()
                .OfType<UsingDirectiveSyntax>().Where(syntax => 
                    syntax.Name.ToString() == "PagedList");

            if (pagedUsings.Count() > 0)
            {
                return root.ReplaceNode(pagedUsings.Single().Name, 
                    SyntaxFactory.IdentifierName("PagedList.Core"));
            }

            return root;
        }

        public SyntaxNode AddMvcCoreUsings(SyntaxNode root)
        {
            List<UsingDirectiveSyntax> coreUsings = new List<UsingDirectiveSyntax>();
            coreUsings.Add(SyntaxFactory.UsingDirective(
                SyntaxFactory.IdentifierName("System.Web")));
            coreUsings.Add(SyntaxFactory.UsingDirective(
                SyntaxFactory.IdentifierName("System.Web.Mvc")));                
            coreUsings.Add(SyntaxFactory.UsingDirective(
                SyntaxFactory.IdentifierName("Microsoft.AspNetCore.Mvc")));

            var usings = root.DescendantNodes().OfType<UsingDirectiveSyntax>();
            
            return root.InsertNodesAfter(usings.Last(), coreUsings).NormalizeWhitespace();
        }        

        public SyntaxNode AddTasksUsing(SyntaxNode root)
        {
            return AddUsing(root, "System.Threading.Tasks");
        }        

        public SyntaxNode AddMvcRenderingUsing(SyntaxNode root)
        {
            if (root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Any(
                    creation => creation.DescendantNodes().OfType<IdentifierNameSyntax>()
                        .Any(id => id.Identifier.Text == "SelectList")
                ))
            {
                return AddUsing(root, "Microsoft.AspNetCore.Mvc.Rendering");
            }

            return root;
        }

        private SyntaxNode AddUsing(SyntaxNode root, string usingText)
        {
            List<UsingDirectiveSyntax> newUsings = new List<UsingDirectiveSyntax>();
            newUsings.Add(SyntaxFactory.UsingDirective(
                SyntaxFactory.IdentifierName(usingText)));

            var usings = root.DescendantNodes().OfType<UsingDirectiveSyntax>();
            
            return root.InsertNodesAfter(usings.Last(), newUsings).NormalizeWhitespace();            
        }
    }
}