using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Utilities
{
    public static class SolutionConstants
    {
        public static readonly string RestartDescription = "Restarting the site may cause application downtime";
        public static readonly string RestartInstructions = @"
    1. Navigate to the resource in Azure Portal
    2. Press `Restart` to invoke a site restart";
        public static readonly string UpdateSettingsInstructions = @"
    1. Navigate to the resource in Azure Portal
    2. Navigate to the `Application Settings` tab
    3. Enter the following settings under the `Application Settings` section:";
    }
}
