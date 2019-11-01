import os, json, time
import logging, asyncio
from gensim.models import TfidfModel
from gensim import corpora, similarities
from __app__.AppSettings.AppSettings import appSettings
from __app__.TrainingModule.TokenizerModule import getAllNGrams
from __app__.TrainingModule.StorageAccountHelper import StorageAccountHelper
from __app__.TrainingModule.DetectorsFetchHelper import getAllDetectors
from __app__.TestingModule.TextSearchModule import loadModel
from __app__.TestingModule.TestSchema import TestCase


class TrainingException(Exception):
    pass

class PublishingException(Exception):
    pass

def testModelForSearch(productid):
    model = None
    try:
        model = loadModel(productid)
    except Exception as e:
        logging.error("Failed to load the model for {0} with exception {1}".format(productid, str(e)), exc_info=True)
        return False
    testCases = []
    try:
        with open("TestingModule/testCases.json", "r") as testFile:
            content = json.loads(testFile.read())
            testCases = [TestCase(t["query"], t["expectedResults"]) for t in content]
            if not testCases:
                logging.warning("No test cases for product {0} .. skipping testing".format(productid))
                return True
    except Exception as e:
        logging.warning("Exception while reading test cases from file {0} .. skipping testing".format(str(e)))
        return True
    if model and testCases:
        return model.runTestCases(testCases)

def trainDictionary(alltokens, productid, outpath):
    dictionary = corpora.Dictionary(alltokens)
    dictionary.save(os.path.join(outpath, "dictionary.dict"))
    
def trainModelM1(detector_tokens, sampleUtterances_tokens, productid, outpath):
    dictionary = corpora.Dictionary.load(os.path.join(outpath, "dictionary.dict"))
    corpus = [dictionary.doc2bow(line) for line in detector_tokens]
    model = TfidfModel(corpus)
    index = similarities.MatrixSimilarity(model[corpus])
    model.save(os.path.join(outpath, "m1.model"))
    index.save(os.path.join(outpath, "m1.index"))

def trainModelM2(detector_tokens, sampleUtterances_tokens, productid, outpath):
    dictionary = corpora.Dictionary.load(os.path.join(outpath, "dictionary.dict"))
    corpus = [dictionary.doc2bow(line) for line in sampleUtterances_tokens]
    model = TfidfModel(corpus)
    index = similarities.MatrixSimilarity(model[corpus])
    model.save(os.path.join(outpath, "m2.model"))
    index.save(os.path.join(outpath, "m2.index"))

async def publishModels(productid, trainingId):
    ts = int(str(time.time()).split(".")[0])
    try:
        sah = StorageAccountHelper()
        for fileName in os.listdir(productid):
            logging.info("Uploading {0} to {1}".format(os.path.join(productid, fileName), os.path.join(productid, "models", str(ts), fileName)))
            await sah.uploadFile(os.path.join(productid, fileName), os.path.join(productid, "models", str(ts), fileName))
    except Exception as e:
        logging.error("Publishing Exception: {0}".format(str(e)))
        raise PublishingException(str(e))

async def trainModel(trainingId, productid, trainingConfig):
    logging.info("Starting training for {0}".format(trainingId))
    logging.info("Training config {0}".format(json.dumps(trainingConfig.__dict__)))
    datapath = "rawdata_{0}".format(productid)
    outpath = "{0}".format(productid)
    try:
        os.mkdir(outpath)
    except FileExistsError:
        pass
    logging.info("Created folder for processed models")
    try:
        detectors_ = getAllDetectors()
        logging.info("DataFetcher: Sucessfully fetched detectors for training")
        if detectors_ and len(detectors_)>0:
            open(os.path.join(datapath, "Detectors.json"), "w").write(json.dumps(detectors_))
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
        logging.info("DetectorProcessor: Sucessfully processed detectors data into tokens")
    except Exception as e:
        logging.error("[ERROR]DetectorProcessor: " + str(e))
        raise TrainingException("DetectorProcessor: " + str(e))
    try:
        #Stackoverflow and Case Incidents data load
        sampleUtterancesContent = json.loads(open(os.path.join(datapath, "SampleUtterances.json"), "r").read())
        sampleUtterances = (sampleUtterancesContent["incidenttitles"] if trainingConfig.includeCaseTitles else []) + (sampleUtterancesContent["stackoverflowtitles"] if trainingConfig.includeStackoverflow else [])
        sampleUtterances_tokens = [getAllNGrams(sampleUtterances[i]["text"], trainingConfig.textNGrams) for i in range(len(sampleUtterances))]
        logging.info("CaseTitlesProcessor: Sucessfully processed sample utterances into tokens")
    except Exception as e:
        logging.error("[ERROR]CaseTitlesProcessor: " + str(e))
        raise TrainingException("CaseTitlesProcessor: " + str(e))
    try:
        trainDictionary(detector_tokens + sampleUtterances_tokens, productid, outpath)
        logging.info("DictionaryTrainer: Sucessfully trained dictionary")
    except Exception as e:
        logging.error("[ERROR]DictionaryTrainer: " + str(e))
        raise TrainingException("DictionaryTrainer: " + str(e))
    if trainingConfig.trainDetectors:
        try:
            trainModelM1(detector_tokens, sampleUtterances_tokens, productid, outpath)
            logging.info("ModelM1Trainer: Sucessfully trained model m1")
        except Exception as e:
            logging.error("[ERROR]ModelM1Trainer: " + str(e))
            raise TrainingException("ModelM1Trainer: " + str(e))
    else:
        pass
        logging.info("ModelM1Trainer: Training is disabled")
    if trainingConfig.trainUtterances:
        try:
            trainModelM2(detector_tokens, sampleUtterances_tokens, productid, outpath)
            logging.info("ModelM2Trainer: Sucessfully trained model m2")
        except Exception as e:
            logging.error("[ERROR]ModelM2Trainer: " + str(e))
            raise TrainingException("ModelM2Trainer: " + str(e))
    open(os.path.join(outpath, "trainingId.txt"), "w").write(str(trainingId))
    open(os.path.join(outpath, "Detectors.json"), "w").write(json.dumps(detectors))
    open(os.path.join(outpath, "SampleUtterances.json"), "w").write(json.dumps(sampleUtterances))
    modelInfo = {"detectorContentSplitted": trainingConfig.detectorContentSplitted, "textNGrams": trainingConfig.textNGrams}
    open(os.path.join(outpath, "ModelInfo.json"), "w").write(json.dumps(modelInfo))
    if testModelForSearch(productid):
        await publishModels(productid, trainingId)
    else:
        raise PublishingException("Unable to publish models, publishing threshold for test cases failed")