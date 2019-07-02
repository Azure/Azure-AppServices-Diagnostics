import os, json, requests
from ResourceFilterHelper import getProductId
from Logger import loggerInstance

class TrainingException(Exception):
    pass

class DetectorsFetcher:
    def __init__(self, detectorsUrl, trainingId):
        config = json.loads(open("resourceConfig/config.json", "r").read())
        self.detectorsUrl = detectorsUrl if detectorsUrl else "http://localhost:{0}/internal/detectors".format(config["internalApiPort"])
        self.trainingId = trainingId

    def fetchDetectors(self, productid, datapath):
        trainingId = self.trainingId
        try:
            content = json.loads(requests.get(self.detectorsUrl).content)
            loggerInstance.logToFile("{0}.log".format(trainingId), "DetectorsFetcher: Fetched " + str(len(content)) + " detectors")
        except:
            loggerInstance.logToFile("{0}.log".format(trainingId), "[ERROR]DetectorsFetcher: " + str(e))
            raise TrainingException("DetectorsFetcher: " + str(e))
        detectors = [detector for detector in content if (productid in getProductId(detector["resourceFilter"]))]
        loggerInstance.logToFile("{0}.log".format(trainingId), "DetectorsFetcher: Shortlisted " + str(len(detectors)) + " detectors for training based on productId " + str(productid))
        for detector in detectors:
            if detector["metadata"]:
                md = json.loads(detector["metadata"])
                detector["utterances"] = md["utterances"] if "utterances" in md else []
            else:
                detector["utterances"] = []
        if len(content)>0:
            try:
                open(os.path.join(datapath, "Detectors.json"), "w").write(json.dumps(detectors, indent=4))
                loggerInstance.logToFile("{0}.log".format(trainingId), "DetectorsFetcher: Written detectors to file Detectors.json")
            except:
                loggerInstance.logToFile("{0}.log".format(trainingId), "[ERROR]DetectorsFetcher: " + str(e))
                raise TrainingException("DetectorsFetcher: " + str(e))