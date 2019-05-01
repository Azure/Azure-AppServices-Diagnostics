from winreg import *
aReg = ConnectRegistry(None,HKEY_LOCAL_MACHINE)
aKey = OpenKey(aReg, r"SOFTWARE\Microsoft\IIS Extensions\Web Hosting Framework")
sourceWatcherKey = OpenKey(aKey, "SourceWatcher")
dataProvidersKey = OpenKey(aKey, "DiagnosticDataProviders")
githubKey = OpenKey(sourceWatcherKey,"Github")
gitHubFolderPath = QueryValueEx(githubKey, "DestinationScriptsPath")[0]
kustoKey = OpenKey(dataProvidersKey, "Kusto")
kustoClientId = QueryValueEx(kustoKey, "ClientId")[0]
kustoAuthority = QueryValueEx(kustoKey, "AADAuthority")[0]
kustoClientSecret = QueryValueEx(kustoKey, "AppKey")[0]
