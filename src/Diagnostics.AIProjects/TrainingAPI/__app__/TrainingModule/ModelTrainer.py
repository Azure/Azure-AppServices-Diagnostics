import os, json, time
import asyncio
from __app__.TrainingModule import logHandler
from __app__.TrainingModule.StorageAccountHelper import StorageAccountHelper
from __app__.TestingModule.TextSearchModule import loadModel
from __app__.TestingModule.TestSchema import TestCase
from __app__.TrainingModule.TfIdfTrainer import TfIdfTrainer
from __app__.TrainingModule.WmdTrainer import WmdTrainer
from __app__.TrainingModule.Exceptions import *
from __app__.AppSettings.AppSettings import appSettings

class ModelTrainPublish:
    def __init__(self, trainingId, productId, trainingConfig):
        self.trainingId = trainingId
        self.productId = productId
        self.trainingConfig = trainingConfig
        if self.trainingConfig.modelType == "TfIdfSearchModel":
            self.trainer = TfIdfTrainer(self.trainingId, self.productId, self.trainingConfig)
        elif self.trainingConfig.modelType == "WmdSearchModel":
            self.trainer = WmdTrainer(self.trainingId, self.productId, self.trainingConfig)
    
    def testModelForSearch(self, syntheticTestCases):
        logHandler.info(f"Starting testing. Received {len(syntheticTestCases)} synthetic test cases to run.")
        model = None
        try:
            model = loadModel(self.productId)
        except Exception as e:
            logHandler.error("Failed to load the model for {0} with exception {1}".format(self.productId, str(e)), exc_info=True)
            return False
        testCases = []
        try:
            with open(os.path.join(appSettings.MODEL_DATA_PATH, "{0}/testCases.json".format(self.productId)), "r") as testFile:
                content = json.loads(testFile.read())
                testCases = [TestCase(t["query"], t["expectedResults"]) for t in content]
                if not testCases:
                    logHandler.warning("No test cases for product {0} .. will run against only synthetic test cases".format(self.productId))
                    pass
        except Exception as e:
            logHandler.warning("Exception while reading test cases from file {0} .. will run against only synthetic test cases".format(str(e)))
            pass
        testCases += [TestCase(t["query"], t["expectedResults"]) for t in syntheticTestCases]
        if model and testCases:
            return model.runTestCases(testCases)
        else:
            logHandler.warning("No test cases to run against. Aborting publish.")
            return False
    
    async def publishModels(self):
        datapath = appSettings.MODEL_DATA_PATH
        ts = int(str(time.time()).split(".")[0])
        try:
            sah = StorageAccountHelper.getInstance()
            for fileName in os.listdir(os.path.join(datapath, self.productId)):
                logHandler.info("Uploading {0} to {1}".format(os.path.join(datapath, self.productId, fileName), os.path.join(self.productId, "models", str(ts), fileName)))
                await sah.uploadFile(os.path.join(datapath, self.productId, fileName), os.path.join(self.productId, "models", str(ts), fileName))
        except Exception as e:
            logHandler.error("Publishing Exception: {0}".format(str(e)))
            raise PublishingException(str(e))
    
    async def trainPublish(self):
        hasTrained, syntheticTestCases = self.trainer.trainModel()
        if not hasTrained:
            logHandler.info("Training was not needed.")
            return
        tested = False
        if not self.trainingConfig.modelType == "WmdSearchModel":
            tested = self.testModelForSearch(syntheticTestCases)
        if tested:
            await self.publishModels()
        else:
            raise PublishingException("Unable to publish models, publishing threshold for test cases failed")