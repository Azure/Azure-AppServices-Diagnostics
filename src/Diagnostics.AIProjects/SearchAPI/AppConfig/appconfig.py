import os, json
from flask import Config

class DevelopmentConfig(Config):
    devJson = json.loads(open("AppConfig/appconfig.json", "r").read())
    ENVIRONMENT = "DEV"
    MODEL_SYNC_ENABLED = devJson.get('MODEL_SYNC_ENABLED', True)
    APP_ID = devJson.get('APP_ID', None)
    WHITELISTED_APPS = devJson.get('WHITELISTED_APPS', None)
    STORAGE_ACCOUNT_NAME = devJson.get('STORAGE_ACCOUNT_NAME', None)
    STORAGE_ACCOUNT_KEY = devJson.get('STORAGE_ACCOUNT_KEY', None)
    STORAGE_ACCOUNT_CONTAINER_NAME = devJson.get('STORAGE_ACCOUNT_CONTAINER_NAME', None)
    TRAINED_MODELS_PATH = devJson.get('TRAINED_MODELS_PATH', 'models')

class ProductionConfig(Config):
    ENVIRONMENT = "PRODUCTION"
    MODEL_SYNC_ENABLED = True
    APP_ID = os.getenv('APP_ID', None)
    WHITELISTED_APPS = os.getenv('WHITELISTED_APPS', None)
    STORAGE_ACCOUNT_NAME = os.getenv('STORAGE_ACCOUNT_NAME', None)
    STORAGE_ACCOUNT_KEY = os.getenv('STORAGE_ACCOUNT_KEY', None)
    STORAGE_ACCOUNT_CONTAINER_NAME = os.getenv('STORAGE_ACCOUNT_CONTAINER_NAME', None)
    TRAINED_MODELS_PATH = os.getenv('TRAINED_MODELS_PATH', 'models')