import os, json, requests
from gensim.models import TfidfModel
from gensim import corpora, similarities
from DataProcessor import DataProcessor
from Logger import loggerInstance
from RegistryReader import *
from TokenizerModule import *

class TrainingException(Exception):
    pass

class PublishingException(Exception):
    pass

def testModelForSearch(model, dictionary, index, query, ngrams=1):
    try:
        vector = model[dictionary.doc2bow(getAllNGrams(query, ngrams))]
        similar_doc_indices = sorted(enumerate(index[vector]), key=lambda item: -item[1])
        if similar_doc_indices[0][1]>0:
            return True
        return False
    except Exception as e:
        return False

def trainDictionary(alltokens, productid, outpath):
    dictionary = corpora.Dictionary(alltokens)
    dictionary.save(os.path.join(outpath, "dictionary.dict"))
    
def trainModelM1(tests, detector_tokens, sampleUtterances_tokens, productid, outpath):
    dictionary = corpora.Dictionary.load(os.path.join(outpath, "dictionary.dict"))
    corpus = [dictionary.doc2bow(line) for line in detector_tokens]
    model = TfidfModel(corpus)
    index = similarities.MatrixSimilarity(model[corpus])
    for test in tests:
        if not testModelForSearch(model, dictionary, index, test):
            return
    model.save(os.path.join(outpath, "m1.model"))
    index.save(os.path.join(outpath, "m1.index"))

def trainModelM2(tests, detector_tokens, sampleUtterances_tokens, productid, outpath):
    dictionary = corpora.Dictionary.load(os.path.join(outpath, "dictionary.dict"))
    corpus = [dictionary.doc2bow(line) for line in sampleUtterances_tokens]
    model = TfidfModel(corpus)
    index = similarities.MatrixSimilarity(model[corpus])
    for test in tests:
        if not testModelForSearch(model, dictionary, index, test):
            return
    model.save(os.path.join(outpath, "m2.model"))
    index.save(os.path.join(outpath, "m2.index"))

def publishModels(productid, modelPath, trainingId):
    config = json.loads(open("resourceConfig/config.json", "r").read())
    publishUrl = "http://localhost:{0}/internal/publishmodel?trainingId={1}".format(config["internalApiPort"], trainingId)
    req = requests.post(publishUrl, data=json.dumps(modelPath), headers={"Content-Type": "application/json"})
    if req.status_code == 200:
        pass
    else:
        raise PublishingException("ModelPublisher: " + str(req.content))

def trainModel(trainingId, productid, trainingConfig):
    loggerInstance.logToFile("{0}.log".format(trainingId), json.dumps(trainingConfig.__dict__))
    datapath = "rawdata_{0}".format(productid)
    outpath = "{0}".format(productid)
    try:
        os.mkdir(datapath)
    except FileExistsError:
        pass
    try:
        os.mkdir(outpath)
    except FileExistsError:
        pass
    loggerInstance.logToFile("{0}.log".format(trainingId), "Created folders for raw data and processed models")
    try:
        dataProcessor = DataProcessor(trainingConfig, trainingId)
        dataProcessor.prepareDataForTraining(productid)
        loggerInstance.logToFile("{0}.log".format(trainingId), "DataFetcher: Sucessfully fetched and processed for training")
    except Exception as e:
        loggerInstance.logToFile("{0}.log".format(trainingId), "[ERROR]DataFetcher: " + str(e))
        raise TrainingException("DataFetcher: " + str(e))
    try:
        detectorsdata = open(os.path.join(datapath, "Detectors.json"), "r").read()
        detectors = json.loads(detectorsdata)
        if trainingConfig.detectorContentSplitted:
            detector_mappings = []
            detector_tokens = []
            i = 0
            for x in detectors:
                detector_mappings.append({"startindex": i, "endindex": i + len(x["utterances"]) + 1, "id": x["id"]})
                detector_tokens += [getAllNGrams(x["name"], trainingConfig.textNGrams)] + [getAllNGrams(x["description"], trainingConfig.textNGrams)] +  [getAllNGrams(y["text"], trainingConfig.textNGrams) for y in x["utterances"]]
                i += (len(x["utterances"]) + 2)
            open(os.path.join(outpath, "Mappings.json"), "w").write(json.dumps(detector_mappings))
        else:
            detector_tokens = [getAllNGrams(x["name"] + " " + x["description"] + " " + " ".join([y["text"] for y in x["utterances"]]), trainingConfig.textNGrams) for x in detectors]
        loggerInstance.logToFile("{0}.log".format(trainingId), "DetectorProcessor: Sucessfully processed detectors data into tokens")
    except Exception as e:
        loggerInstance.logToFile("{0}.log".format(trainingId), "[ERROR]DetectorProcessor: " + str(e))
        raise TrainingException("DetectorProcessor: " + str(e))
    try:
        #Stackoverflow and Case Incidents data load
        sampleUtterancesContent = json.loads(open(os.path.join(datapath, "SampleUtterances.json"), "r").read())
        sampleUtterances = (sampleUtterancesContent["incidenttitles"] if trainingConfig.includeCaseTitles else []) + (sampleUtterancesContent["stackoverflowtitles"] if trainingConfig.includeStackoverflow else [])
        sampleUtterances_tokens = [getAllNGrams(sampleUtterances[i]["text"], trainingConfig.textNGrams) for i in range(len(sampleUtterances))]
        loggerInstance.logToFile("{0}.log".format(trainingId), "CaseTitlesProcessor: Sucessfully processed sample utterances into tokens")
    except Exception as e:
        loggerInstance.logToFile("{0}.log".format(trainingId), "[ERROR]CaseTitlesProcessor: " + str(e))
        raise TrainingException("CaseTitlesProcessor: " + str(e))
    try:
        trainDictionary(detector_tokens + sampleUtterances_tokens, productid, outpath)
        loggerInstance.logToFile("{0}.log".format(trainingId), "DictionaryTrainer: Sucessfully trained dictionary")
    except Exception as e:
        loggerInstance.logToFile("{0}.log".format(trainingId), "[ERROR]DictionaryTrainer: " + str(e))
        raise TrainingException("DictionaryTrainer: " + str(e))
    if trainingConfig.trainDetectors:
        try:
            trainModelM1([], detector_tokens, sampleUtterances_tokens, productid, outpath)
            loggerInstance.logToFile("{0}.log".format(trainingId), "ModelM1Trainer: Sucessfully trained model m1")
        except Exception as e:
            loggerInstance.logToFile("{0}.log".format(trainingId), "[ERROR]ModelM1Trainer: " + str(e))
            raise TrainingException("ModelM1Trainer: " + str(e))
    else:
        loggerInstance.logToFile("{0}.log".format(trainingId), "ModelM1Trainer: Training is disabled")
    if trainingConfig.trainUtterances:
        try:
            trainModelM2([], detector_tokens, sampleUtterances_tokens, productid, outpath)
            loggerInstance.logToFile("{0}.log".format(trainingId), "ModelM2Trainer: Sucessfully trained model m2")
        except Exception as e:
            loggerInstance.logToFile("{0}.log".format(trainingId), "[ERROR]ModelM2Trainer: " + str(e))
            raise TrainingException("ModelM2Trainer: " + str(e))
    open(os.path.join(outpath, "Detectors.json"), "w").write(json.dumps(detectors))
    open(os.path.join(outpath, "SampleUtterances.json"), "w").write(json.dumps(sampleUtterances))
    modelInfo = {"detectorContentSplitted": trainingConfig.detectorContentSplitted, "textNGrams": trainingConfig.textNGrams}
    open(os.path.join(outpath, "ModelInfo.json"), "w").write(json.dumps(modelInfo))
    modelPath = os.path.join(os.path.dirname(os.path.abspath(__file__)), outpath)
    try:
        publishModels(productid, modelPath, trainingId)
        loggerInstance.logToFile("{0}.log".format(trainingId), "ModelPublisher: Sucessfully published models")
    except Exception as e:
        loggerInstance.logToFile("{0}.log".format(trainingId), "[ERROR]ModelPublisher: " + str(e))
        raise TrainingException("ModelPublisher: " + str(e))