import os, datetime, pytz, time
from azure.storage.blob import BlockBlobService
from SearchModule import app
import pythoncom
from SearchModule.TextSearchModule import refreshModel

class StorageAccountHelper:
	def __init__(self, logger):
		self.firstTime = {}
		self.loggerInstance = logger
		self.blob_service = None
	
	def watchModels(self, productIds):
		# Run coinitialize for the new thread to be able to log
		pythoncom.CoInitialize()
		if ("STORAGE_ACCOUNT_NAME" in app.config and app.config["STORAGE_ACCOUNT_NAME"]) and ("STORAGE_ACCOUNT_KEY" in app.config and app.config["STORAGE_ACCOUNT_KEY"]):
			self.blob_service = BlockBlobService(account_name=app.config["STORAGE_ACCOUNT_NAME"], account_key=app.config["STORAGE_ACCOUNT_KEY"])
		else:
			self.loggerInstance.logHandledException("modelRefreshTask", Exception("Failed to read storage account name and key values from configurations"))
			raise Exception('Failed to read storage account name and key values from configurations')
		for productId in productIds:
			self.firstTime[productId] = True
		while True:
			for productId in productIds:
				modelOnDisk = None
				try:
					modelOnDisk = open(os.path.join(os.getcwd(), app.config["TRAINED_MODELS_PATH"], productId, "trainingId.txt")).read()
				except:
					pass
				self.loggerInstance.logInsights("modelRefreshTask: Running model watcher for {0}".format(productId))
				isChanged = False
				try:
					now = datetime.datetime.now(pytz.utc)
					if self.blob_service:
						allblobsList = [blob for blob in list(self.blob_service.list_blobs(app.config["STORAGE_ACCOUNT_CONTAINER_NAME"])) if blob.name.startswith("{0}/models".format(productId))]
						if not len(allblobsList)>0:
							self.firstTime[productId] = False
							continue
						folders = list(set([int(blob.name.split("/")[2]) for blob in allblobsList]))
						latestFolder = str(max(folders))
						trainingId = self.blob_service.get_blob_to_text(app.config["STORAGE_ACCOUNT_CONTAINER_NAME"], f"{productId}/models/{latestFolder}/trainingId.txt")
						if trainingId:
							trainingId = trainingId.content
						if modelOnDisk and trainingId and trainingId==modelOnDisk:
							isChanged = False
						else:
							downloadList = [blob for blob in allblobsList if blob.name.startswith("{0}/models/{1}".format(productId, latestFolder))]
							for blob in downloadList:
								blobname = blob.name
								dirpath = os.path.join(os.getcwd(), app.config["TRAINED_MODELS_PATH"], productId)
								try:
									os.makedirs(dirpath)
								except:
									pass
								self.blob_service.get_blob_to_path(app.config["STORAGE_ACCOUNT_CONTAINER_NAME"], blobname, os.path.join(dirpath, blobname.split("/")[-1]))
								isChanged = True
					if self.firstTime[productId]:
						self.firstTime[productId] = False
				except Exception as e:
					pass
				if isChanged:
					try:
						self.loggerInstance.logInsights("modelRefreshTask: Models are changed for {0}. Triggering model refresh.".format(productId))
						refreshModel(productId)
					except Exception as e:
						self.loggerInstance.logHandledException("modelRefreshTask", "Failed to refresh model: {0}".format(str(e)))
			time.sleep(5*60)