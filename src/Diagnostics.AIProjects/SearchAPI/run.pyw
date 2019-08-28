import os, gc, shutil, uuid
from argparse import ArgumentParser
from flask import Flask, request
from RegistryReader import detectorsFolderPath
from Logger import loggerInstance
from datetime import datetime, timezone
import json
from functools import wraps
from ModelInfo import ModelInfo
from TokenizerModule import *

argparser = ArgumentParser()
argparser.add_argument("-d", "--debug", default=False, help="flag for debug mode")
args = vars(argparser.parse_args())
loggerInstance.isLogToKustoEnabled = (not args['debug'])

from gensim.models import TfidfModel
from gensim import corpora, similarities

class ModelDownloadFailed(Exception):
    pass
class ModelFileConfigFailed(Exception):
    pass
class ModelFileLoadFailed(Exception):
    pass
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
    "sampleUtterancesFile": "SampleUtterances.json",
    "mappingsFile": "Mappings.json",
    "modelInfo": "ModelInfo.json"
}

optionalFiles = ["mappingsFile", "modelInfo"]

modelsPath = detectorsFolderPath
def copyFolder(src, dst):
    if os.path.isdir(src):
        if os.path.isdir(dst):
            shutil.rmtree(dst)
        shutil.copytree(src, dst)
def downloadModels(productid, path=""):
    try:
        copyFolder(os.path.join(modelsPath, productid), os.path.join(path, productid))
    except Exception as e:
        raise ModelDownloadFailed("Model can't be downloaded " + str(e))
def downloadResourceConfig():
    try:
        copyFolder(os.path.join(modelsPath, "resourceConfig"), os.path.join(os.path.dirname(os.path.abspath(__file__)), "resourceConfig"))
    except Exception as e:
        raise ResourceConfigDownloadFailed("Resource config can't be downloaded " + str(e))

downloadResourceConfig()
config = json.loads(open("resourceConfig/config.json", "r").read())
resourceConfig = config["resourceConfig"]
def getProductId(resourceObj):
    productids = []
    if resourceObj["ResourceType"] == "App":
        apptypes = resourceObj["AppType"].split(",")
        for app in apptypes:
            if app == "WebApp":
                platformtypes = resourceObj["PlatformType"].split(",")
                for platform in platformtypes:
                    try:
                        productids.append(resourceConfig[resourceObj["ResourceType"]][app][platform])
                    except KeyError:
                        pass
    if productids:
        return list(set(productids))
    else:
        return None
        
#### Text Search model for Queries ####
def verifyFile(filename):
    try:
        fp = open(filename, "rb")
        fp.close()
        return True
    except FileNotFoundError:
        return False

class TextSearchModel:
    def __init__(self, modelpackagepath, packageFiles):
        for key in packageFiles.keys():
            packageFiles[key] = os.path.join(modelpackagepath, packageFiles[key])
        self.packageFiles = packageFiles
        self.models = {"dictionary": None, "m1Model": None, "m1Index": None, "m2Model": None, "m2Index": None, "detectors": None, "sampleUtterances": None, "mappings": None, "modelInfo": None}
        try:
            self.models["modelInfo"] = ModelInfo(json.loads(open(self.packageFiles["modelInfo"], "r").read()))
        except:
            self.models["modelInfo"] = ModelInfo({})
        try:
            self.models["dictionary"] = corpora.Dictionary.load(self.packageFiles["dictionaryFile"])
        except:
            raise ModelFileLoadFailed("Failed to load dictionary from file " + self.packageFiles["dictionaryFile"])
        try:
            self.models["m1Model"] = TfidfModel.load(self.packageFiles["m1ModelFile"])
        except:
            raise ModelFileLoadFailed("Failed to load model from file " + self.packageFiles["m1ModelFile"])
        try:
            self.models["m1Index"] = similarities.MatrixSimilarity.load(self.packageFiles["m1IndexFile"])
        except:
            raise ModelFileLoadFailed("Failed to load index from file " + self.packageFiles["m1IndexFile"])
        try:
            self.models["m2Model"] = TfidfModel.load(self.packageFiles["m2ModelFile"])
            self.models["m2Model"] = None
            gc.collect()
        except:
            raise ModelFileLoadFailed("Failed to load model from file " + self.packageFiles["m2ModelFile"])
        try:
            self.models["m2Index"] = similarities.MatrixSimilarity.load(self.packageFiles["m2IndexFile"])
            self.models["m2Index"] = None
            gc.collect()
        except:
            raise ModelFileLoadFailed("Failed to load index from file " + self.packageFiles["m2IndexFile"])
        try:
            with open(self.packageFiles["detectorsFile"], "r") as f:
                self.models["detectors"] = json.loads(f.read())
                f.close()
        except:
            raise ModelFileLoadFailed("Failed to parse json from file " + self.packageFiles["detectorsFile"])
        if self.models["modelInfo"].detectorContentSplitted:
            try:
                with open(self.packageFiles["mappingsFile"], "r") as f:
                    self.models["mappings"] = json.loads(f.read())
                    f.close()
            except:
                raise ModelFileLoadFailed("Failed to parse json from file " + self.packageFiles["mappingsFile"])
        try:
            with open(self.packageFiles["sampleUtterancesFile"], "r") as f:
                self.models["sampleUtterances"] = json.loads(f.read())
                f.close()
                self.models["sampleUtterances"] = None
                gc.collect()
        except:
            raise ModelFileLoadFailed("Failed to parse json from file " + self.packageFiles["sampleUtterancesFile"])
    def getDetectorByIndex(self, index):
        detector = [x for x in self.models["mappings"] if (x["startindex"] <= index <= x["endindex"])]
        if detector and detector[0]:
            return detector[0]["id"]
        else:
            return None

    def queryDetectors(self, query=None):
        if query:
            try:
                vector = self.models["m1Model"][self.models["dictionary"].doc2bow(getAllNGrams(query, self.models["modelInfo"].textNGrams))]
                if self.models["modelInfo"].detectorContentSplitted:    
                    similar_doc_indices = sorted(enumerate(self.models["m1Index"][vector]), key=lambda item: -item[1])[:10]
                    similar_docs = []
                    for x in similar_doc_indices:
                        detector = self.getDetectorByIndex(x[0])
                        if detector and (not (detector in [p["detector"] for p in similar_docs])):
                            similar_docs.append({"detector": self.getDetectorByIndex(x[0]), "score": str(x[1])})
                else:
                    similar_doc_indices = sorted(enumerate(self.models["m1Index"][vector]), key=lambda item: -item[1])
                    similar_docs = list(map(lambda x: {"detector": self.models["detectors"][x[0]]["id"], "score": str(x[1])}, similar_doc_indices))
                return {"query": query, "results": similar_docs}
            except Exception as e:
                return {"query": query, "results": []}
        return None

    def loadUtteranceModel(self):
        self.models["m2Model"] = TfidfModel.load(self.packageFiles["m2ModelFile"])
        self.models["m2Index"] = similarities.MatrixSimilarity.load(self.packageFiles["m2IndexFile"])
        with open(self.packageFiles["sampleUtterancesFile"], "r") as f:
            self.models["sampleUtterances"] = json.loads(f.read())
            f.close()

    def unloadUtteranceModel(self):
        self.models["m2Model"] = None
        self.models["m2Index"] = None
        self.models["sampleUtterances"] = None
        gc.collect()

    def queryUtterances(self, query=None, existing_utterances=[]):
        if query:
            query = query + " ".join(existing_utterances)
            self.loadUtteranceModel()
            try:
                vector = self.models["m2Model"][self.models["dictionary"].doc2bow(getAllNGrams(query, self.models["modelInfo"].textNGrams))]
                similar_doc_indices = sorted(enumerate(self.models["m2Index"][vector]), key=lambda item: -item[1])
                similar_doc_indices = [x for x in similar_doc_indices if self.models["sampleUtterances"][x[0]]["text"].lower() not in existing_utterances][:10]
                similar_docs = list(map(lambda x: {"sampleUtterance": self.models["sampleUtterances"][x[0]], "score": str(x[1])}, similar_doc_indices))
                return {"query": query, "results": similar_docs}
            except Exception as e:
                return {"query": query, "results": None}
            self.unloadUtteranceModel()
        return None

def loadModel(productid, model=None):
    if model:
        loaded_models[productid] = model
        gc.collect()
        return
    if productid in loaded_models:
        return
    modelpackagepath = productid
    if not os.path.isdir(productid) or not all([verifyFile(os.path.join(modelpackagepath, x)) for x in packageFileNames.values()]):
        downloadModels(productid)
    if not all([verifyFile(os.path.join(modelpackagepath, packageFileNames[x])) for x in packageFileNames.keys() if x not in optionalFiles]):
        raise FileNotFoundError("One or more of model file(s) are missing")
    loaded_models[productid] = TextSearchModel(modelpackagepath, dict(packageFileNames))

def refreshModel(productid):
    path = str(uuid.uuid4())
    downloadModels(productid, path=path)
    modelpackagepath = os.path.join(path, productid)
    if not all([verifyFile(os.path.join(modelpackagepath, packageFileNames[x])) for x in packageFileNames.keys() if x not in optionalFiles]):
        return "Failed to refresh model because One or more of model file(s) are missing"
    try:
        temp = TextSearchModel(modelpackagepath, dict(packageFileNames))
        temp = None
        gc.collect()
        if os.path.isdir(productid):
            shutil.rmtree(productid)
        copyFolder(modelpackagepath, productid)
        shutil.rmtree(path)
    except Exception as e:
        shutil.rmtree(path)
        return "Failed to refresh Model Exception:" + str(e)
    loadModel(productid, model=TextSearchModel(productid, dict(packageFileNames)))
    return "Model Refreshed Successfully"

def freeModel(productid):
    loaded_models[productid] = None
    loaded_models.pop(productid, None)
    gc.collect()


loaded_models = {}
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
            try:
                downloadResourceConfig()
            except:
                pass
            startTime = getUTCTime()
            res = None
            requestId = getRequestId(request)
            requestId = requestId if requestId else (str(uuid.uuid4()) if not requestIdRequired else None)
            if not requestId:
                res = ("BadRequest: Missing parameter requestId", 400)
                endTime = getUTCTime()
                loggerInstance.logApiSummary("Null", str(request.url_rule), res[1], getLatency(startTime, endTime), startTime.strftime("%H:%M:%S.%f"), endTime.strftime("%H:%M:%S.%f"), res[0])
                return res
            else:
                try:
                    res = f(*args, **kwargs)
                except Exception as e:
                    res = (str(e), 500)
                    loggerInstance.logUnhandledException(requestId, str(e))
            endTime = getUTCTime()
            loggerInstance.logApiSummary(requestId, str(request.url_rule), res[1], getLatency(startTime, endTime), startTime.strftime("%H:%M:%S.%f"), endTime.strftime("%H:%M:%S.%f"), res[0])
            return res
        return logger
    return loggingOuter

@app.route('/healthping')
def healthPing():
    return ("I am alive!", 200)

@app.route('/queryDetectors', methods=["POST"])
@loggingProvider(requestIdRequired=True)
def queryDetectorsMethod():
    data = json.loads(request.data.decode('ascii'))
    requestId = data['requestId']

    txt_data = data['text']
    if not txt_data:
        return ("No text provided for search", 400)
    productid = getProductId(data)
    if not productid:
        return ('Resource data not available', 404)
    productid = productid[0]
    try:
        loadModel(productid)
    except Exception as e:
        loggerInstance.logHandledException(requestId, e)
        loggerInstance.logToFile(requestId, e)
        return (json.dumps({"query": txt_data, "results": []}), 200)
    
    res = json.dumps(loaded_models[productid].queryDetectors(txt_data))
    return (res, 200)

@app.route('/queryUtterances', methods=["POST"])
@loggingProvider(requestIdRequired=True)
def queryUtterancesMethod():
    data = json.loads(request.data.decode('ascii'))
    requestId = data['requestId']
    
    txt_data = data['detector_description']
    existing_utterances = [str(x).lower() for x in json.loads(data['detector_utterances'])]
    if not txt_data:
        return ("No text provided for search", 400)
    productid = getProductId(data)
    if not productid:
        return ('Resource type product data not available', 404)
    results = {"query": txt_data, "results": []}
    for product in productid:
        try:
            loadModel(product)
            res = loaded_models[product].queryUtterances(txt_data, existing_utterances)
        except Exception as e:
            loggerInstance.logHandledException(requestId, e)
            res = {"query": txt_data, "results": None}
        if res:
            results["results"] += res["results"] if res["results"] else []
    res = json.dumps(results)

    return (res, 200)

@app.route('/freeModel')
def freeModelMethod():
    productid = str(request.args.get('productId'))
    freeModel(productid)
    return ('', 204)

@app.route('/refreshModel', methods=["GET"])
@loggingProvider(requestIdRequired=True)
def refreshModelMethod():
    productid = str(request.args.get('productId')).strip()
    res = "{0} - {1}".format(productid, refreshModel(productid))
    return (res, 200)

if __name__ == '__main__':
    HOST = os.environ.get('SERVER_HOST', '0.0.0.0')
    PORT = 8010
    app.run(HOST, PORT, debug=args['debug'])
