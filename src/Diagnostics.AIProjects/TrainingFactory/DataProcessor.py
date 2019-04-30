from SampleUtterancesFetcher import SampleUtterancesFetcher
from DetectorsFetcher import DetectorsFetcher
import gc, os

class DataProcessor:
    def __init__(self):
        pass

    def prepareDataForTraining(self, productid):
        # check product in training config
        rawdatapath = "rawdata_{0}".format(productid)
        try:
            os.makedirs(rawdatapath)
        except FileExistsError:
            pass
        sampleUtterancesFetcher = SampleUtterancesFetcher()
        sampleUtterancesFetcher.run(productid, rawdatapath)
        detectorsFetcher = DetectorsFetcher("http://localhost:{0}/internal/detectors".format("1743"))
        detectorsFetcher.fetchDetectors(productid, rawdatapath)
        gc.collect()