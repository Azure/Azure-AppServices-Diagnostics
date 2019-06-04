from winreg import *
githubFolderPath = None
kustoClientId = ""
kustoClientSecret = ""
try:
	aReg = ConnectRegistry(None,HKEY_LOCAL_MACHINE)
	aKey = OpenKey(aReg, r"SOFTWARE\Microsoft\IIS Extensions\Web Hosting Framework")
	sourceWatcherKey = OpenKey(aKey, "SourceWatcher")
	dataProvidersKey = OpenKey(aKey, "DiagnosticDataProviders")
	try:
		githubKey = OpenKey(sourceWatcherKey,"Github")
		githubFolderPath = QueryValueEx(githubKey, "DestinationScriptsPath")[0]
	except:
		try:
			githubKey = OpenKey(sourceWatcherKey,"Local")
			githubFolderPath = QueryValueEx(githubKey, "LocalScriptsPath")[0]
		except:
			raise Exception("RegistryError: Detector path not found")
	kustoKey = OpenKey(dataProvidersKey, "Kusto")
	kustoClientId = QueryValueEx(kustoKey, "ClientId")[0]
	kustoClientSecret = QueryValueEx(kustoKey, "AppKey")[0]
except:
	pass
