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

        public EntityMetadata(string scriptText, string lastModifiedMarker, EntityType type = EntityType.Signal, string metadata = null)
        {
            ScriptText = scriptText;
            Metadata = metadata;
            Type = type;
            LastModifiedMarker = lastModifiedMarker;
        }
    }
}
