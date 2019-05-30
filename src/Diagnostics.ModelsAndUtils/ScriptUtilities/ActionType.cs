namespace Diagnostics.ModelsAndUtils.ScriptUtilities
{
    public enum ActionType
    {
        ArmApi,
        OpenTab,
        GoToBlade
    }

    public class ArmApiOptions
    {
        public string Route { get; set; }

        public string Verb { get; set; }
    }

    public class OpenTabOptions
    {
        public string TabUrl { get; set; }
    }

    public class GoToBladeOptions
    {
        public string DetailBlade { get; set; }

        public object DetailBladeInputs { get; set; }

        public string Extension { get; set; }
    }
}
