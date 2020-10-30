import importlib, sys
#importlib.reload(sys.modules['azure'])
import asyncio, json, os
from __app__.TrainingModule import logHandler
import logging
from pathlib import Path
import azure.functions as func
from __app__.AppSettings.AppSettings import appSettings
from __app__.TrainingModule.HandleRequest import triggerTrainingMethod
from __app__.TrainingModule.StorageAccountHelper import StorageAccountHelper
from __app__.TrainingModule.TrainingConfig import TrainingConfig
from __app__.TrainingModule.Exceptions import *
#from azure.common import AzureMissingResourceHttpError

async def main(req: func.HttpRequest) -> func.HttpResponse:
    logHandler.info('Python HTTP trigger function processed a request.')
    req_data = {}
    try:
        req_data = req.get_json()
        logHandler.info("Parsed request data {0}".format(json.dumps(req_data)))
    except ValueError:
        pass
    if req_data:
        productId = req_data["productId"]
        if appSettings.debug:
            fh = logging.FileHandler(f'TrainingLogs_{productId}.log')
            fh.setLevel(logging.DEBUG)
            formatter = logging.Formatter('%(asctime)s - %(name)s - %(levelname)s - %(message)s')
            fh.setFormatter(formatter)
            logHandler.addHandler(fh)
        if "trainingConfig" in req_data:
            try:
                trainingConfig = TrainingConfig(json.loads(req_data["trainingConfig"]))
                sah = StorageAccountHelper()
                if trainingConfig.modelType == "WmdSearchModel":
                    if not Path(os.path.join(appSettings.WORD2VEC_PATH, appSettings.WORD2VEC_MODEL_NAME)).exists():
                        sah.downloadFile("word2vec/w2vModel.bin", appSettings.WORD2VEC_PATH)
                sah.downloadFile("resourceConfig/config.json")
                try:
                    sah.downloadFile("{0}/testCases.json".format(productId))
                except Exception as e:
                    logHandler.warning(f"Test case file not found for productId {productId}. This is unsafe and absence of test cases might cause bad models to go in production.")
                    if trainingConfig.blockOnMissingTestCases:
                        ex = TestCasesMissingException(f"Test cases file for productId {productId} not found. Will abort training because 'blockOnMissingTestCases' is set to True.")
                        raise ex
                sah.downloadFile("{0}/rawdata/SampleUtterances.json".format(productId), "rawdata_{0}".format(productId))
                try:
                    res, stat = await triggerTrainingMethod(req_data)
                    if appSettings.debug:
                        logHandler.removeHandler(fh)
                    return func.HttpResponse(f"{res}", status_code=stat)
                except Exception as e:
                    if appSettings.debug:
                        logHandler.removeHandler(fh)
                    return func.HttpResponse(f"{e}", status_code=500)
            except Exception as ex:
                if appSettings.debug:
                    logHandler.removeHandler(fh)
                raise ex
    return func.HttpResponse("Please provide training configuration in the request body", status_code=400)
