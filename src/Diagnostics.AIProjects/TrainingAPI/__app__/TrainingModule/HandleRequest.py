import os, gc, shutil, uuid
from datetime import datetime, timezone
import json, logging, asyncio
from functools import wraps
from __app__.AppSettings.AppSettings import appSettings
from __app__.TrainingModule.ResourceFilterHelper import getProductId, findProductId
from __app__.TrainingModule.ModelTrainer import trainModel, publishModels
from __app__.TrainingModule.TrainingConfig import TrainingConfig

class ResourceConfigDownloadFailed(Exception):
    pass

def getUTCTime():
    return datetime.now(timezone.utc)

def getLatency(startTime, endTime):
    return (endTime-startTime).total_seconds()*1000

packageFileNames = {
    "dictionaryFile": "dictionary.dict",
    "m1ModelFile": "m1.model",
    "m1IndexFile": "m1.index",
    "m2ModelFile": "m2.model",
    "m2IndexFile": "m2.index",
    "detectorsFile": "Detectors.json",
    "sampleUtterancesFile": "SampleUtterances.json"
}

async def triggerTrainingMethod(data):
    if not "productId" in data:
        return ("Please provide a productId for training", 400)
    productId = data["productId"]
    exists = findProductId(productId)
    if not exists:
        return ('Data not available for productId {0}'.format(productId), 404)
    if not 'trainingConfig' in data:
        return ("No config provided for training", 400)
    trainingConfig = TrainingConfig(json.loads(data["trainingConfig"]))
    if (trainingConfig.trainDetectors or trainingConfig.trainUtterances):
        trainingId = str(uuid.uuid4())
        try:
            await trainModel(trainingId, productId, trainingConfig)
            return ("Model Trained successfully for productId {0} - trainingId {1}".format(productId, trainingId), 200)
        except Exception as e:
            logging.error("Exception: {0}".format(str(e)))
            return (str(e), 500)
    else:
        return ("Training flags are all set to false. Training not required", 400)