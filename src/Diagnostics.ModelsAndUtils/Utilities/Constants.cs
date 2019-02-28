using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Utilities
{
    public static class SolutionConstants
    {
        public static readonly string AppRestartDescription = @"
 #### App Restart

 An App Restart will kill the app process on all instances.

 If your app is in a bad state, performing a web app restart can be enough to fix the problem in some cases.";
        public static readonly string RestartInstructions = " 1. Navigate to the resource in Azure Portal\n" +
            " 2. Press `Restart` to invoke a site restart";
        public static readonly string UpdateSettingsInstructions = " 1. Navigate to the resource in Azure Portal\n" +
            " 2. Navigate to the `Application Settings` tab\n" +
            " 3. Enter the following settings under the `Application Settings` section:";
    }
}
