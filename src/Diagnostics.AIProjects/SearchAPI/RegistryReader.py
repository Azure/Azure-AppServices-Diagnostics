from winreg import *
detectorsFolderPath = None
kustoClientId = ""
kustoClientSecret = ""
try:
	aReg = ConnectRegistry(None,HKEY_LOCAL_MACHINE)
	aKey = OpenKey(aReg, r"SOFTWARE\Microsoft\IIS Extensions\Web Hosting Framework")
	sourceWatcherKey = OpenKey(aKey, "SourceWatcher")
	dataProvidersKey = OpenKey(aKey, "DiagnosticDataProviders")
	try:
		githubKey = OpenKey(sourceWatcherKey,"Github")
		detectorsFolderPath = QueryValueEx(githubKey, "DestinationScriptsPath")[0]
	except:
		try:
			localKey = OpenKey(sourceWatcherKey,"Local")
			detectorsFolderPath = QueryValueEx(localKey, "LocalScriptsPath")[0]
		except:
			raise Exception("RegistryError: Detector path not found")
	kustoKey = OpenKey(dataProvidersKey, "Kusto")
	kustoClientId = QueryValueEx(kustoKey, "ClientId")[0]
	kustoClientSecret = QueryValueEx(kustoKey, "AppKey")[0]
except:
	pass
