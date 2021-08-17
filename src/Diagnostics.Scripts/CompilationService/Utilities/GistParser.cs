using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Diagnostics.Scripts.CompilationService.Utilities
{
    /// <summary>
    /// Parser for extracting gist class.
    /// </summary>
    public static class GistParser
    {
        /// <summary>
        /// Extracts the gist script from decompiled code.
        /// </summary>
        /// <param name="decompiledCode">decompiledCode from ILSpy</param>
        /// <returns>Returns full string of gist script.</returns>
        public static string GetGistClassAsString(string decompiledCode)
        {
            string gistClassCode = string.Empty;
            var writer = new StringBuilder();
            SyntaxTree tree = CSharpSyntaxTree.ParseText(decompiledCode);
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

            // Print out using statements first.
            writer.AppendLine(root.Usings.ToFullString());

            // Combine the class code.

            var found = false;
            var mainClass = (ClassDeclarationSyntax)root.Members.Where(s => s.Kind() == SyntaxKind.ClassDeclaration).FirstOrDefault();
            foreach (var classMember in mainClass.Members)
            {
                if (classMember.Kind() == SyntaxKind.ClassDeclaration)
                {
                    // Most reliable way of finding the gist class is by checking which class has the definition attribute.
                    
                    var classToParse = (ClassDeclarationSyntax)classMember;
                    foreach (var att in classToParse.AttributeLists)
                    {
                        if (att.ToFullString().Contains("[Definition("))
                        {
                            writer.AppendLine(classToParse.ToFullString());
                            found = true;
                            break;
                        }
                    }
                }
                if (found) break;
            }

            gistClassCode = writer.ToString();

            return gistClassCode;
        }
    }
}
