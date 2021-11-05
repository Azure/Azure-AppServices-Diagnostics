using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Diagnostics.Scripts.CompilationService.Utilities.Extensions;
using System.Text.RegularExpressions;
using Diagnostics.Scripts.CompilationService.Models;
using Diagnostics.Scripts.CompilationService.Utilities;
using Diagnostics.ModelsAndUtils.Models;

namespace Diagnostics.Scripts.CompilationService.CustomCodeAnalyzers
{
    [DiagnosticAnalyzer("C#")]
    public class BlockCallsAnalyzer : DiagnosticAnalyzer
    {
        public BlockConfig _blockConfig = null;
        public const string DEFAULT_INVOCATION_BLOCK_MESSAGE_FORMAT = "Invoking {0} from {1} is restricted. Please contact the AppLens team for additional help.";
        public const string DEFAULT_PROPERTY_ACCESS_BLOCK_MESSAGE_FORMAT = "Accessing {0} from {1} is restricted. Please contact the AppLens team for additional help.";
        public const string DEFAULT_INSTANTIATION_BLOCK_MESSAGE_FORMAT = "Creating an object of type {0} is restricted. Please contact the AppLens team for additional help.";

        private DiagnosticDescriptor blockCallsDiagnosticDescriptor = new DiagnosticDescriptor(id: "ApBlkP001", title: "Forbidden call detected.", 
            messageFormat:"{0}", category: "ApBlk001:AppLensBlockedAccess",defaultSeverity:DiagnosticSeverity.Error, isEnabledByDefault:true);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create<DiagnosticDescriptor>(blockCallsDiagnosticDescriptor); // throw new NotImplementedException();

        public override void Initialize(AnalysisContext context)
        {
            if (APIBlockConfigProvider.GetConfig != null)
            {
                _blockConfig = APIBlockConfigProvider.GetConfig;
                context.EnableConcurrentExecution();
                context.RegisterSymbolAction(context => {

                    var methodSymbol = (IMethodSymbol)context.Symbol;
                    var k = context.Symbol.Kind;
                    var n = methodSymbol.Name;
                }, SymbolKind.Method);
                context.RegisterOperationBlockStartAction(context =>
                {
                    context.RegisterOperationAction(HandleMemberReference, ImmutableArray.Create<OperationKind>(
                        OperationKind.EventReference,
                        OperationKind.FieldReference,
                        OperationKind.MethodReference,
                        OperationKind.PropertyReference));

                    context.RegisterOperationAction(HandleInvocation, ImmutableArray.Create<OperationKind>(OperationKind.Invocation));
                    context.RegisterOperationAction(HandleObjectCreation, ImmutableArray.Create<OperationKind>(OperationKind.ObjectCreation));
                });
            }
        }


        private void HandleMemberReference(OperationAnalysisContext operationContext)
        {
            var memberReference = (IMemberReferenceOperation)operationContext.Operation;

            if (_blockConfig != null)
            {
                #region Handle property reference
                string containingType = memberReference.Member.ContainingType.ToString();
                if (!string.IsNullOrWhiteSpace(containingType) && _blockConfig.MatchesClassToBlock(containingType))
                {
                    _blockConfig.GetMatchingClassConfig(containingType)?.ForEach(cConfig => {
                        if (cConfig.MethodsToBlock?.Count > 0)
                        {
                            BlockConfig.GetMatchingBlockMessageList(cConfig.PropertiesToBlock, memberReference.Member.Name)?.ForEach(strMessage =>
                            {
                                operationContext.ReportDiagnostic(memberReference.CreateDiagnostic(blockCallsDiagnosticDescriptor,
                                        string.IsNullOrWhiteSpace(strMessage) ? new string[] { string.Format(DEFAULT_INVOCATION_BLOCK_MESSAGE_FORMAT, memberReference.Member.Name, containingType) } : new string[] { strMessage }
                                        ));
                            });
                        }
                        else
                        {
                            //No specific methods to block was indicated, however the type name did match the config, so the default action is to block all methods from this type
                            operationContext.ReportDiagnostic(memberReference.CreateDiagnostic(blockCallsDiagnosticDescriptor,
                                new string[] { string.Format(DEFAULT_PROPERTY_ACCESS_BLOCK_MESSAGE_FORMAT, "any property", containingType) }
                                ));
                        }
                    });
                }
                #endregion
            }

        }

        private void HandleInvocation(OperationAnalysisContext operationContext)
        {
            var invocation = (IInvocationOperation)operationContext.Operation;
            if (_blockConfig != null)
            {
                #region Handle PInvoke method calls
                if (invocation.TargetMethod.IsExtern && invocation.TargetMethod.GetDllImportData() != null)
                {
                    //This is a PInvoke call
                    string targetPInvokeDll = invocation.TargetMethod.GetDllImportData().ModuleName;
                    if (_blockConfig.MatchesPInvokeToBlock(targetPInvokeDll))
                    {
                        //Current DLL name is in the list of PInvoke DLL's that should be blocked.
                        //Evaluate if the specific call is blocked

                        _blockConfig.GetMatchingPInvokeConfig(targetPInvokeDll)?.ForEach(piConfig => {
                            if (piConfig.MethodsToBlock?.Count > 0)
                            {
                                string pInvokeFunctionName = !string.IsNullOrWhiteSpace(invocation.TargetMethod.GetDllImportData().EntryPointName)? invocation.TargetMethod.GetDllImportData().EntryPointName : invocation.TargetMethod.Name;

                                BlockConfig.GetMatchingBlockMessageList(piConfig.MethodsToBlock, pInvokeFunctionName)?.ForEach(strMessage =>
                                {
                                    operationContext.ReportDiagnostic(invocation.CreateDiagnostic(blockCallsDiagnosticDescriptor,
                                        string.IsNullOrWhiteSpace(strMessage) ? new string[] { string.Format(DEFAULT_INVOCATION_BLOCK_MESSAGE_FORMAT, invocation.TargetMethod.Name, targetPInvokeDll) } : new string[] { strMessage }
                                        ));
                                });
                            }
                            else
                            {
                                //No specific methods to block was indicated, however the DLL name did match the config, so the default action is to block all methods from this DLL
                                operationContext.ReportDiagnostic(invocation.CreateDiagnostic(blockCallsDiagnosticDescriptor,
                                    string.IsNullOrWhiteSpace(piConfig.MessageToShowWhenBlocked) ? new string[] { string.Format(DEFAULT_INVOCATION_BLOCK_MESSAGE_FORMAT, "any method", targetPInvokeDll) } : new string[] { piConfig.MessageToShowWhenBlocked }
                                    ));
                            }
                        });
                    }
                }
                #endregion

                #region Handle class and instance method invocation
                string containingType = invocation.TargetMethod.ContainingType.ToString();
                if (!string.IsNullOrWhiteSpace(containingType) && _blockConfig.MatchesClassToBlock(containingType))
                {
                    _blockConfig.GetMatchingClassConfig(containingType)?.ForEach(cConfig => {
                        if (cConfig.MethodsToBlock?.Count > 0)
                        {
                            BlockConfig.GetMatchingBlockMessageList(cConfig.MethodsToBlock, invocation.TargetMethod.Name)?.ForEach(strMessage => 
                            {
                                operationContext.ReportDiagnostic(invocation.CreateDiagnostic(blockCallsDiagnosticDescriptor,
                                        string.IsNullOrWhiteSpace(strMessage) ? new string[] { string.Format(DEFAULT_INVOCATION_BLOCK_MESSAGE_FORMAT, invocation.TargetMethod.Name, containingType) } : new string[] { strMessage }
                                        ));
                            });
                        }
                        else
                        {
                            //No specific methods to block was indicated, however the type name did match the config, so the default action is to block all methods from this type
                            operationContext.ReportDiagnostic(invocation.CreateDiagnostic(blockCallsDiagnosticDescriptor,
                                new string[] { string.Format(DEFAULT_INVOCATION_BLOCK_MESSAGE_FORMAT, "any method", containingType) } 
                                ));
                        }
                    });
                }

                containingType = invocation.Type.ToString(); //This is required to evaluate expressions where invocations take a generic Type as a parameter
                if (!string.IsNullOrWhiteSpace(containingType) && _blockConfig.MatchesClassToBlock(containingType))
                {
                    bool blockedObjectCreation = false;
                    foreach (ClassBlockConfig classConfig in _blockConfig.GetMatchingClassConfig(containingType)?.Where(cConfig => cConfig?.IsObjectCreationBlocked == true))
                    {
                        blockedObjectCreation = true;
                        operationContext.ReportDiagnostic(invocation.CreateDiagnostic(blockCallsDiagnosticDescriptor,
                            string.IsNullOrWhiteSpace(classConfig.MessageToShowWhenBlocked) ? new string[] { string.Format(DEFAULT_INSTANTIATION_BLOCK_MESSAGE_FORMAT, containingType) } : new string[] { classConfig.MessageToShowWhenBlocked }
                            ));
                    }
                    if(!blockedObjectCreation)
                    {
                        _blockConfig.GetMatchingClassConfig(containingType)?.ForEach(cConfig => {
                            if (cConfig.MethodsToBlock?.Count > 0)
                            {
                                BlockConfig.GetMatchingBlockMessageList(cConfig.MethodsToBlock, invocation.TargetMethod.Name)?.ForEach(strMessage =>
                                {
                                    operationContext.ReportDiagnostic(invocation.CreateDiagnostic(blockCallsDiagnosticDescriptor,
                                            string.IsNullOrWhiteSpace(strMessage) ? new string[] { string.Format(DEFAULT_INVOCATION_BLOCK_MESSAGE_FORMAT, invocation.TargetMethod.Name, containingType) } : new string[] { strMessage }
                                            ));
                                });
                            }
                            else
                            {
                                //No specific methods to block was indicated, however the type name did match the config, so the default action is to block all methods from this type
                                operationContext.ReportDiagnostic(invocation.CreateDiagnostic(blockCallsDiagnosticDescriptor,
                                    new string[] { string.Format(DEFAULT_INVOCATION_BLOCK_MESSAGE_FORMAT, "any method", containingType) }
                                    ));
                            }
                        });
                    }
                }

                #endregion 
            }
        }

        private void HandleObjectCreation(OperationAnalysisContext operationContext)
        {
            var objectCreation = (IObjectCreationOperation)operationContext.Operation;
            if (_blockConfig != null)
            {
                #region Handle object instantiation
                string typeBeingInstantiated = objectCreation.Type.ToString();
                if (!string.IsNullOrWhiteSpace(typeBeingInstantiated) && _blockConfig.MatchesClassToBlock(typeBeingInstantiated))
                {
                    foreach (ClassBlockConfig classConfig in _blockConfig.GetMatchingClassConfig(typeBeingInstantiated)?.Where(cConfig => cConfig?.IsObjectCreationBlocked == true))
                    {
                        operationContext.ReportDiagnostic(objectCreation.CreateDiagnostic(blockCallsDiagnosticDescriptor,
                            string.IsNullOrWhiteSpace(classConfig.MessageToShowWhenBlocked)? new string[] { string.Format(DEFAULT_INSTANTIATION_BLOCK_MESSAGE_FORMAT, typeBeingInstantiated) } : new string[] { classConfig.MessageToShowWhenBlocked }
                            ));
                    }
                }
                #endregion
            }
        }
    }
}
