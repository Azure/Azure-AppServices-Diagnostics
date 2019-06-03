using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Diagnostics.Scripts.Models
{
    public class EntityMethodSignature
    {
        private readonly ImmutableArray<EntityParameter> _parameters;
        private readonly string _parentTypeName;
        private readonly string _methodName;
        private readonly string _returnTypeName;
        private readonly ImmutableArray<AttributeData> _attributes;

        public EntityMethodSignature(string methodName)
        {
            _methodName = methodName;
        }

        public EntityMethodSignature(string parentTypeName, string methodName, ImmutableArray<EntityParameter> parameters, string returnTypeName, ImmutableArray<AttributeData> attributes)
        {
            _parameters = parameters;
            _parentTypeName = parentTypeName;
            _returnTypeName = returnTypeName;
            _methodName = methodName;
            _attributes = attributes;
        }

        public ImmutableArray<EntityParameter> Parameters => _parameters;

        public string ParentTypeName => _parentTypeName;

        public string MethodName => _methodName;

        public string ReturnTypeName => _returnTypeName;

        public ImmutableArray<AttributeData> Attributes => _attributes;

        public MethodInfo GetMethod(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            //TODO: Remove comments when we are sure we are choosing the correct type

            //return assembly.DefinedTypes
            //    .FirstOrDefault(t => string.Compare(t.FullName, ParentTypeName, StringComparison.Ordinal) == 0)
            //    ?.GetMethod(MethodName);

            //return assembly.DefinedTypes.FirstOrDefault(t => t.DeclaringType == null)?.GetMethod(MethodName);

            foreach (var type in assembly.DefinedTypes.Where(t => t.DeclaringType == null))
            {
                var method = type.GetMethod(MethodName);
                if (method != null)
                {
                    return method;
                }
            }
            return null;
        }
    }
}
