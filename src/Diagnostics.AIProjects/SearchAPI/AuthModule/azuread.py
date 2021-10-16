import jwt, requests
from cryptography.x509 import load_pem_x509_certificate
from cryptography.hazmat.backends import default_backend
from SearchModule import app

# Get jwk openid-configuration to find where the jwk keys are located
openid_res = requests.get('https://login.microsoftonline.com/common/.well-known/openid-configuration')
jwk_uri = openid_res.json()['jwks_uri']
jwk_res = requests.get(jwk_uri)
jwk_keys = jwk_res.json()

def validateToken(token):
    if not token:
        return ("Token is empty", False)
    try:
        tid = app.config["TENANT_ID"]
        if not tid:
            raise Exception("TENANT_ID is empty")
    except Exception as e:
        raise Exception("Error reading TENANT_ID from environment variables " + str(e))
    try:
        iss = app.config["TOKEN_ISSUER"]
        if not iss:
            raise Exception("TOKEN_ISSUER is empty")
    except Exception as e:
        raise Exception("Error reading TOKEN_ISSUER from environment variables " + str(e))        
    try:
        appid = app.config["APP_ID"]
        if not appid:
            raise Exception("APP_ID is empty")
    except Exception as e:
        raise Exception("Error reading APP_ID from environment variables " + str(e))
    try:
        whitelistedapps = [x.strip() for x in app.config["WHITELISTED_APPS"].split(",")]
        if not whitelistedapps:
            raise Exception("WHITELISTED_APPS is empty")
    except Exception as e:
        raise Exception("Error reading WHITELISTED_APPS from environment variables " + str(e))
    token_header = jwt.get_unverified_header(token)
    x5c = None
    for key in jwk_keys['keys']:
        if key['kid'] == token_header['kid']:
            x5c = key['x5c']
    if not x5c:
        return ("MalformedToken - Failed to read token attributes", False)
    cert = ''.join(['-----BEGIN CERTIFICATE-----\n', x5c[0], '\n-----END CERTIFICATE-----\n',])
    public_key = load_pem_x509_certificate(cert.encode(), default_backend()).public_key()
    decoded_token = jwt.decode(token, public_key, algorithms='RS256', audience=appid,)
    if not (decoded_token.get('tid', None) == tid):
        return (f"Token from the tenant {decoded_token.get('tid', None)} is unauthorized to access this resource", False)
    if not (decoded_token.get('iss', None) == iss):
        return (f"Token from the issuer {decoded_token.get('iss', None)} is unauthorized to access this resource", False)
    if not (decoded_token.get('appid', None) in whitelistedapps):
        return (f"App with id {decoded_token.get('appid', None)} is unauthorized to access this resource", False)
    return (None, True)