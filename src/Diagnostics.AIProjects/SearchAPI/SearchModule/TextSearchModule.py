import os, gc, shutil, json
from SearchModule.Logger import loggerInstance
from SearchModule.ModelInfo import ModelInfo
from SearchModule.TfIdfSearchModel import TfIdfSearchModel
from SearchModule.WmdSearchModel import WmdSearchModel
from SearchModule.Exceptions import *
from SearchModule.Utilities import absPath, copyFolder, moveModels, modelsPath
from SearchModule.MessageStrings import loadModelMessage, refreshModelMessage, fileMissingMessage
        
#### Text Search model for Queries ####
class TextSearchModel():
    def __init__(self, modelpackagepath):
        modelInfo = ModelInfo(json.loads(open(absPath(os.path.join(modelpackagepath, "ModelInfo.json"))).read()))
        loggerInstance.logInsights(f"Model type is {modelInfo.modelType}")
        if modelInfo.modelType == "TfIdfSearchModel":
            self.model = TfIdfSearchModel(modelpackagepath)
        elif modelInfo.modelType == "WmdSearchModel":
            self.model = WmdSearchModel(modelpackagepath)

    def queryDetectors(self, query=None):
        return self.model.queryDetectors(query)
    
    def queryUtterances(self, query=None, existing_utterances=[]):
        return self.model.queryUtterances(query, existing_utterances)

def loadModel(productId, model=None):
    prelogMessage = loadModelMessage.format(productId)
    if model:
        loggerInstance.logInsights(f"{prelogMessage}From provided pre-loaded model.")
        loaded_models[productId] = model
        gc.collect()
        return
    if productId in loaded_models:
        loggerInstance.logInsights(f"{prelogMessage}Model is already loaded in app")
        return
    modelpackagepath = os.path.join("SearchModule", productId)
    loggerInstance.logInsights(f"{prelogMessage}Loading from folder {modelpackagepath}")
    if not os.path.isdir(absPath(modelpackagepath)):
        loggerInstance.logInsights(f"{prelogMessage}Could not find model folder. Getting model from download location.")
        moveModels(productId, "SearchModule")
    loaded_models[productId] = TextSearchModel(modelpackagepath)

def refreshModel(productId):
    prelogMessage = refreshModelMessage.format(productId)
    modelpackagepath = os.path.join(modelsPath, productId)
    try:
        # Try to load the given model
        temp = TextSearchModel(modelpackagepath)
        temp = None
        gc.collect()
        if os.path.isdir(os.path.join("SearchModule", productId)):
            shutil.rmtree(os.path.join("SearchModule", productId))
        copyFolder(modelpackagepath, os.path.join("SearchModule", productId))
        #shutil.rmtree(absPath(path))
        loggerInstance.logInsights(f"{prelogMessage}Verified the new model by loading it. Triggering the switch.")
        loadModel(productId, model=TextSearchModel(os.path.join("SearchModule", productId)))
        loggerInstance.logInsights(f"{prelogMessage}Successfully refreshed model.")
        return "Model Refreshed Successfully"
    except Exception as e:
        #shutil.rmtree(absPath(path))
        loggerInstance.logHandledException("modelRefreshTask", ModelRefreshException(f"{prelogMessage}{str(e)}"))
        return "Failed to refresh Model Exception:" + str(e)

def freeModel(productId):
    loaded_models[productId] = None
    loaded_models.pop(productId, None)
    gc.collect()


loaded_models = {}