import os, shutil, json, re
from SearchModule.Logger import loggerInstance
from SearchModule.ModelInfo import ModelInfo
from SearchModule.TfIdfSearchModel import TfIdfSearchModel
from SearchModule.WmdSearchModel import WmdSearchModel
from SearchModule.Exceptions import *
from SearchModule.Utilities import absPath, copyFolder, moveModels, modelsPath
from SearchModule.MessageStrings import loadModelMessage, refreshModelMessage, fileMissingMessage

def breakQuery(query):
    queries = [y for y in list(map(lambda x: " ".join(re.sub(r'[^(0-9a-zA-Z )]+', " ", x).split()), re.split(r'[\.,]', query))) if (y and len(y)>=2)]
    return queries
def mergeResults(query, resultsList):
    finalResults = {}
    exceptions = []
    for result in resultsList:
        for x in result["results"]:
            finalResults[x["detector"]] = max([x["score"], finalResults.get(x["detector"], 0)])
            if "exception" in x:
                exceptions.append(x["exception"])
    mergedResult = {"query": query, "results": [{"detector": key, "score": value} for key, value in finalResults.items()]}
    if exceptions and len(exceptions) > 0:
        mergedResult["exception"] = " | ".join(exceptions)
    return mergedResult
#### Text Search model for Queries ####
class TextSearchModel():
    def __init__(self, modelpackagepath):
        self.trainingId = None
        try:
            self.trainingId = open(absPath(os.path.join(modelpackagepath, "trainingId.txt"))).read().strip()
        except:
            pass
        modelInfo = ModelInfo(json.loads(open(absPath(os.path.join(modelpackagepath, "ModelInfo.json"))).read()))
        loggerInstance.logInsights(f"Model type is {modelInfo.modelType}")
        if modelInfo.modelType == "TfIdfSearchModel":
            self.model = TfIdfSearchModel(modelpackagepath)
        elif modelInfo.modelType == "WmdSearchModel":
            self.model = WmdSearchModel(modelpackagepath)

    def queryDetectors(self, query=None):
        cleansed_query = " ".join(re.sub(r'[^(0-9a-zA-Z )]+', " ", query).split())
        # Break the query into smaller chunks if number of word is greater than 6
        if len(cleansed_query.split())>6:
            queries = breakQuery(query)
            queries = [cleansed_query] + queries
            return mergeResults(cleansed_query, [self.model.queryDetectors(query) for query in queries])
        else:
            return self.model.queryDetectors(cleansed_query)
    
    def queryUtterances(self, query=None, existing_utterances=[]):
        return self.model.queryUtterances(query, existing_utterances)

def loadModel(productId, model=None, forced=False):
    prelogMessage = loadModelMessage.format(productId)
    if model:
        loggerInstance.logInsights(f"{prelogMessage}From provided pre-loaded model.")
        loaded_models[productId] = model
        return
    if (productId in loaded_models) and (not forced):
        loggerInstance.logInsights(f"{prelogMessage}Model is already loaded in app")
        return
    modelpackagepath = os.path.join("SearchModule", productId)
    loggerInstance.logInsights(f"{prelogMessage}Loading from folder {modelpackagepath}")
    if not os.path.isdir(absPath(modelpackagepath)):
        loggerInstance.logInsights(f"{prelogMessage}Could not find model folder. Getting model from download location.")
        moveModels(productId, "SearchModule")
    try:
        loaded_models[productId] = TextSearchModel(modelpackagepath)
    except Exception as e:
        if type(e).__name__ == "ModelFileVerificationFailed":
            loggerInstance.logInsights(f"Loading model failed for {productId} in ModelFileVerification step. Will copy the model folder and reload.")
        moveModels(productId, "SearchModule")
        loaded_models[productId] = TextSearchModel(modelpackagepath)

def refreshModel(productId):
    prelogMessage = refreshModelMessage.format(productId)
    modelpackagepath = os.path.join(modelsPath, productId)
    try:
        # Try to load the given model
        temp = TextSearchModel(modelpackagepath)
        del temp
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
    del loaded_models[productId]


loaded_models = {}