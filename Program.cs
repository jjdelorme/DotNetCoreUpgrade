using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Build.Locator;
using System.Threading.Tasks;

namespace AspNetCoreMvcUpgrade
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string path = args[0];

            foreach (var file in await GetFilesAsync(path))
            {
                var root = GetRoot(file);

                if (HasBaseTypeOfController(root))
                {
                    root = UpdateController(root);
                    WriteFile(file, root);
                }
            }
            
            // string programText = System.IO.File.ReadAllText(path);
            // SyntaxTree tree = CSharpSyntaxTree.ParseText(programText);
            // string newPath = path.Replace("/old/", "/new/");
            // File.WriteAllText(newPath, root.ToString());            
        }

        private static void WriteFile(string file, SyntaxNode root)
        {
            string newPath = file.Replace("/old/", "/new/");

            if (!Directory.Exists(Path.GetDirectoryName(newPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(newPath));
            }

            File.WriteAllText(newPath, root.ToString());
        }

        private static SyntaxNode GetRoot(string file)
        {
            string programText = System.IO.File.ReadAllText(file);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(programText);      
            return tree.GetRoot();
        }

        private static bool HasBaseTypeOfController(SyntaxNode root) =>
            root.DescendantNodes().OfType<BaseTypeSyntax>()
                    .Any(baseType => baseType.DescendantNodes()
                        .OfType<IdentifierNameSyntax>()
                            .Any(id => id.Identifier.Text == "Controller"));          

        private static async Task<IEnumerable<string>> GetFilesAsync(string path)
        {
            MSBuildLocator.RegisterDefaults();
            var workspace = MSBuildWorkspace.Create();
            var solution = await workspace.OpenSolutionAsync(path);
            
            var adHocWorkspace = new AdhocWorkspace();
            var project = adHocWorkspace.AddProject(
                solution.Projects.First().AssemblyName,
                solution.Projects.First().Language);
            var relPath = Path.GetDirectoryName(solution.Projects.First().FilePath);
            var files = Directory.GetFiles(relPath, "*.cs", SearchOption.AllDirectories);

            return files;    
        }

        private static SyntaxNode UpdateController(SyntaxNode root)
        {
            root = new UpdateUsings().Update(root);
            root = new ReplaceHttpStatusCodeResult().Visit(root);
            root = new ReplaceHttpNotFoundMethod().Visit(root);
            root = new RemoveBindIncludeParameter().Visit(root);
            root = new MakeTryUpdateModelAsync().Visit(root);

            return root;
        }
    }
}
