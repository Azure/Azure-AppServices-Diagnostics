import os, gc, shutil, uuid
from SearchModule.Logger import loggerInstance
from datetime import datetime, timezone
import json
from SearchModule.ModelInfo import ModelInfo
from SearchModule.TokenizerModule import *

from gensim.models import TfidfModel
from gensim import corpora, similarities

SITE_ROOT = os.getcwd()
loggerInstance.logInsights("SITE_ROOT: {0}".format(SITE_ROOT))

def absPath(path):
    return os.path.join(SITE_ROOT, path)

class ModelDownloadFailed(Exception):
    pass
class ModelFileConfigFailed(Exception):
    pass
class ModelFileLoadFailed(Exception):
    pass
class ResourceConfigDownloadFailed(Exception):
    pass
class ModelRefreshException(Exception):
    pass
class CopySourceFolderNotFoundException(Exception):
    pass
class CopyTaskException(Exception):
    pass

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

modelsPath = "models"
def copyFolder(src, dst):
    if os.path.isdir(absPath(src)):
        try:
            if os.path.isdir(absPath(dst)):
                try:
                    shutil.rmtree(absPath(dst))
                except Exception as e:
                    raise Exception("folderCopyTask: Delete existing folder Exception: {0}".format(str(e)))
            try:
                shutil.copytree(absPath(src), absPath(dst))
            except Exception as e:
                raise Exception("folderCopyTask: Copying to folder Exception: {0}".format(str(e)))
        except Exception as e:
            exception = CopyTaskException("TextSearchModule: {0}, src:{1}, dst:{2}".format(str(e), absPath(src), absPath(dst)))
            loggerInstance.logHandledException("folderCopyTask", exception)
            raise exception
    else:
        exception = CopySourceFolderNotFoundException("Source folder not found Copying Folder from {0} to {1}".format(absPath(src), absPath(dst)))
        loggerInstance.logHandledException("folderCopyTask", exception)
        raise exception

def downloadModels(productid, path=""):
    try:
        copyFolder(os.path.join(modelsPath, productid), absPath(os.path.join(path, productid)))
        loggerInstance.logInsights("TextSearchModule: Copied Folder for product {0} to {1}".format(productid, absPath(os.path.join(path, productid))))
    except Exception as e:
        raise ModelDownloadFailed("Model can't be downloaded " + str(e))
def downloadResourceConfig():
    try:
        copyFolder(os.path.join(modelsPath, "resourceConfig"), absPath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "resourceConfig")))
    except Exception as e:
        raise ResourceConfigDownloadFailed("Resource config can't be downloaded " + str(e))

config = json.loads(open(absPath(os.path.join("SearchModule", "resourceConfig.json")), "r").read())
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
def verifyFile(filename, prelogMessage=""):
    try:
        with open(absPath(filename), "rb") as fp:
            fp.close()
        loggerInstance.logInsights("TextSearchModule: {0}Verified File {1}".format(prelogMessage, absPath(filename)))
        return True
    except FileNotFoundError:
        loggerInstance.logInsights("TextSearchModule: {0}Failed to Verify File {1}".format(prelogMessage, absPath(filename)))
        return False

class TextSearchModel:
    def __init__(self, modelpackagepath, packageFiles):
        for key in packageFiles.keys():
            packageFiles[key] = absPath(os.path.join(modelpackagepath, packageFiles[key]))
        self.packageFiles = packageFiles
        self.models = {"dictionary": None, "m1Model": None, "m1Index": None, "m2Model": None, "m2Index": None, "detectors": None, "sampleUtterances": None, "mappings": None, "modelInfo": None}
        try:
            with open(self.packageFiles["modelInfo"], "r") as fp:
                self.models["modelInfo"] = ModelInfo(json.loads(fp.read()))
                fp.close()
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
                return {"query": query, "results": [], "exception": str(e)}
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
        loggerInstance.logInsights("TextSearchModule: Loading model for product {0}: From provided pre-loaded model.".format(productid))
        loaded_models[productid] = model
        gc.collect()
        return
    if productid in loaded_models:
        loggerInstance.logInsights("TextSearchModule: Loading model for product {0}: Model is already loaded in app".format(productid))
        return
    modelpackagepath = os.path.join("SearchModule", productid)
    loggerInstance.logInsights("TextSearchModule: Loading model for product {0}: Loading from folder {1}".format(productid, modelpackagepath))
    if not os.path.isdir(absPath(modelpackagepath)) or not all([verifyFile(os.path.join(modelpackagepath, x)) for x in packageFileNames.values()]):
        loggerInstance.logInsights("TextSearchModule: Loading model for product {0}: Could not find model folder. Getting model from download location.".format(productid))
        downloadModels(productid, "SearchModule")
    if not all([verifyFile(os.path.join(modelpackagepath, packageFileNames[x])) for x in packageFileNames.keys() if x not in optionalFiles]):
        loggerInstance.logHandledException("modelLoadTask", ModelFileConfigFailed("TextSearchModule: Loading model for product {0}: {1}".format(productid, "One or more of model file(s) are missing")))
        raise FileNotFoundError("One or more of model file(s) are missing")
    loaded_models[productid] = TextSearchModel(modelpackagepath, dict(packageFileNames))

def refreshModel(productid):
    path = str(uuid.uuid4())
    loggerInstance.logInsights("TextSearchModule: Refresh model request for product {0}: Copying models from download folder".format(productid))
    downloadModels(productid, path=path)
    modelpackagepath = os.path.join(path, productid)
    if not all([verifyFile(os.path.join(modelpackagepath, packageFileNames[x]), "Refresh model request for product {0}: ".format(productid)) for x in packageFileNames.keys() if x not in optionalFiles]):
        loggerInstance.logHandledException("modelRefreshTask", ModelRefreshException("TextSearchModule: Refresh model request for product {0}: Failed to refresh model because One or more of model file(s) are missing".format(productid)))
        return "Failed to refresh model because One or more of model file(s) are missing"
    loggerInstance.logInsights("TextSearchModule: Refresh model request for product {0}: Successfully verified all files exist for the model.".format(productid))
    try:
        temp = TextSearchModel(modelpackagepath, dict(packageFileNames))
        temp = None
        gc.collect()
        if os.path.isdir(os.path.join("SearchModule", productid)):
            shutil.rmtree(os.path.join("SearchModule", productid))
        copyFolder(modelpackagepath, os.path.join("SearchModule", productid))
        shutil.rmtree(absPath(path))
        loggerInstance.logInsights("TextSearchModule: Refresh model request for product {0}: Verified the new model by loading it. Triggering the switch.".format(productid))
    except Exception as e:
        shutil.rmtree(absPath(path))
        loggerInstance.logHandledException("modelRefreshTask", ModelRefreshException("TextSearchModule: Refresh model request for product {0}: {1}".format(productid, str(e))))
        return "Failed to refresh Model Exception:" + str(e)
    loadModel(productid, model=TextSearchModel(os.path.join("SearchModule", productid), dict(packageFileNames)))
    loggerInstance.logInsights("TextSearchModule: Refresh model request for product {0}: Successfully refreshed model.".format(productid))
    return "Model Refreshed Successfully"

def freeModel(productid):
    loaded_models[productid] = None
    loaded_models.pop(productid, None)
    gc.collect()


loaded_models = {}