using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace Diagnostics.RuntimeHost.Models
{
    public enum UserAccessStatus
    {
        Unauthorized,

        Forbidden,

        NotFound,

        BadRequest,

        ResourceNotRelatedToCase,

        RequestFailure,

        SGMembershipNeeded,

        CaseNumberNeeded,

        HasAccess
    }

    public class UserAccessResponse
    {
        public UserAccessStatus Status { get; set; }
        public bool HasCustomerConsent { get; set; }
        public string DetailText { get; set; }
    }

    public enum AccessLevel
    {
        MSaaS,
        All
    }

    public class DurianConfig
    {
        public bool IsEnabled { get; set; } = false;
        public bool EnforceValidCase { get; set; } = true;
        public bool EnforceCaseAccess { get; set; } = false;
        public bool EnforceResourceAccess { get; set; } = false;
        public bool EnforceCustomerConsent { get; set; } = true;
        public string EnforcedOnClients { get; set; }
        public List<string> EnforcedClientCerts { get; set; }
        public string AllowedAppIdsForUserToken { get; set; }
        public List<string> TrustedAppIdsForUserToken { get; set; }
        public string GraphAPIClientId { get; set; }
        public string GraphAPIClientSecret { get; set; }
        public string UserAccessRequirements { get; set; }
        public string MsaasUrl { get; set; }
        public string MsaasClientId { get; set; }
        public string MsaasClientSecret { get; set; }

        public DurianConfig(IConfiguration configuration)
        {
            configuration.Bind("Durian", this);
            IsEnabled = configuration.GetValue("Durian:IsEnabled", false);
            EnforceValidCase = configuration.GetValue("Durian:EnforceValidCase", true);
            EnforceCaseAccess = configuration.GetValue("Durian:EnforceCaseAccess", false);
            EnforceResourceAccess = configuration.GetValue("Durian:EnforceResourceAccess", false);
            EnforceCustomerConsent = configuration.GetValue("Durian:EnforceCustomerConsent", true);
        }
    }

    /// <summary>
    /// Security Group Configuration.
    /// </summary>
    public class SecurityGroupConfig
    {
        /// <summary>
        /// Gets or sets Name of Security Group.
        /// </summary>
        public string GroupName { get; set; }

        /// <summary>
        /// Gets or sets Object Id of Security Group.
        /// </summary>
        public string GroupId { get; set; }

        /// <summary>
        /// Gets or sets Access Level of Security Group.
        /// </summary>
        public AccessLevel GroupAccessLevel { get; set; }
    }

    /// <summary>
    /// User Auth Response
    /// </summary>
    public class UserAuthResponse
    {
        /// <summary>
        /// Gets or sets success status of response
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Gets or sets status of response
        /// </summary>
        public int HttpStatus { get; set; }

        /// <summary>
        /// Gets or sets detailed access response in case there is one
        /// </summary>
        public UserAccessResponse AccessResponse { get; set; }
    }

    class CachedUserInfo
    {
        public string UserId { get; set; }

        public SecurityGroupConfig SecurityGroup { get; set; }
        public long UserAddedTimestamp { get; set; }

        public List<CachedResourceInfo> CachedResources { get; set; }
    }

    class CachedResourceInfo
    {
        public string ResourceId { get; set; }

        public string SubscriptionId { get; set; }

        public bool HasAccess { get; set; }

        public bool HasCustomerConsent { get; set; }

    }
}
