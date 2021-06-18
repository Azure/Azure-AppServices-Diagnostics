using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils.ScriptUtilities
{
    public class SolutionButtonOption
    {
        public string Label { get; set; }
        public SolutionButtonType Type { get; set; } = SolutionButtonType.Button;
        public SolutionButtonPosition Position { get; set; } = SolutionButtonPosition.Bottom;

        public SolutionButtonOption(string label, SolutionButtonType type = SolutionButtonType.Button, SolutionButtonPosition position = SolutionButtonPosition.Bottom)
        {
            this.Label = label;
            this.Type = type;
            this.Position = position;
        }
    }

    public enum SolutionButtonType
    {
        Button,
        Link,
    }

    public enum SolutionButtonPosition
    {
        Bottom = 0,
        NextToTitle = 1,
    }
}
