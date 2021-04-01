import os, json, shutil
from __app__.TrainingModule import logHandler
from gensim.models import TfidfModel
from gensim import corpora, similarities
from __app__.TrainingModule.TokenizerModule import getAllNGrams
from __app__.TrainingModule.DetectorsFetchHelper import getAllDetectors
from __app__.TrainingModule.ResourceFilterHelper import getProductId
from __app__.TrainingModule.StorageAccountHelper import StorageAccountHelper
from __app__.TrainingModule.Utilities import compareDetectorSets
from __app__.TrainingModule.Exceptions import *
from __app__.AppSettings.AppSettings import appSettings

class TfIdfTrainer:
    def __init__(self, trainingId, productId, trainingConfig):
        self.trainingId = trainingId
        self.productId = productId
        self.trainingConfig = trainingConfig
    
    def trainDictionary(self, alltokens, outfile, trimDict=False):
        dictionary = corpora.Dictionary(alltokens)
        if trimDict:
            oldSize = len(dictionary)
            dictionary.filter_extremes(no_below=2, no_above=0.3, keep_n=min([500, int(len(dictionary)/2)]))
            logHandler.info(f"Trimmed dictionary from {oldSize} features to {len(dictionary)} features")
        dictionary.save(outfile)
    
    def trainModelM1(self, detector_tokens, outpath):
        if self.trainingConfig.splitDictionary:
            dictionary = corpora.Dictionary.load(os.path.join(outpath, "dictionary1.dict"))
        else:
            dictionary = corpora.Dictionary.load(os.path.join(outpath, "dictionary.dict"))
        corpus = [dictionary.doc2bow(line) for line in detector_tokens]
        model = TfidfModel(corpus)
        index = similarities.MatrixSimilarity(model[corpus])
        model.save(os.path.join(outpath, "m1.model"))
        index.save(os.path.join(outpath, "m1.index"))
    
    def trainModelM2(self, sampleUtterances_tokens, outpath):
        if self.trainingConfig.splitDictionary:
            dictionary = corpora.Dictionary.load(os.path.join(outpath, "dictionary2.dict"))
        else:
            dictionary = corpora.Dictionary.load(os.path.join(outpath, "dictionary.dict"))
        corpus = [dictionary.doc2bow(line) for line in sampleUtterances_tokens]
        model = TfidfModel(corpus)
        index = similarities.MatrixSimilarity(model[corpus])
        model.save(os.path.join(outpath, "m2.model"))
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
            ## Check if training is needed at all
            sah = StorageAccountHelper.getInstance()
            old_detectors_ = sah.getLastModelDetectorsForProduct(self.productId)
            if compareDetectorSets(old_detectors_, detectors_):
                logHandler.info(f"CompareDetectors: Nothing has changed with detectors since last training. Skipping training for {self.productId}.")
                return False, syntheticTestCases
            # A sanity check if the detectors list is not messed up
            if not detectors_ or len(detectors_)<5:
                raise TrainingException(f"TooFewDetectors: Only {len(detectors_)} were found for training. The required threshold is at least 5 detectors. Please check the response from runtime host API.")
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
        # Train dictionary
        try:
            if self.trainingConfig.splitDictionary:
                self.trainDictionary(detector_tokens, os.path.join(outpath, "dictionary1.dict"))
                self.trainDictionary(sampleUtterances_tokens, os.path.join(outpath, "dictionary2.dict"), trimDict=True)
            else:
                self.trainDictionary(detector_tokens + sampleUtterances_tokens, os.path.join(outpath, "dictionary.dict"))
            logHandler.info("DictionaryTrainer: Sucessfully trained dictionary")
        except Exception as e:
            logHandler.error("[ERROR]DictionaryTrainer: " + str(e))
            raise TrainingException("DictionaryTrainer: " + str(e))
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
        modelInfo = {"splitDictionary": self.trainingConfig.splitDictionary, "detectorContentSplitted": self.trainingConfig.detectorContentSplitted, "textNGrams": self.trainingConfig.textNGrams, "modelType": self.trainingConfig.modelType}
        open(os.path.join(outpath, "ModelInfo.json"), "w").write(json.dumps(modelInfo))
        return True, syntheticTestCases