namespace Diagnostics.Scripts.Models
{
    public sealed class EntityMetadata
    {
        public EntityType Type;

        public string ScriptText;
        public string Metadata;
        public string Sha;

        public EntityMetadata()
        {
        }

        public EntityMetadata(string scriptText, EntityType type = EntityType.Signal, string metadata = null, string sha = null)
        {
            ScriptText = scriptText;
            Metadata = metadata;
            Type = type;
            Sha = sha;
        }
    }
}
