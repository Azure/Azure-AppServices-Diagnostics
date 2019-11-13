class ModelInfo:
    def __init__(self, modelInfo):
        if not modelInfo:
            modelInfo = {}
        self.modelType = modelInfo.get("modelType", "TfIdfSearchModel")
        self.detectorContentSplitted = modelInfo.get("detectorContentSplitted", False)
        self.textNGrams = modelInfo.get("textNGrams", 1)