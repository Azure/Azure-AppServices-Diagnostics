import json
import adal
from __app__.AppSettings.AppSettings import appSettings

authority_url = "https://login.microsoftonline.com/microsoft.onmicrosoft.com"

def acquireAccessToken():
    if not(appSettings.DETECTORS_APP_RESOURCE and appSettings.DETECTORS_CLIENT_ID and appSettings.DETECTORS_CLIENT_SECRET):
        raise Exception("Unable to fetch credentials to acquire token for detectors api")
    try:
        context = adal.AuthenticationContext(authority_url)
        token = context.acquire_token_with_client_credentials(
            appSettings.DETECTORS_APP_RESOURCE,
            appSettings.DETECTORS_CLIENT_ID,
            appSettings.DETECTORS_CLIENT_SECRET)
        if token and "accessToken" in token:
            return token["accessToken"]
    except Exception as e:
        raise Exception("Failed to get access token from Azure AD {0}".format(str(e)))