import os, json, shutil
import logging
from gensim.models import TfidfModel
from gensim import corpora, similarities
from __app__.TrainingModule.TokenizerModule import getAllNGrams
from __app__.TrainingModule.DetectorsFetchHelper import getAllDetectors
from __app__.TrainingModule.ResourceFilterHelper import getProductId
from __app__.TrainingModule.Exceptions import *

class TfIdfTrainer:
    def __init__(self, trainingId, productId, trainingConfig):
        self.trainingId = trainingId
        self.productId = productId
        self.trainingConfig = trainingConfig
    
    def trainDictionary(self, alltokens, outpath):
        dictionary = corpora.Dictionary(alltokens)
        dictionary.save(os.path.join(outpath, "dictionary.dict"))
    
    def trainModelM1(self, detector_tokens, outpath):
        dictionary = corpora.Dictionary.load(os.path.join(outpath, "dictionary.dict"))
        corpus = [dictionary.doc2bow(line) for line in detector_tokens]
        model = TfidfModel(corpus)
        index = similarities.MatrixSimilarity(model[corpus])
        model.save(os.path.join(outpath, "m1.model"))
        index.save(os.path.join(outpath, "m1.index"))
    
    def trainModelM2(self, sampleUtterances_tokens, outpath):
        dictionary = corpora.Dictionary.load(os.path.join(outpath, "dictionary.dict"))
        corpus = [dictionary.doc2bow(line) for line in sampleUtterances_tokens]
        model = TfidfModel(corpus)
        index = similarities.MatrixSimilarity(model[corpus])
        model.save(os.path.join(outpath, "m2.model"))
        index.save(os.path.join(outpath, "m2.index"))
    
    def trainModel(self):
        logging.info("Starting training for {0}".format(self.trainingId))
        logging.info("Training config {0}".format(json.dumps(self.trainingConfig.__dict__)))
        datapath = "rawdata_{0}".format(self.productId)
        outpath = "{0}".format(self.productId)
        try:
            os.mkdir(outpath)
        except FileExistsError:
            try:
                cleanFolder(outpath)
            except:
                pass
        logging.info("Created folder for processed models")
        try:
            detectors_ = getAllDetectors()
            logging.info("DataFetcher: Sucessfully fetched detectors for training")
            detectors_ = [detector for detector in detectors_ if self.productId in getProductId(detector["resourceFilter"] if "resourceFilter" in detector else {})]
            logging.info(f"DataFetcher: Successfully filtered fetched {len(detectors_)} detectors based on productid {self.productId}.")
            if detectors_ and len(detectors_)>0:
                open(os.path.join(datapath, "Detectors.json"), "w").write(json.dumps(detectors_))
            detectorsdata = open(os.path.join(datapath, "Detectors.json"), "r").read()
            detectors = json.loads(detectorsdata)
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
            logging.info("DetectorProcessor: Sucessfully processed detectors data into tokens")
        except Exception as e:
            logging.error("[ERROR]DetectorProcessor: " + str(e))
            raise TrainingException("DetectorProcessor: " + str(e))
        try:
            #Stackoverflow and Case Incidents data load
            sampleUtterancesContent = json.loads(open(os.path.join(datapath, "SampleUtterances.json"), "r").read())
            sampleUtterances = (sampleUtterancesContent["incidenttitles"] if self.trainingConfig.includeCaseTitles else []) + (sampleUtterancesContent["stackoverflowtitles"] if self.trainingConfig.includeStackoverflow else [])
            sampleUtterances_tokens = [getAllNGrams(sampleUtterances[i]["text"], self.trainingConfig.textNGrams) for i in range(len(sampleUtterances))]
            logging.info("CaseTitlesProcessor: Sucessfully processed sample utterances into tokens")
        except Exception as e:
            logging.error("[ERROR]CaseTitlesProcessor: " + str(e))
            raise TrainingException("CaseTitlesProcessor: " + str(e))
        # Train dictionary
        try:
            self.trainDictionary(detector_tokens + sampleUtterances_tokens, outpath)
            logging.info("DictionaryTrainer: Sucessfully trained dictionary")
        except Exception as e:
            logging.error("[ERROR]DictionaryTrainer: " + str(e))
            raise TrainingException("DictionaryTrainer: " + str(e))
        # Train model to search detectors
        if self.trainingConfig.trainDetectors:
            try:
                self.trainModelM1(detector_tokens, outpath)
                logging.info("ModelM1Trainer: Sucessfully trained model m1")
            except Exception as e:
                logging.error("[ERROR]ModelM1Trainer: " + str(e))
                raise TrainingException("ModelM1Trainer: " + str(e))
        else:
            pass
            logging.info("ModelM1Trainer: Training is disabled")
        # Train model to recommend search terms
        if self.trainingConfig.trainUtterances:
            try:
                self.trainModelM2(sampleUtterances_tokens, outpath)
                logging.info("ModelM2Trainer: Sucessfully trained model m2")
            except Exception as e:
                logging.error("[ERROR]ModelM2Trainer: " + str(e))
                raise TrainingException("ModelM2Trainer: " + str(e))
        # Save data files and configuration files
        open(os.path.join(outpath, "trainingId.txt"), "w").write(str(self.trainingId))
        open(os.path.join(outpath, "Detectors.json"), "w").write(json.dumps(detectors))
        open(os.path.join(outpath, "SampleUtterances.json"), "w").write(json.dumps(sampleUtterances))
        modelInfo = {"detectorContentSplitted": self.trainingConfig.detectorContentSplitted, "textNGrams": self.trainingConfig.textNGrams, "modelType": self.trainingConfig.modelType}
        open(os.path.join(outpath, "ModelInfo.json"), "w").write(json.dumps(modelInfo))