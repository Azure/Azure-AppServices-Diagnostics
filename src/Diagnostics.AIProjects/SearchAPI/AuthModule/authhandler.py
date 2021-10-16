from flask import request
from functools import wraps
from SearchModule import app
from SearchModule.Logger import loggerInstance
from AuthModule.azuread import validateToken
from AuthModule.certauth import validateCertificate

def authProvider():
    def authOuter(f):
        @wraps(f)            
        def authChecker(*args, **kwargs):
            if ("ENVIRONMENT" in app.config and app.config["ENVIRONMENT"]=="DEV"):
                return f(*args, **kwargs)
            authHeader = request.headers.get('Authorization')
            certHeader = request.headers.get('x-ms-diagcert')
            if not certHeader:
                certHeader = request.headers.get('X-ARR-ClientCert')
            res = None
            if authHeader:
                try:
                    token = authHeader.split("Bearer ")[1]
                    tokenCheck = validateToken(token)
                    res = f(*args, **kwargs) if tokenCheck[1] else (tokenCheck[0] if tokenCheck[0] else "Unauthorized Access", 401)
                except Exception as e:
                    if type(e).__name__ == "ExpiredSignatureError":
                        res = ("Token has expired", 401)
                    else:
                        loggerInstance.logHandledException("AuthorizationException", e)
                        res = ("Failed to authorize request due to system exception", 401)
            elif certHeader:
                certCheck = validateCertificate(certHeader)
                if certCheck[1]:
                    res = f(*args, **kwargs)
                else:
                    res = (certCheck[0] if certCheck[0] else "Unauthorized Access", 401)
            else:
                res = ("Request is missing Authorization header", 401)
            if res and (res[1] == 401):
                loggerInstance.logInsights(f"{res[0]}, AuthHeader: {authHeader}, CertHeader: {certHeader}, {res[1]}")
            return res
        return authChecker
    return authOuter