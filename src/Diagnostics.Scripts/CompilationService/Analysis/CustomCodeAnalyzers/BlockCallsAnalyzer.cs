using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Diagnostics.Scripts.CompilationService.Analysis.CustomCodeAnalyzers
{
    [DiagnosticAnalyzer("C#")]
    public class BlockCallsAnalyzer : DiagnosticAnalyzer
    {
        private DiagnosticDescriptor blockCallsDiagnosticDescriptor = new DiagnosticDescriptor(id: "ApBlkP001", title: "Forbidden call detected.", 
            messageFormat:"Reference to {0} is forbidden.", category: "ApBlk001:AppLensBlockedAccess",defaultSeverity:DiagnosticSeverity.Error, isEnabledByDefault:true);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create<DiagnosticDescriptor>(blockCallsDiagnosticDescriptor); // throw new NotImplementedException();

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSemanticModelAction(InspectForCallsToBlock);
            //throw new NotImplementedException();
        }

        private void InspectForCallsToBlock(SemanticModelAnalysisContext semanticContext)
        {
            var semanticModel = semanticContext.SemanticModel;
            if (IsForbiddenMethodOrPropertyUsed(semanticModel, TargetSymbolTypes.Property | TargetSymbolTypes.Member, "Environment.NewLine", out Diagnostic diagnosticResult))
            {
                semanticContext.ReportDiagnostic(diagnosticResult);
            }
        }

        private bool IsForbiddenMethodOrPropertyUsed(SemanticModel semanticModel, TargetSymbolTypes targetSymbolType, string nameToLookup, out Diagnostic diagnosticResult)
        {
            List<ISymbol> symbolsToReturn = new List<ISymbol>();
            if ((targetSymbolType & TargetSymbolTypes.Property) > 0 )
            {
                var propertiesReferenced = semanticModel.SyntaxTree.GetRoot().DescendantNodes().OfType<IPropertyReferenceOperation>();
                foreach (var propertyReferenced in propertiesReferenced)
                {
                    if (propertyReferenced.Property.Name == "NewLine" && propertyReferenced.Property.ContainingType.Name == "System.Environment" && propertyReferenced.Property.ContainingType.IsStatic)
                    {
                        //diagnosticResult = Diagnostic.Create(id: "ApBlkP001", category: "ReferencedForbiddenProperty", message: "Line : . Use of System.Environment.NewLine is forbidden", severity: DiagnosticSeverity.Error,
                        //    defaultSeverity: DiagnosticSeverity.Error, isEnabledByDefault: true, warningLevel: 0, title: "Property reference not allowed",
                        //    description: "Due to security reasons, we are not allowing the use of System.Environment.NewLine property.", helpLink: null,
                        //    location: propertyReferenced.SemanticModel.SyntaxTree.GetRoot().GetLocation());
                        diagnosticResult = Diagnostic.Create(blockCallsDiagnosticDescriptor, location: propertyReferenced.SemanticModel.SyntaxTree.GetRoot().GetLocation(), new string[] { "System.Environment.NewLine" });
                        return true;
                    }
                }

            }

            if ((targetSymbolType & TargetSymbolTypes.Member) > 0)
            {
                var membersReferenced = semanticModel.SyntaxTree.GetRoot().DescendantNodes().OfType<IMemberReferenceOperation>();
                foreach (var memberReferenced in membersReferenced)
                {
                    if (memberReferenced.Member.Name == "NewLine" && memberReferenced.Member.ContainingType.Name == "System.Environment" && memberReferenced.Member.ContainingType.IsStatic)
                    {
                        //diagnosticResult = Diagnostic.Create(id: "ApBlkdP001", category: "ForbiddenPropertyReferenced", message: "Use of System.Environment.NewLine is forbidden", severity: DiagnosticSeverity.Error,
                        //    defaultSeverity: DiagnosticSeverity.Error, isEnabledByDefault: true, warningLevel: 0, title: "Property reference not allowed",
                        //    description: "Due to security reasons, we are not allowing the use of System.Environment.NewLine property.", helpLink: null,
                        //    location: propertyReferenced.SemanticModel.SyntaxTree.GetRoot().GetLocation());

                        diagnosticResult = Diagnostic.Create(blockCallsDiagnosticDescriptor, location: memberReferenced.SemanticModel.SyntaxTree.GetRoot().GetLocation(), new string[] { "System.Environment.NewLine" });
                        return true;
                    }
                }

                var membersAccessed = semanticModel.SyntaxTree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>();
                foreach(var memberAccessed in membersAccessed)
                {
                    if (memberAccessed.Name.ToString() == "NewLine" && memberAccessed.Expression.ToString() == "Environment")
                    {
                        diagnosticResult = Diagnostic.Create(blockCallsDiagnosticDescriptor, location: memberAccessed.GetLocation(), new string[] { "System.Environment.NewLine" });
                        return true;
                    }
                }

            }

            //var methodDeclarations = semanticModel.SyntaxTree.GetRoot().DescendantNodes().OfType<IPropertyReferenceOperation>();
            //var symbolList = new List<ISymbol>();

            //foreach (var declaration in methodDeclarations)
            //{
            //    foreach (var attributeList in declaration.AttributeLists)
            //    {
            //        if (attributeList.Attributes.Any(a => (a.Name as IdentifierNameSyntax)?.Identifier.Text == attributeName))
            //        {
            //            symbolList.Add(semanticModel.GetDeclaredSymbol(declaration));
            //            break;
            //        }
            //    }
            //}
            diagnosticResult = Diagnostic.Create(blockCallsDiagnosticDescriptor, location: null, new string[] { "Forced failure" });
            return true;


            diagnosticResult = null;
            return false;
        }

        private enum TargetSymbolTypes
        {
            Method = 1,
            Property = 2,
            Member = 4
        }
    }
}
