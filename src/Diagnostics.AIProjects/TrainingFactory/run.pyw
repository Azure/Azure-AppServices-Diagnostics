import os, gc, shutil, uuid
from flask import Flask, request
from Logger import *
from datetime import datetime, timezone
import json
from functools import wraps
from ResourceFilterHelper import getProductId, findProductId
from ModelTrainer import trainModel
from RegistryReader import *

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

def removeLogFile(fileName):
    try:
        os.remove(fileName)
    except OSError:
        pass

modelsPath = githubFolderPath
def copyFolder(src, dst):
    if os.path.isdir(src):
        if os.path.isdir(dst):
            shutil.rmtree(dst)
        shutil.copytree(src, dst)
def downloadResourceConfig():
    try:
        copyFolder(os.path.join(modelsPath, "resourceConfig"), os.path.join(os.path.dirname(os.path.abspath(__file__)), "resourceConfig"))
    except Exception as e:
        raise ResourceConfigDownloadFailed("Resource config can't be downloaded " + str(e))

######## RUN THE API SERVER IN FLASK  #############
app = Flask(__name__)
# Make the WSGI interface available at the top level so wfastcgi can get it.
wsgi_app = app.wsgi_app

def getRequestId(req):
    if req.method == 'POST':
        data = json.loads(request.data.decode('ascii'))
        return data['requestId'] if 'requestId' in data else None
    elif req.method == 'GET':
        return request.args.get('requestId')
    return None

def loggingProvider(requestIdRequired=True):
    def loggingOuter(f):
        @wraps(f)
        def logger(*args, **kwargs):
            downloadResourceConfig()
            startTime = getUTCTime()
            res = None
            requestId = getRequestId(request)
            requestId = requestId if requestId else (str(uuid.uuid4()) if not requestIdRequired else None)
            if not requestId:
                res = ("BadRequest: Missing parameter requestId", 400)
                endTime = getUTCTime()
                logApiSummary("Null", str(request.url_rule), res[1], getLatency(startTime, endTime), startTime.strftime("%H:%M:%S.%f"), endTime.strftime("%H:%M:%S.%f"), res[0])
                return res
            else:
                try:
                    res = f(*args, **kwargs)
                except Exception as e:
                    res = (str(e), 500)
                    logUnhandledException(requestId, str(e))
            endTime = getUTCTime()
            logApiSummary(requestId, str(request.url_rule), res[1], getLatency(startTime, endTime), startTime.strftime("%H:%M:%S.%f"), endTime.strftime("%H:%M:%S.%f"), res[0])
            return res
        return logger
    return loggingOuter

@app.route('/healthping')
def healthPing():
    return ("I am alive!", 200)

@app.route('/triggerTraining', methods=["POST"])
@loggingProvider(requestIdRequired=True)
def triggerTrainingMethod():
    data = json.loads(request.data.decode('ascii'))
    requestId = data['requestId']
    if not "productId" in data:
        return ("Please provide a productId for training", 400)
    productId = data["productId"]
    exists = findProductId(productId)
    if not exists:
        return ('Data not available for productId {0}'.format(productId), 404)
    if not 'trainingConfig' in data:
        return ("No config provided for training", 400)
    trainingConfig = json.loads(data["trainingConfig"])
    trainingId = str(uuid.uuid4())
    try:
        startTime = getUTCTime()
        trainModel(trainingId, productId, trainingConfig)
        endTime = getUTCTime()
        logTrainingSummary(requestId, trainingId, productId, getLatency(startTime, endTime), startTime.strftime("%H:%M:%S.%f"), endTime.strftime("%H:%M:%S.%f"), json.dumps({"trainingLogs":  open("{0}.log".format(trainingId), "r").read()}))
        removeLogFile("{0}.log".format(trainingId))
        return ("Model Trained successfully for productId {0} - trainingId {1}".format(productId, trainingId), 200)
    except Exception as e:
        logTrainingException(requestId, trainingId, productId, e)
        removeLogFile("{0}.log".format(trainingId))
        return (str(e), 500)

if __name__ == '__main__':
    HOST = os.environ.get('SERVER_HOST', '0.0.0.0')
    PORT = 8011
    app.run(HOST, PORT)
