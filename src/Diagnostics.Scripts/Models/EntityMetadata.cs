using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.Scripts.Models
{
    public sealed class EntityMetadata
    {
        public EntityType Type;

        public string ScriptText;

        public EntityMetadata()
        {
        }

        public EntityMetadata(string scriptText, EntityType type = EntityType.Signal)
        {
            ScriptText = scriptText;
            Type = type;
        }
    }
}
