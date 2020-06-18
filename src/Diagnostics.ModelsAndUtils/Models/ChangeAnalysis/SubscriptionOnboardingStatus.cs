namespace Diagnostics.ModelsAndUtils.Models.ChangeAnalysis
{
    /// <summary>
    /// This class captures whether a given subscription has registered ChangeAnalysis RP.
    /// </summary>
    public class SubscriptionOnboardingStatus
    {
        /// <summary>
        /// Subscription Id.
        /// </summary>
        public string SubscriptionId;

        /// <summary>
        /// State of registration.
        /// </summary>
        public string State;

        /// <summary>
        /// If the service returns 404, we set isRegistered = false.
        /// </summary>
        public bool IsRegistered;
    }
}
