import os, json, time
import logging, asyncio
from __app__.TrainingModule.StorageAccountHelper import StorageAccountHelper
from __app__.TestingModule.TextSearchModule import loadModel
from __app__.TestingModule.TestSchema import TestCase
from __app__.TrainingModule.TfIdfTrainer import TfIdfTrainer
from __app__.TrainingModule.WmdTrainer import WmdTrainer
from __app__.TrainingModule.Exceptions import *

class ModelTrainPublish:
    def __init__(self, trainingId, productId, trainingConfig):
        self.trainingId = trainingId
        self.productId = productId
        self.trainingConfig = trainingConfig
        if self.trainingConfig.modelType == "TfIdfSearchModel":
            self.trainer = TfIdfTrainer(self.trainingId, self.productId, self.trainingConfig)
        elif self.trainingConfig.modelType == "WmdSearchModel":
            self.trainer = WmdTrainer(self.trainingId, self.productId, self.trainingConfig)
    
    def testModelForSearch(self):
        model = None
        try:
            model = loadModel(self.productId)
        except Exception as e:
            logging.error("Failed to load the model for {0} with exception {1}".format(self.productId, str(e)), exc_info=True)
            return False
        testCases = []
        try:
            with open("TestingModule/testCases.json", "r") as testFile:
                content = json.loads(testFile.read())
                testCases = [TestCase(t["query"], t["expectedResults"]) for t in content]
                if not testCases:
                    logging.warning("No test cases for product {0} .. skipping testing".format(self.productId))
                    return True
        except Exception as e:
            logging.warning("Exception while reading test cases from file {0} .. skipping testing".format(str(e)))
            return True
        if model and testCases:
            return model.runTestCases(testCases)
    
    async def publishModels(self):
        ts = int(str(time.time()).split(".")[0])
        try:
            sah = StorageAccountHelper()
            for fileName in os.listdir(self.productId):
                logging.info("Uploading {0} to {1}".format(os.path.join(self.productId, fileName), os.path.join(self.productId, "models", str(ts), fileName)))
                await sah.uploadFile(os.path.join(self.productId, fileName), os.path.join(self.productId, "models", str(ts), fileName))
        except Exception as e:
            logging.error("Publishing Exception: {0}".format(str(e)))
            raise PublishingException(str(e))
    
    async def trainPublish(self):
        self.trainer.trainModel()
        tested = True
        if not self.trainingConfig.modelType == "WmdSearchModel":
            tested = self.testModelForSearch()
        if tested:
            await self.publishModels()
        else:
            raise PublishingException("Unable to publish models, publishing threshold for test cases failed")