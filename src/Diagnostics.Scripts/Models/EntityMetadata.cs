namespace Diagnostics.Scripts.Models
{
    public sealed class EntityMetadata
    {
        public EntityType Type;

        public string ScriptText;
        public string Metadata;
        public string LastModifiedMarker;

        public EntityMetadata()
        {
        }

        public EntityMetadata(string scriptText, EntityType type = EntityType.Signal, string metadata = null, string lastModifiedMarker = null)
        {
            ScriptText = scriptText;
            Metadata = metadata;
            Type = type;
            LastModifiedMarker = lastModifiedMarker;
        }
    }
}
