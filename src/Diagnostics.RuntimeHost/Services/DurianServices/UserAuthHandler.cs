using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Diagnostics.RuntimeHost.Models;
using Diagnostics.RuntimeHost.Utilities;
using Newtonsoft.Json;

namespace Diagnostics.RuntimeHost.Services
{
    public interface IUserAuthHandler
    {
        bool IsEnabled();
        bool ClientHasDurianEnforced(string clientCertSubjectName);
        Task<UserAuthResponse> HandleRequirementAsync(HttpContext context);
    }

    public class UserAuthHandlerNationalCloud : IUserAuthHandler
    {
        public bool IsEnabled()
        {
            return false;
        }

        public bool ClientHasDurianEnforced(string clientCertSubjectName)
        {
            return false;
        }

        public async Task<UserAuthResponse> HandleRequirementAsync(HttpContext context)
        {
            // Not required in national cloud, so succeed always
            return new UserAuthResponse()
            {
                IsSuccess = true
            };
        }
    }

    public class DefaultUserAuthHandler : IUserAuthHandler
    {
        public bool IsEnabled()
        {
            return false;
        }

        public bool ClientHasDurianEnforced(string clientCertSubjectName)
        {
            return false;
        }

        public async Task<UserAuthResponse> HandleRequirementAsync(HttpContext context)
        {
            return new UserAuthResponse()
            {
                IsSuccess = true
            };
        }
    }

    /// <summary>
    /// Security Group Authorization Handler.
    /// </summary>
    public class UserAuthHandler : IUserAuthHandler
    {
        //private bool isDurianEnabled = false;
        private DurianConfig Config = null;
        private readonly string graphUrl = "https://graph.microsoft.com/v1.0/users/{0}/checkMemberGroups";
        private readonly int loggedInUserCacheClearIntervalInMs = 60 * 60 * 1000; // 1 hour
        private readonly int loggedInUserExpiryIntervalInSeconds = 6 * 60 * 60; // 6 hours
        private Dictionary<string, SecurityGroupConfig> groupIdToName;
        private string[] allGroupIds;
        private List<SecurityGroupConfig> securityGroupConfigs;
        private Dictionary<string, CachedUserInfo> loggedInUsersCache;

        protected IMsaasHandler _msaasHandler;

        public bool IsEnabled()
        {
            return Config.IsEnabled;
        }

        public bool ClientHasDurianEnforced(string clientCertSubjectName)
        {
            if (Config.IsEnabled && Config.EnforcedClientCerts != null)
            {
                return Config.EnforcedClientCerts.Contains(clientCertSubjectName, StringComparer.OrdinalIgnoreCase);
            }
            return false;
        }

        public UserAuthHandler(IConfiguration configuration, IServiceProvider services)
        {
            loggedInUsersCache = new Dictionary<string, CachedUserInfo>();
            groupIdToName = new Dictionary<string, SecurityGroupConfig>();
            securityGroupConfigs = new List<SecurityGroupConfig>();
            Config = new DurianConfig(configuration);

            //isDurianEnabled = configuration.GetValue("Durian:IsEnabled", false);

            if (Config.IsEnabled)
            {
                string sgConfigs = Config.UserAccessRequirements;
                try
                {
                    if (Config.UserAccessRequirements != null) securityGroupConfigs = JsonConvert.DeserializeObject<List<SecurityGroupConfig>>(sgConfigs);
                    Config.EnforcedClientCerts = Config.EnforcedOnClients != null? Config.EnforcedOnClients.Split(',').ToList(): new List<string>();

                    //Create a map from group ids to group info
                    foreach (SecurityGroupConfig sgCfg in securityGroupConfigs)
                    {
                        if (!string.IsNullOrEmpty(sgCfg.GroupId) && !string.IsNullOrEmpty(sgCfg.GroupName) && !groupIdToName.TryGetValue(sgCfg.GroupId, out var val))
                        {
                            groupIdToName.Add(sgCfg.GroupId, sgCfg);
                        }
                    }
                }
                catch (Exception ex)
                {
                    //TODO: Log this exception
                    throw new Exception("An exception occurred in application initialization. Please check application settings.");
                }
                allGroupIds = groupIdToName.Keys.ToArray();


                RecycleLoggedInUserCache();
                _msaasHandler = (IMsaasHandler)services.GetService(typeof(IMsaasHandler));
            }
        }

        /// <summary>
        /// Task to clear users from cache at regular interval.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task RecycleLoggedInUserCache()
        {
            while (true)
            {
                long now = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                foreach (KeyValuePair<string, CachedUserInfo> userCache in loggedInUsersCache)
                {
                    if ((now - userCache.Value.UserAddedTimestamp) > loggedInUserExpiryIntervalInSeconds)
                    {
                        // Pop out user from logged in users list
                        loggedInUsersCache.Remove(userCache.Key);
                    }
                }

                await Task.Delay(loggedInUserCacheClearIntervalInMs);
            }
        }

        /// <summary>
        /// Adds user to cached dictionary.
        /// </summary>
        /// <param name="userId">userId.</param>
        /// <param name="securityGroup">securityGroup.</param>
        /// <param name="resourceInfo">resourceInfo.</param>
        private void AddUserToCache(string userId, SecurityGroupConfig securityGroup, CachedResourceInfo resourceInfo)
        {
            CachedUserInfo userInfo;
            if (loggedInUsersCache.TryGetValue(userId, out userInfo))
            {
                if (resourceInfo != null && securityGroup.GroupAccessLevel == AccessLevel.MSaaS)
                {
                    if (userInfo.CachedResources == null)
                    {
                        userInfo.CachedResources = new List<CachedResourceInfo>();
                    }
                    userInfo.CachedResources.Add(resourceInfo);
                }
            }
            else
            {
                long ts = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                List<CachedResourceInfo> cachedResources = (resourceInfo != null && securityGroup != null && securityGroup.GroupAccessLevel == AccessLevel.MSaaS) ? new List<CachedResourceInfo>() { resourceInfo } : null;
                userInfo = new CachedUserInfo()
                {
                    UserId = userId,
                    SecurityGroup = securityGroup,
                    UserAddedTimestamp = ts,
                    CachedResources = cachedResources
                };
                loggedInUsersCache.Add(userId, userInfo);
            }
        }

        /// <summary>
        /// Gets cached user info if user exists.
        /// </summary>
        /// <param name="userId">userId.</param>
        /// <returns>cached user info.</returns>
        private CachedUserInfo GetUserFromCache(string userId)
        {
            if (loggedInUsersCache.TryGetValue(userId, out var userInfo))
            {
                return userInfo;
            }
            return null;
        }

        /// <summary>
        /// Checks cached dictionary to find if user exists.
        /// </summary>
        /// <param name="userId">userId.</param>
        /// <returns>boolean value.</returns>
        private bool IsUserInCache(string userId)
        {
            return loggedInUsersCache.TryGetValue(userId, out var userInfo);
        }

        /// <summary>
        /// Checks if a user is part of a security group
        /// </summary>
        /// <param name="userId">UserId.</param>
        /// <param name="securityGroupObjectId">Security Group Object Id.</param>
        /// <returns>Boolean.</returns>
        private async Task<CachedUserInfo> GetSecurityGroupMembership(string userId)
        {
            //check in existing cache
            var userInfo = GetUserFromCache(userId);
            if (userInfo != null) return userInfo;

            //call Graph API to get user SG membership
            var userSGMemberships = await GraphAPIUtils.CheckUserGroupMemberships(userId, allGroupIds, true);
            if (userSGMemberships.Length > 0)
            {
                AccessLevel highestLevel = 0;
                SecurityGroupConfig groupConfigHL = null;
                foreach (var groupId in userSGMemberships)
                {
                    if (groupIdToName.TryGetValue(groupId, out var val))
                    {
                        if (val.GroupAccessLevel >= highestLevel)
                        {
                            highestLevel = val.GroupAccessLevel;
                            groupConfigHL = val;
                        }
                    }
                }
                if (groupConfigHL != null)
                {
                    AddUserToCache(userId, groupConfigHL, null);
                }
            }
            else
            {
                AddUserToCache(userId, null, null);
            }
            return GetUserFromCache(userId);
        }
        public async Task<bool> CheckUserNeedsCaseNumber(string userId)
        {
            var userSgInfo = await GetSecurityGroupMembership(userId);
            if (userSgInfo.SecurityGroup.GroupAccessLevel != AccessLevel.All)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Handles authorization and checks if required policies are met.
        /// </summary>
        /// <param name="context">context.</param>
        /// <param name="requirement">requirement.</param>
        /// <returns>Authorization Status.</returns>
        public async Task<UserAuthResponse> HandleRequirementAsync(HttpContext httpContext)
        {
            bool hasAccess = false;
            bool hasCustomerConsent = false;
            UserAccessResponse userAccessResponse = new UserAccessResponse();
            string userId = null;
            string failureMessage = null;
            int responseStatus = -1;
            CachedUserInfo userInfo = null;
            try
            {
                string userToken = httpContext.Request.Headers["x-ms-user-token"].ToString();
                if (!string.IsNullOrEmpty(userToken))
                {
                    userId = GraphAPIUtils.GetUserIdFromToken(userToken);
                    if (!string.IsNullOrEmpty(userId))
                    {
                        userInfo = await GetSecurityGroupMembership(userId);
                        if (userInfo != null && userInfo.SecurityGroup != null)
                        {
                            if (userInfo.SecurityGroup.GroupAccessLevel == AccessLevel.All)
                            {
                                userAccessResponse.Status = UserAccessStatus.HasAccess;
                                userAccessResponse.HasCustomerConsent = true;
                            }

                            else if (userInfo.SecurityGroup.GroupAccessLevel == AccessLevel.MSaaS)
                            {
                                string resourceId = httpContext.Request.Path.Value.ToLower().Trim('/');
                                bool isAccessCheckPath = (resourceId == $"{UriElements.Durian}/{UriElements.CheckUserAccess}".ToLower());
                                if (isAccessCheckPath)
                                {
                                    return new UserAuthResponse()
                                    {
                                        IsSuccess = false,
                                        HttpStatus = 200,
                                        AccessResponse = new UserAccessResponse()
                                        {
                                            Status = UserAccessStatus.CaseNumberNeeded,
                                            DetailText = "User is not allowed to access this resource without a case number and the request does not contain a valid value for case number."
                                        }
                                    };
                                }
                                string caseNumber = httpContext.Request.Headers["x-ms-customer-casenumber"].ToString();
                                if (string.IsNullOrEmpty(caseNumber))
                                {
                                    responseStatus = 400;
                                    userAccessResponse.Status = UserAccessStatus.CaseNumberNeeded;
                                    userAccessResponse.DetailText = "User is not allowed to access this resource without a case number and the request does not contain a valid value for case number.";
                                }
                                else
                                {
                                    var splits = resourceId.Split("/detectors"); //TODO: also split on diagnostics/query and diagnostics/publish
                                    if (splits.Length > 0)
                                    {
                                        resourceId = splits[0];
                                    }
                                    CachedResourceInfo resInfo = null;
                                    if (userInfo.CachedResources != null && userInfo.CachedResources.Count > 0)
                                    {
                                        resInfo = userInfo.CachedResources.Find(x => x.ResourceId == resourceId);
                                    }
                                    // If found in cache
                                    if (resInfo != null && resInfo.HasAccess)
                                    {
                                        userAccessResponse.Status = UserAccessStatus.HasAccess;
                                        userAccessResponse.HasCustomerConsent = resInfo.HasCustomerConsent;
                                    }

                                    else
                                    {
                                        // If not found in cache or has no access
                                        // TODO: MSaaS Handler will throw exceptions which should be handled below
                                        userAccessResponse = await _msaasHandler.CheckUserAccess(userId, caseNumber, resourceId);
                                        if (userAccessResponse.Status == UserAccessStatus.HasAccess)
                                        {
                                            resInfo = new CachedResourceInfo()
                                            {
                                                ResourceId = resourceId,
                                                HasAccess = true,
                                                HasCustomerConsent = userAccessResponse.HasCustomerConsent,
                                                SubscriptionId = ""
                                            };
                                            AddUserToCache(userInfo.UserId, userInfo.SecurityGroup, resInfo);
                                        }
                                        else
                                        {
                                            switch (userAccessResponse.Status)
                                            {
                                                case UserAccessStatus.Unauthorized:
                                                case UserAccessStatus.NotFound:
                                                case UserAccessStatus.ResourceNotRelatedToCase:
                                                    responseStatus = 401; break;
                                                case UserAccessStatus.Forbidden:
                                                    responseStatus = 403; break;
                                                case UserAccessStatus.RequestFailure:
                                                    responseStatus = 424; break;
                                                default:
                                                    break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            responseStatus = 401;
                            userAccessResponse.Status = UserAccessStatus.SGMembershipNeeded;
                            userAccessResponse.DetailText = "User needs to be part of one of the security groups needed to access applens";
                        }
                    }
                }
                else
                {
                    responseStatus = 401;
                    userAccessResponse.Status = UserAccessStatus.Unauthorized;
                    userAccessResponse.DetailText = "Unauthorized: The request must contain user access token header 'x-ms-user-token'";
                }
            }
            catch (Exception ex)
            {
                // Handle this exception to return a graceful response, handle scenarios of bad request.
                responseStatus = 424;
                userAccessResponse.Status = UserAccessStatus.RequestFailure;
                userAccessResponse.DetailText = "Dependency failure: an error occurred in trying to validate user access on this resource.";
                // TODO: Log this exception
            }

            if (userAccessResponse.Status == UserAccessStatus.HasAccess)
            {
                httpContext.Items.Add("HAS_CUSTOMER_CONSENT", userAccessResponse.HasCustomerConsent);
                return new UserAuthResponse()
                {
                    IsSuccess = true
                };
            }

            else
            {
                if (string.IsNullOrEmpty(userAccessResponse.DetailText) || userAccessResponse.Status == UserAccessStatus.RequestFailure)
                {
                    userAccessResponse.DetailText = "Could not determine the authorization status of the user. Please contact AppLens team.";
                }
                return new UserAuthResponse()
                {
                    IsSuccess = false,
                    HttpStatus = responseStatus > 0 ? responseStatus : 403,
                    AccessResponse = userAccessResponse
                };
            }
        }
    }
}
