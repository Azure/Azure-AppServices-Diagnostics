using Microsoft.CodeAnalysis;

namespace Diagnostics.Scripts.Models
{
    public class EntityParameter
    {
        public EntityParameter(string name, string typeName, bool isOptional, RefKind refkind)
        {
            Name = name;
            TypeName = typeName;
            IsOptional = isOptional;
            RefKind = refkind;
        }

        public string Name { get; }

        public string TypeName { get; }

        public bool IsOptional { get; }

        public RefKind RefKind { get; }
    }
}
