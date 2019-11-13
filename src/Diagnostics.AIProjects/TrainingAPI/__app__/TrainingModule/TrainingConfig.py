class TrainingConfig:
	def __init__(self, trainingConfig):
		self.downloadStackoverflowEnabled = trainingConfig.get("downloadStackoverflowEnabled", False)
		self.includeStackoverflow = trainingConfig.get("includeStackoverflow", True)
		self.stackoverflowTags = trainingConfig.get("stackoverflowTags", [])
		self.stackoverFlowTopN = trainingConfig.get("stackoverFlowTopN", 50)
		self.stackoverflowKey = trainingConfig.get("stackoverflowKey", None)
		
		self.downloadCaseTitlesEnabled = trainingConfig.get("downloadCaseTitlesEnabled", False)
		self.includeCaseTitles = trainingConfig.get("includeCaseTitles", True)
		self.runExtractionEnabled = trainingConfig.get("runExtractionEnabled", True)
		self.extractionRatio = trainingConfig.get("extractionRatio", 0.1)
		self.caseTitlesDaysSince = trainingConfig.get("caseTitlesDaysSince", 50)
		
		self.textNGrams = trainingConfig.get("textNGrams", 1)
		self.detectorContentSplitted = trainingConfig.get("detectorContentSplitted", False)
		self.trainDetectors = trainingConfig.get("trainDetectors", False)
		self.trainUtterances = trainingConfig.get("trainUtterances", False)

		self.modelType = trainingConfig.get("modelType", "TfIdfSearchModel")