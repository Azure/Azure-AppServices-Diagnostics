from cryptography import x509
from cryptography.hazmat.backends import default_backend
from cryptography.x509.oid import NameOID
import datetime
from SearchModule import app

def validateCertificate(certStr):
    if not certStr:
        return ("Empty certificate value", False)
    try:
        certStr = ''.join(['-----BEGIN CERTIFICATE-----\n', certStr, '\n-----END CERTIFICATE-----\n',])
        certBytes = certStr.encode()
        certInfo = readCertificateInfo(certBytes)
        if not certInfo:
            return ("MalformedCertificate - Unable to verify certificate", False)
        if certInfo["expiry_date"]<=datetime.datetime.now():
            return ("Certificate is expired", False)
        try:
            ALLOWED_ISSUERS, ALLOWED_SUBJECTNAMES = readAppConfig()
        except Exception as e:
            return ("Failed to validate certificate due to system error", False)
        if not certInfo["issuer"] in ALLOWED_ISSUERS:
            return ("Certificate is unauthorized to access this resource", False)
        if not certInfo["subject_name"] in ALLOWED_SUBJECTNAMES:
            return ("Certificate is unauthorized to access this resource", False)
        return (None, True)
    except Exception as e:
        return (str(e), False)

def readAppConfig():
    try:
        ALLOWED_ISSUERS = app.config["ALLOWED_ISSUERS"]
        if ALLOWED_ISSUERS and len(ALLOWED_ISSUERS)>1:
            ALLOWED_ISSUERS = ALLOWED_ISSUERS.strip().split(",")
        else:
            ALLOWED_ISSUERS = []
    except Exception as e:
        raise Exception("Error reading ALLOWED_ISSUERS from environment variables " + str(e))
    try:
        ALLOWED_SUBJECTNAMES = app.config["ALLOWED_SUBJECTNAMES"]
        if ALLOWED_SUBJECTNAMES and len(ALLOWED_SUBJECTNAMES)>1:
            ALLOWED_SUBJECTNAMES = ALLOWED_SUBJECTNAMES.strip().split(",")
        else:
            ALLOWED_SUBJECTNAMES = []
    except Exception as e:
        raise Exception("Error reading ALLOWED_SUBJECTNAMES from environment variables " + str(e))
    return (ALLOWED_ISSUERS, ALLOWED_SUBJECTNAMES)

def readCertificateInfo(certBytes):
    try:
        cert = x509.load_pem_x509_certificate(certBytes, default_backend())
        issuer_name = cert.issuer.get_attributes_for_oid(NameOID.COMMON_NAME)[0].value
        subject_name = cert.subject.get_attributes_for_oid(NameOID.COMMON_NAME)[0].value
        expiry_date = cert.not_valid_after
        return {"issuer": issuer_name, "subject_name": subject_name, "expiry_date": expiry_date}
    except Exception as e:
        return None