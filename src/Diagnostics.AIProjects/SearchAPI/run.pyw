"""
This script runs the application using a development server.
It contains the definition of routes and views for the application.
"""
import os, gc, shutil, uuid
from flask import Flask, request
from RegistryReader import githubFolderPath
from Logger import *
from datetime import datetime, timezone
import json, itertools, nltk
from functools import wraps
downloadResourceConfig()
config = json.loads(open("resourceConfig/config.json", "r").read())

try:
    nltk.data.find('tokenizers/punkt')
except LookupError:
    nltk.download('punkt')
try:
    nltk.data.find('stopwords')
except LookupError:
    nltk.download('stopwords')
from nltk.corpus import stopwords
from gensim.models import TfidfModel
from nltk.stem.porter import *
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
    "sampleUtterancesFile": "SampleUtterances.json"
}

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
        
modelsPath = githubFolderPath
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
#### Text Processing setup and Text Search model for Queries ####
stemmer = PorterStemmer()
stop = stopwords.words('english')

def tokenize_text(txt):
    return [stemmer.stem(word) for word in nltk.word_tokenize(txt.lower()) if word not in stop]

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
        self.models = {"dictionary": None, "m1Model": None, "m1Index": None, "m2Model": None, "m2Index": None, "detectors": None, "sampleUtterances": None}
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
        try:
            with open(self.packageFiles["sampleUtterancesFile"], "r") as f:
                self.models["sampleUtterances"] = json.loads(f.read())
                f.close()
                self.models["sampleUtterances"] = None
                gc.collect()
        except:
            raise ModelFileLoadFailed("Failed to parse json from file " + self.packageFiles["sampleUtterancesFile"])

    def queryDetectors(self, query=None):
        if query:
            try:
                vector = self.models["m1Model"][self.models["dictionary"].doc2bow(tokenize_text(query))]
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
                vector = self.models["m2Model"][self.models["dictionary"].doc2bow(tokenize_text(query))]
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
    if not all([verifyFile(os.path.join(modelpackagepath, x)) for x in packageFileNames.values()]):
        raise FileNotFoundError("One or more of model file(s) are missing")
    loaded_models[productid] = TextSearchModel(modelpackagepath, dict(packageFileNames))

def refreshModel(productid):
    path = str(uuid.uuid4())
    downloadModels(productid, path=path)
    modelpackagepath = os.path.join(path, productid)
    if not all([verifyFile(os.path.join(modelpackagepath, x)) for x in packageFileNames.values()]):
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
        logHandledException(requestId, e)
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
            logHandledException(requestId, e)
            res = {"query": txt_data, "results": None}
        if res:
            results["results"] += res["results"] if res["results"] else []
    res = json.dumps(results)

    return (res, 200)

@app.route('/freeModel')
def freeModelMethod():
    productid = str(request.args.get('productid'))
    freeModel(productid)
    return ('', 204)

@app.route('/refreshModel')
@loggingProvider(requestIdRequired=False)
def refreshModelMethod():
    productid = str(request.args.get('productid'))
    res = "{0} - {1}".format(productid, refreshModel(productid))
    return (res, 200)

if __name__ == '__main__':
    HOST = os.environ.get('SERVER_HOST', '0.0.0.0')
    PORT = 8010
    app.run(HOST, PORT)
