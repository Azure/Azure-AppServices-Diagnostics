import os, asyncio, json
from __app__.TrainingModule import logHandler
from azure.storage.blob import BlobServiceClient
#from azure.common import AzureMissingResourceHttpError
from __app__.AppSettings.AppSettings import appSettings

class StorageAccountHelper:
	__instance = None
	@staticmethod
	def getInstance():
		if StorageAccountHelper.__instance == None:
			return StorageAccountHelper()
		else:
			return StorageAccountHelper.__instance

	def __init__(self):
		if StorageAccountHelper.__instance != None:
			raise Exception("StorageAccountHelper is a singleton class")
		else:
			StorageAccountHelper.__instance = self
		self.firstTime = True
		if appSettings.STORAGE_ACCOUNT_NAME and appSettings.STORAGE_ACCOUNT_KEY:
			self.blob_service = BlobServiceClient(account_url=f"https://{appSettings.STORAGE_ACCOUNT_NAME}.blob.core.windows.net", credential=appSettings.STORAGE_ACCOUNT_KEY)
			#self.blob_service = BlockBlobService(account_name=appSettings.STORAGE_ACCOUNT_NAME, account_key=appSettings.STORAGE_ACCOUNT_KEY)
		else:
			raise Exception('Failed to read storage account name and key values from configurations')

	def getLastModelDetectorsForProduct(self, productId):
		containerClient = self.blob_service.get_container_client(container=appSettings.STORAGE_ACCOUNT_CONTAINER_NAME)
		allblobsList = [blob for blob in list(containerClient.list_blobs()) if blob.name.startswith("{0}/models".format(productId))]
		if not len(allblobsList)>0:
			return None
		folders = list(set([int(blob.name.split("/")[2]) for blob in allblobsList]))
		latestFolder = str(max(folders))
		latestFolderFiles = [blob.name for blob in allblobsList if latestFolder in blob.name]
		detectorsFile = [blobname for blobname in latestFolderFiles if "detectors.json" in blobname.lower()]
		if not detectorsFile:
			return None
		blobClient = containerClient.get_blob_client(detectorsFile[0])
		blob_data = blobClient.download_blob().readall()
		return json.loads(blob_data)

	def downloadFile(self, blobname, destpath=None):
		writepath = os.path.join(appSettings.MODEL_DATA_PATH, os.path.normpath('/'.join(blobname.split("/")[:-1])))
		fileName = blobname.split("/")[-1]
		if destpath:
			writepath = os.path.join(appSettings.MODEL_DATA_PATH, os.path.normpath(destpath))
		try:
			os.makedirs(writepath)
		except:
			pass
		try:
			blobClient = self.blob_service.get_blob_client(container=appSettings.STORAGE_ACCOUNT_CONTAINER_NAME, blob=blobname)
			with open(os.path.join(writepath, fileName), "wb") as blobFile:
				blob_data = blobClient.download_blob()
				blob_data.readinto(blobFile)
			#self.blob_service.get_blob_to_path(appSettings.STORAGE_ACCOUNT_CONTAINER_NAME, blobname, os.path.join(writepath, fileName))
			logHandler.info("Downloaded file {0} to path {1}".format(blobname, writepath))
			return writepath
		except Exception as e:
			logHandler.error("File {0} not found on blob".format(blobname), exc_info=True)
			raise e
	
	async def uploadFile(self, srcfilepath, destfilepath):
		blobClient = self.blob_service.get_blob_client(container=appSettings.STORAGE_ACCOUNT_CONTAINER_NAME, blob=destfilepath)
		with open(srcfilepath, "rb") as data:
			blobClient.upload_blob(data)
		#self.blob_service.create_blob_from_path(appSettings.STORAGE_ACCOUNT_CONTAINER_NAME, destfilepath, srcfilepath)