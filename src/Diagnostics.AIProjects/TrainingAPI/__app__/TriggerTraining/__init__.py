import logging, asyncio
import azure.functions as func
from __app__.AppSettings.AppSettings import appSettings
from __app__.TrainingModule.HandleRequest import triggerTrainingMethod
from __app__.TrainingModule.StorageAccountHelper import StorageAccountHelper

async def main(req: func.HttpRequest) -> func.HttpResponse:
    logging.info('Python HTTP trigger function processed a request.')
    req_data = {}
    try:
        req_data = req.get_json()
    except ValueError:
        pass
    if req_data:
        productId = req_data["productId"]
        sah = StorageAccountHelper()
        await sah.downloadFile("resourceConfig/config.json")
        await sah.downloadFile("{0}/testCases.json".format(productId), "TestingModule")
        await sah.downloadFile("{0}/rawdata/SampleUtterances.json".format(productId), "rawdata_{0}".format(productId))
        try:
            res, stat = await triggerTrainingMethod(req_data)
            return func.HttpResponse(f"{res}", status_code=stat)
        except Exception as e:
            return func.HttpResponse(f"{e}", status_code=500)
    else:
        return func.HttpResponse(
             "Please pass data in the request body",
             status_code=400
        )
