using Diagnostics.Scripts.CompilationService.Interfaces;
using Diagnostics.Scripts.Models;
using Microsoft.CodeAnalysis.Scripting;
using System;

namespace Diagnostics.Scripts.CompilationService
{
    public static class CompilationServiceFactory
    {
        public static ICompilationService CreateService(EntityMetadata metaData, ScriptOptions scriptOptions)
        {
            if(metaData == null)
            {
                throw new ArgumentNullException("Entity Metadata cannot be null.");
            }

            if(scriptOptions == null)
            {
                throw new ArgumentNullException("ScriptOptions cannot be null.");
            }

            switch (metaData.Type)
            {
                case EntityType.Signal:
                    return new SignalCompilationService(metaData, scriptOptions);
                case EntityType.Detector:
                    return new DetectorCompilationService(metaData, scriptOptions);
                case EntityType.Analysis:
                    return new AnalysisCompilationService(metaData, scriptOptions);
                default:
                    throw new NotSupportedException($"EntityMetaData with type {metaData.Type} is invalid.");
            }
        }
    }
}
