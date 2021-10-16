import os, json
from flask import Config

class DevelopmentConfig(Config):
    devJson = json.loads(open("AppConfig/appconfig.json", "r").read())
    ENVIRONMENT = "DEV"
    MODEL_SYNC_ENABLED = devJson.get('MODEL_SYNC_ENABLED', True)
    TENANT_ID = devJson.get('TENANT_ID', None)
    TOKEN_ISSUER = devJson.get('TOKEN_ISSUER', None)
    APP_ID = devJson.get('APP_ID', None)
    WHITELISTED_APPS = devJson.get('WHITELISTED_APPS', None)
    STORAGE_ACCOUNT_NAME = devJson.get('STORAGE_ACCOUNT_NAME', None)
    STORAGE_ACCOUNT_KEY = devJson.get('STORAGE_ACCOUNT_KEY', None)
    STORAGE_ACCOUNT_CONTAINER_NAME = devJson.get('STORAGE_ACCOUNT_CONTAINER_NAME', None)
    TRAINED_MODELS_PATH = devJson.get('TRAINED_MODELS_PATH', 'models')
    LUIS_APP_ID = devJson.get('LUIS_APP_ID', None)
    LUIS_APP_KEY = devJson.get('LUIS_APP_KEY', None)
    ALLOWED_ISSUERS = devJson.get('ALLOWED_ISSUERS', None)
    ALLOWED_SUBJECTNAMES = devJson.get('ALLOWED_SUBJECTNAMES', None)

class ProductionConfig(Config):
    ENVIRONMENT = "PRODUCTION"
    MODEL_SYNC_ENABLED = True
    TENANT_ID = os.getenv('TENANT_ID', None)
    TOKEN_ISSUER = os.getenv('TOKEN_ISSUER', None)
    APP_ID = os.getenv('APP_ID', None)
    WHITELISTED_APPS = os.getenv('WHITELISTED_APPS', None)
    STORAGE_ACCOUNT_NAME = os.getenv('STORAGE_ACCOUNT_NAME', None)
    STORAGE_ACCOUNT_KEY = os.getenv('STORAGE_ACCOUNT_KEY', None)
    STORAGE_ACCOUNT_CONTAINER_NAME = os.getenv('STORAGE_ACCOUNT_CONTAINER_NAME', None)
    TRAINED_MODELS_PATH = os.getenv('TRAINED_MODELS_PATH', 'models')
    LUIS_APP_ID = os.getenv('LUIS_APP_ID', None)
    LUIS_APP_KEY = os.getenv('LUIS_APP_KEY', None)
    ALLOWED_ISSUERS = os.getenv('ALLOWED_ISSUERS', None)
    ALLOWED_SUBJECTNAMES = os.getenv('ALLOWED_SUBJECTNAMES', None)