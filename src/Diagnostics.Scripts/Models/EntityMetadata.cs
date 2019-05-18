using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.Scripts.Models
{
    public sealed class EntityMetadata
    {
        public EntityType Type;

        public string ScriptText;
        public string Metadata;

        public EntityMetadata()
        {
        }

        public EntityMetadata(string scriptText, EntityType type = EntityType.Signal, string metadata = null)
        {
            ScriptText = scriptText;
            Metadata = metadata;
            Type = type;
        }
    }
}
