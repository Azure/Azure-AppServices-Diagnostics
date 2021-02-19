import jwt, requests, os
from flask import request
from cryptography.x509 import load_pem_x509_certificate
from cryptography.hazmat.backends import default_backend
from functools import wraps
from SearchModule import app
from SearchModule.Logger import loggerInstance

# Get jwk openid-configuration to find where the jwk keys are located
res = requests.get('https://login.microsoftonline.com/common/.well-known/openid-configuration')
tid = "72f988bf-86f1-41af-91ab-2d7cd011db47"
iss = "https://sts.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47/"
jwk_uri = res.json()['jwks_uri']
res = requests.get(jwk_uri)
jwk_keys = res.json()

def authProvider():
    def authOuter(f):
        @wraps(f)            
        def authChecker(*args, **kwargs):
            if ("ENVIRONMENT" in app.config and app.config["ENVIRONMENT"]=="DEV"):
                return f(*args, **kwargs)
            authHeader = request.headers.get('Authorization')
            res = None
            if authHeader:
                try:
                    token = authHeader.split("Bearer ")[1]
                    res = f(*args, **kwargs) if validateToken(token) else ("Unauthorized Access", 401)
                except Exception as e:
                    if type(e).__name__ == "ExpiredSignatureError":
                        res = ("Token has expired", 401)
                    else:
                        res = (str(e), 401)
            else:
                res = ("Request is missing Authorization header", 401)
            if res and (res[1] == 401):
                loggerInstance.logInsights(f"{res[0]}, AuthHeader: {authHeader}, {res[1]}")
            return res
        return authChecker
    return authOuter


def validateToken(token):
    try:
        appid = app.config["APP_ID"]
    except Exception as e:
        raise Exception("Error reading APP_ID from environment variables " + str(e))
    try:
        whitelistedapps = [x.strip() for x in app.config["WHITELISTED_APPS"].split(",")]
    except Exception as e:
        raise Exception("Error reading WHITELISTED_APPS from environment variables " + str(e))
    token_header = jwt.get_unverified_header(token)
    x5c = None
    for key in jwk_keys['keys']:
        if key['kid'] == token_header['kid']:
            x5c = key['x5c']
    if not x5c:
        return False
    cert = ''.join(['-----BEGIN CERTIFICATE-----\n', x5c[0], '\n-----END CERTIFICATE-----\n',])
    public_key = load_pem_x509_certificate(cert.encode(), default_backend()).public_key()
    decoded_token = jwt.decode(token, public_key, algorithms='RS256', audience=appid,)
    if (decoded_token.get('appid', None) in whitelistedapps) and (decoded_token.get('iss', None) == iss) and (decoded_token.get('tid', None) == tid):
        return True
    return False