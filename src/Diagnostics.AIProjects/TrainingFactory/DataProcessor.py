from SampleUtterancesFetcher import SampleUtterancesFetcher
from DetectorsFetcher import DetectorsFetcher
import gc, os, json
from Logger import loggerInstance

class TrainingException(Exception):
    pass
class DataProcessor:
    def __init__(self, trainingConfig, trainingId):
        self.trainingConfig = trainingConfig
        self.trainingId = trainingId
        pass

    def prepareDataForTraining(self, productid):
        config = json.loads(open("resourceConfig/config.json", "r").read())
        trainingId = self.trainingId
        # check product in training config
        rawdatapath = "rawdata_{0}".format(productid)
        try:
            os.makedirs(rawdatapath)
        except FileExistsError:
            pass
        loggerInstance.logToFile("{0}.log".format(trainingId), "Created folders for raw data")
        try:
            sampleUtterancesFetcher = SampleUtterancesFetcher(self.trainingConfig, self.trainingId)
            sampleUtterancesFetcher.run(productid, rawdatapath)
            loggerInstance.logToFile("{0}.log".format(trainingId), "SampleUtterancesFetcher: Successfully fetched & extracted sample utterances")
        except Exception as e:
            loggerInstance.logToFile("{0}.log".format(trainingId), "[ERROR]SampleUtterancesFetcher: " + str(e))
            raise TrainingException("SampleUtterancesFetcher: " + str(e))
        try:
            detectorsFetcher = DetectorsFetcher("http://localhost:{0}/internal/detectors".format(config["internalApiPort"]), self.trainingId)
            detectorsFetcher.fetchDetectors(productid, rawdatapath)
            loggerInstance.logToFile("{0}.log".format(trainingId), "DetectorsFetcher: Successfully fetched detectors")
        except Exception as e:
            loggerInstance.logToFile("{0}.log".format(trainingId), "[ERROR]DetectorsFetcher: " + str(e))
            raise TrainingException("DetectorsFetcher: " + str(e))
        gc.collect()