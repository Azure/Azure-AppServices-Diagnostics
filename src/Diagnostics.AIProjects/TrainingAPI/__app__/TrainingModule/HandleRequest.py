import os, gc, shutil, uuid
from datetime import datetime, timezone
import json, logging, asyncio
from functools import wraps
from __app__.TrainingModule.ResourceFilterHelper import findProductId
from __app__.TrainingModule.ModelTrainer import ModelTrainPublish
from __app__.TrainingModule.TrainingConfig import TrainingConfig
from __app__.TrainingModule.Exceptions import *

def getUTCTime():
    return datetime.now(timezone.utc)

def getLatency(startTime, endTime):
    return (endTime-startTime).total_seconds()*1000

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
            trainingHandler = ModelTrainPublish(trainingId, productId, trainingConfig)
            await trainingHandler.trainPublish()
            return ("Model Trained successfully for productId {0} - trainingId {1}".format(productId, trainingId), 200)
        except Exception as e:
            logging.error("Exception: {0}".format(str(e)))
            return (str(e), 500)
    else:
        return ("Training flags are all set to false. Training not required", 400)