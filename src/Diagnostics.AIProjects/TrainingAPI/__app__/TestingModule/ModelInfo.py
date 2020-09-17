class ModelInfo:
    def __init__(self, modelInfo):
        if not modelInfo:
            modelInfo = {}
        self.detectorContentSplitted = modelInfo.get("detectorContentSplitted", False)
        self.textNGrams = modelInfo.get("textNGrams", 1)
        self.splitDictionary = modelInfo.get("splitDictionary", False)