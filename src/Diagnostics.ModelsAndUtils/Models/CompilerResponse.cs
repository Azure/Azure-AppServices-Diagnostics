using Diagnostics.ModelsAndUtils.Models.ResponseExtensions;
using System.Collections.Generic;

namespace Diagnostics.ModelsAndUtils.Models
{
    public class CompilerResponse
    {
        public bool CompilationSucceeded;

        public IEnumerable<string> CompilationTraces;
        public IEnumerable<CompilationTraceOutputDetails> DetailedCompilationTraces;

        public IEnumerable<string> References;

        public string AssemblyBytes;

        public string PdbBytes;

        public string AssemblyName;
    }

    public class CompilationTraceOutputDetails
    {
        public InsightStatus Severity { get; set; } 
        public string Message { get; set; }
        public LocationSpan? Location { get; set; }

        public static IEnumerable<CompilationTraceOutputDetails> GetCompilationTraceDetailsList(IEnumerable<string> compilationOutput, InsightStatus defaultStatusToUse = InsightStatus.Critical)
        {
            List<CompilationTraceOutputDetails> returnObj = new List<CompilationTraceOutputDetails>();
            foreach (string str in compilationOutput)
            {
                returnObj.Add(new CompilationTraceOutputDetails() { 
                    Severity = defaultStatusToUse,
                    Message = str,
                    Location = new LocationSpan() { 
                        Start = new Position() { LinePos = 0, ColPos = 0},
                        End = new Position() { LinePos = 0, ColPos = 0 },
                    }
                });
            }
            return returnObj;
        }
    }

    public class LocationSpan
    { 
        public Position Start { get; set; }
        public Position End { get; set; }
    }
    public class Position
    { 
        public int LinePos { get; set; }
        public int ColPos { get; set; }

    }

}
