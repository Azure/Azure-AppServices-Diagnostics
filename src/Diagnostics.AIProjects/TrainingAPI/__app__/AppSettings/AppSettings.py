import json, logging
from os import environ
class AppSettings:
    def __init__(self):
        self.debug = False
        config = environ
        try:
            appsettings = json.loads(open("AppSettings/appsettings.json", "r").read())
            self.debug = appsettings.get("debug", False)
            if self.debug:
                config = appsettings
        except Exception as e:
            logging.info("Unable to read app settings from file, will read from environment variables")
        self.STORAGE_ACCOUNT_NAME = config.get("STORAGE_ACCOUNT_NAME", None)
        self.STORAGE_ACCOUNT_KEY = config.get("STORAGE_ACCOUNT_KEY", None)
        self.STORAGE_ACCOUNT_CONTAINER_NAME = config.get("STORAGE_ACCOUNT_CONTAINER_NAME", "searchservice")
        self.DETECTORS_URL = config.get("DETECTORS_URL", None)
        self.DETECTORS_CLIENT_ID = config.get("DETECTORS_CLIENT_ID", None)
        self.DETECTORS_CLIENT_SECRET = config.get("DETECTORS_CLIENT_SECRET", None)
        self.DETECTORS_APP_RESOURCE = config.get("DETECTORS_APP_RESOURCE", None)
        self.WORD2VEC_PATH = config.get("WORD2VEC_PATH", "word2vec")
        self.WORD2VEC_MODEL_NAME = config.get("WORD2VEC_MODEL_NAME", "w2vModel.bin")
appSettings = AppSettings()