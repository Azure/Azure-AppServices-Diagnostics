using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace Diagnostics.Scripts.CompilationService.Utilities
{
    /// <summary>
    /// Parser for detector script.
    /// </summary>
    public static class DetectorParser
    {
        /// <summary>
        /// Gets the names of gists referenced in detector code.
        /// </summary>
        /// <param name="detectorCode">Detector script.</param>
        /// <returns>List of gist referenced.</returns>
        public static List<string> GetLoadDirectiveNames(string detectorCode)
        {
            var result = new List<string>();
            SyntaxTree tree = CSharpSyntaxTree.ParseText(detectorCode);
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

            if (root == null) return result;

            foreach(var loadRef in root.GetLoadDirectives())
            {
                result.Add(loadRef.File.ValueText);
            }

            return result;
        }
    
    }
}
