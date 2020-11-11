import os, json, shutil
from __app__.TrainingModule import logHandler
import numpy as np
import gensim
from gensim.similarities import WmdSimilarity
from gensim import corpora
from __app__.TrainingModule.TokenizerModule import getAllNGrams
from __app__.TrainingModule.DetectorsFetchHelper import getAllDetectors
from __app__.TrainingModule.ResourceFilterHelper import getProductId
from __app__.AppSettings.AppSettings import appSettings
from __app__.TrainingModule.Exceptions import *
from __app__.TrainingModule.Utilities import cleanFolder

class WmdTrainer:
    def __init__(self, trainingId, productId, trainingConfig):
        self.trainingId = trainingId
        self.productId = productId
        self.trainingConfig = trainingConfig
        self.w2vModel = gensim.models.KeyedVectors.load_word2vec_format(os.path.join(appSettings.WORD2VEC_PATH, appSettings.WORD2VEC_MODEL_NAME), binary=True, limit=20000)
    
    def trainModelM1(self, detector_tokens, outpath):
        index = WmdSimilarity(detector_tokens, self.w2vModel)
        index.save(os.path.join(outpath, "m1.index"))
    
    def trainModelM2(self, sampleUtterances_tokens, outpath):
        index = WmdSimilarity(sampleUtterances_tokens, self.w2vModel)
        index.save(os.path.join(outpath, "m2.index"))
    
    def prepareSyntheticTestCases(self, detectors):
        syntheticTestCases = []
        for x in detectors:
            allparts = [x["name"] if x["name"] and len(x["name"])>2 else None, x["description"] if x["description"] and len(x["description"])>2 else None] + [y["text"] for y in x["utterances"]]
            syntheticTestCases += [{"query": p, "expectedResults": [x["id"]]} for p in allparts if p]
        return syntheticTestCases
    
    def trainModel(self):
        logHandler.info("Starting training for {0}".format(self.trainingId))
        logHandler.info("Training config {0}".format(json.dumps(self.trainingConfig.__dict__)))
        datapath = os.path.join(appSettings.MODEL_DATA_PATH, "rawdata_{0}".format(self.productId))
        outpath = os.path.join(appSettings.MODEL_DATA_PATH, "{0}".format(self.productId))
        syntheticTestCases = None
        try:
            os.mkdir(outpath)
        except FileExistsError:
            try:
                cleanFolder(outpath)
            except:
                pass
        logHandler.info("Created folder for processed models")
        try:
            detectors_ = getAllDetectors()
            logHandler.info("DataFetcher: Sucessfully fetched detectors for training")
            detectors_ = [detector for detector in detectors_ if self.productId in getProductId(detector["resourceFilter"] if "resourceFilter" in detector else {})]
            logHandler.info(f"DataFetcher: Successfully filtered fetched {len(detectors_)} detectors based on productid {self.productId}.")
            # A sanity check if the detectors list is not messed up
            if not detectors_ or len(detectors_)<30:
                raise TrainingException(f"TooFewDetectors: Only {len(detectors_)} were found for training. The required threshold is at least 30 detectors. Please check the response from runtime host API.")
            open(os.path.join(datapath, "Detectors.json"), "w").write(json.dumps(detectors_))
            detectorsdata = open(os.path.join(datapath, "Detectors.json"), "r").read()
            detectors = json.loads(detectorsdata)
            syntheticTestCases = self.prepareSyntheticTestCases(detectors)
            if self.trainingConfig.detectorContentSplitted:
                detector_mappings = []
                detector_tokens = []
                i = 0
                for x in detectors:
                    detector_mappings.append({"startindex": i, "endindex": i + len(x["utterances"]) + 1, "id": x["id"]})
                    detector_tokens += [getAllNGrams(x["name"], self.trainingConfig.textNGrams)] + [getAllNGrams(x["description"], self.trainingConfig.textNGrams)] +  [getAllNGrams(y["text"], self.trainingConfig.textNGrams) for y in x["utterances"]]
                    i += (len(x["utterances"]) + 2)
                open(os.path.join(outpath, "Mappings.json"), "w").write(json.dumps(detector_mappings))
            else:
                detector_tokens = [getAllNGrams(x["name"] + " " + x["description"] + " " + " ".join([y["text"] for y in x["utterances"]]), self.trainingConfig.textNGrams) for x in detectors]
            logHandler.info("DetectorProcessor: Sucessfully processed detectors data into tokens")
        except Exception as e:
            logHandler.error("[ERROR]DetectorProcessor: " + str(e))
            raise TrainingException("DetectorProcessor: " + str(e))
        try:
            #Stackoverflow and Case Incidents data load
            sampleUtterancesContent = json.loads(open(os.path.join(datapath, "SampleUtterances.json"), "r").read())
            sampleUtterances = (sampleUtterancesContent["incidenttitles"] if self.trainingConfig.includeCaseTitles else []) + (sampleUtterancesContent["stackoverflowtitles"] if self.trainingConfig.includeStackoverflow else [])
            sampleUtterances_tokens = [getAllNGrams(sampleUtterances[i]["text"], self.trainingConfig.textNGrams) for i in range(len(sampleUtterances))]
            logHandler.info("CaseTitlesProcessor: Sucessfully processed sample utterances into tokens")
        except Exception as e:
            logHandler.error("[ERROR]CaseTitlesProcessor: " + str(e))
            raise TrainingException("CaseTitlesProcessor: " + str(e))
        # Train model to search detectors
        if self.trainingConfig.trainDetectors:
            try:
                self.trainModelM1(detector_tokens, outpath)
                logHandler.info("ModelM1Trainer: Sucessfully trained model m1")
            except Exception as e:
                logHandler.error("[ERROR]ModelM1Trainer: " + str(e))
                raise TrainingException("ModelM1Trainer: " + str(e))
        else:
            pass
            logHandler.info("ModelM1Trainer: Training is disabled")
        # Train model to recommend search terms
        if self.trainingConfig.trainUtterances:
            try:
                self.trainModelM2(sampleUtterances_tokens, outpath)
                logHandler.info("ModelM2Trainer: Sucessfully trained model m2")
            except Exception as e:
                logHandler.error("[ERROR]ModelM2Trainer: " + str(e))
                raise TrainingException("ModelM2Trainer: " + str(e))
        # Save data files and configuration files
        open(os.path.join(outpath, "trainingId.txt"), "w").write(str(self.trainingId))
        open(os.path.join(outpath, "Detectors.json"), "w").write(json.dumps(detectors))
        open(os.path.join(outpath, "SampleUtterances.json"), "w").write(json.dumps(sampleUtterances))
        modelInfo = {"detectorContentSplitted": self.trainingConfig.detectorContentSplitted, "textNGrams": self.trainingConfig.textNGrams, "modelType": self.trainingConfig.modelType}
        open(os.path.join(outpath, "ModelInfo.json"), "w").write(json.dumps(modelInfo))
        return syntheticTestCases