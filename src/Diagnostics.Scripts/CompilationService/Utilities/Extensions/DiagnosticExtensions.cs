using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.ModelsAndUtils.Models.ResponseExtensions;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Diagnostics.Scripts.CompilationService.Utilities.Extensions
{
#nullable enable
    internal static class DiagnosticExtensions
    {

        public static CompilationTraceOutputDetails GetCompilationTraceOutputDetails(this Diagnostic d)
        {
            if (d == null) return null;
            CompilationTraceOutputDetails traceDetail = new CompilationTraceOutputDetails();

            if (d.Severity == DiagnosticSeverity.Error) traceDetail.Severity = InsightStatus.Critical;
            if (d.Severity == DiagnosticSeverity.Warning) traceDetail.Severity = InsightStatus.Warning;
            if (d.Severity == DiagnosticSeverity.Info) traceDetail.Severity = InsightStatus.Info;
            if (d.Severity == DiagnosticSeverity.Hidden) traceDetail.Severity = InsightStatus.None;

            traceDetail.Message = d.ToString().Replace("ApBlkP", "AppBlkP0", StringComparison.Ordinal);

            traceDetail.Location = new LocationSpan();
            if (d.Location != null)
            {
                traceDetail.Location.Start = new Position()
                {
                    LinePos = d.Location.GetLineSpan().StartLinePosition.Line,
                    ColPos = d.Location.GetLineSpan().StartLinePosition.Character
                };

                traceDetail.Location.End = new Position()
                {
                    LinePos = d.Location.GetLineSpan().EndLinePosition.Line,
                    ColPos = d.Location.GetLineSpan().EndLinePosition.Character
                };
            }
            else
            {
                traceDetail.Location.Start = new Position() { LinePos = 0, ColPos = 0 };
                traceDetail.Location.End = new Position() { LinePos = 0, ColPos = 0 };
            }
            return traceDetail;
        }

        public static int CompareTo(this Diagnostic x, Diagnostic? y)
        {
            if (y == null) return -1;
            if (x.Severity == y.Severity)
            {                
                if (x.Location == null || y.Location == null)
                {
                    if (x.Location == null && y.Location == null)
                    {
                        return 0;
                    }
                    else
                    {
                        if (x.Location == null && y.Location != null)
                        {
                            return 1;
                        }
                        else
                        {
                            //(x.Location != null && y.Location == null)
                            return -1;
                        }
                        
                    }
                    
                }
                else
                {
                    if (x.Location.GetLineSpan().StartLinePosition.Line == y.Location.GetLineSpan().StartLinePosition.Line)
                    {
                        if (x.Location.GetLineSpan().StartLinePosition.Character == y.Location.GetLineSpan().StartLinePosition.Character)
                        {
                            return string.Compare(x.ToString(), y.ToString(), StringComparison.OrdinalIgnoreCase);
                        }
                        else
                        {
                            return x.Location.GetLineSpan().StartLinePosition.Character.CompareTo(y.Location.GetLineSpan().StartLinePosition.Character);
                        }
                    }
                    else
                    {
                        return x.Location.GetLineSpan().StartLinePosition.Line.CompareTo(y.Location.GetLineSpan().StartLinePosition.Line);
                    }
                }                
            }
            else
            {
                //Compare for severity Decending
                return y.Severity.CompareTo(x.Severity);
                //return x.Severity.CompareTo(y.Severity);
            }
        }
        public static Diagnostic CreateDiagnostic(this IOperation operation, DiagnosticDescriptor rule, params object[] args) 
            => operation.CreateDiagnostic(rule, properties: null, args);

        public static Diagnostic CreateDiagnostic(this IOperation operation, DiagnosticDescriptor rule, ImmutableDictionary<string, string?>? properties, params object[] args)
            => operation.Syntax.CreateDiagnostic(rule, properties, args);
        

        public static Diagnostic CreateDiagnostic(this IOperation operation, DiagnosticDescriptor rule, ImmutableArray<Location> additionalLocations, ImmutableDictionary<string, string?>? properties, params object[] args)
            => operation.Syntax.CreateDiagnostic(rule, additionalLocations, properties, args);

        public static Diagnostic CreateDiagnostic(this ISymbol symbol, DiagnosticDescriptor rule, params object[] args)
            => symbol.Locations.CreateDiagnostic(rule, args);

        public static Diagnostic CreateDiagnostic(this ISymbol symbol, DiagnosticDescriptor rule, ImmutableDictionary<string, string?>? properties, params object[] args)
            => symbol.Locations.CreateDiagnostic(rule, properties, args);

        public static Diagnostic CreateDiagnostic(this SyntaxNode node, DiagnosticDescriptor rule, params object[] args) 
            => node.CreateDiagnostic(rule, properties: null, args);

        public static Diagnostic CreateDiagnostic(this SyntaxNode node, DiagnosticDescriptor rule, ImmutableDictionary<string, string?>? properties, params object[] args) 
            => node.CreateDiagnostic(rule, additionalLocations: ImmutableArray<Location>.Empty, properties, args);

        public static Diagnostic CreateDiagnostic(this SyntaxNode node, DiagnosticDescriptor rule, ImmutableArray<Location> additionalLocations, ImmutableDictionary<string, string?>? properties, params object[] args) 
            => node.GetLocation().CreateDiagnostic( rule: rule, additionalLocations: additionalLocations, properties: properties, args: args);

        public static Diagnostic CreateDiagnostic(this Location location, DiagnosticDescriptor rule, params object[] args) 
            => location.CreateDiagnostic(rule: rule, properties: ImmutableDictionary<string, string?>.Empty, args: args);

        public static Diagnostic CreateDiagnostic( this Location location, DiagnosticDescriptor rule, ImmutableDictionary<string, string?>? properties, params object[] args) 
            => location.CreateDiagnostic(rule, ImmutableArray<Location>.Empty, properties, args);

        public static Diagnostic CreateDiagnostic(this Location location,  DiagnosticDescriptor rule, ImmutableArray<Location> additionalLocations, ImmutableDictionary<string, string?>? properties, params object[] args)
        {
            if (!location.IsInSource)
            {
                location = Location.None;
            }

            return Diagnostic.Create(
                descriptor: rule,
                location: location,
                additionalLocations: additionalLocations,
                properties: properties,
                messageArgs: args);
        }

        public static Diagnostic CreateDiagnostic(this IEnumerable<Location> locations, DiagnosticDescriptor rule, params object[] args)
            => locations.CreateDiagnostic(rule, null, args);

        public static Diagnostic CreateDiagnostic(this IEnumerable<Location> locations, DiagnosticDescriptor rule, ImmutableDictionary<string, string?>? properties, params object[] args)
        {
            IEnumerable<Location> inSource = locations.Where(l => l.IsInSource);
            if (!inSource.Any())
            {
                return Diagnostic.Create(rule, null, args);
            }

            return Diagnostic.Create(rule, location: inSource.First(), additionalLocations: inSource.Skip(1), properties: properties, messageArgs: args);
        }

    }
#nullable disable
}
