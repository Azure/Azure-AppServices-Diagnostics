import logging, asyncio, json, os
from pathlib import Path
import azure.functions as func
from __app__.AppSettings.AppSettings import appSettings
from __app__.TrainingModule.HandleRequest import triggerTrainingMethod
from __app__.TrainingModule.StorageAccountHelper import StorageAccountHelper
from __app__.TrainingModule.TrainingConfig import TrainingConfig

async def main(req: func.HttpRequest) -> func.HttpResponse:
    logging.info('Python HTTP trigger function processed a request.')
    req_data = {}
    try:
        req_data = req.get_json()
        logging.info("Parsed request data {0}".format(json.dumps(req_data)))
    except ValueError:
        pass
    if req_data:
        productId = req_data["productId"]
        if "trainingConfig" in req_data:
            trainingConfig = TrainingConfig(json.loads(req_data["trainingConfig"]))
            sah = StorageAccountHelper()
            if trainingConfig.modelType == "WmdSearchModel":
                if not Path(os.path.join(appSettings.WORD2VEC_PATH, appSettings.WORD2VEC_MODEL_NAME)).exists():
                    await sah.downloadFile("word2vec/w2vModel.bin", appSettings.WORD2VEC_PATH)
            await sah.downloadFile("resourceConfig/config.json")
            await sah.downloadFile("{0}/testCases.json".format(productId), "TestingModule")
            await sah.downloadFile("{0}/rawdata/SampleUtterances.json".format(productId), "rawdata_{0}".format(productId))
            try:
                res, stat = await triggerTrainingMethod(req_data)
                return func.HttpResponse(f"{res}", status_code=stat)
            except Exception as e:
                return func.HttpResponse(f"{e}", status_code=500)
    return func.HttpResponse("Please provide training configuration in the request body", status_code=400)
