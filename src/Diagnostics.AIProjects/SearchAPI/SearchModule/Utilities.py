import os, json, shutil
from SearchModule.Logger import loggerInstance
from SearchModule.Exceptions import *

SITE_ROOT = os.getcwd()

modelsPath = "models"

# To convert any file/folder path to absolute path
def absPath(path):
    return os.path.join(SITE_ROOT, path)

# To copy folder from one path to another
def copyFolder(src, dst):
    if os.path.isdir(absPath(src)):
        try:
            if os.path.isdir(absPath(dst)):
                try:
                    shutil.rmtree(absPath(dst))
                except Exception as e:
                    raise Exception("folderCopyTask: Delete existing folder Exception: {0}".format(str(e)))
            try:
                shutil.copytree(absPath(src), absPath(dst))
            except Exception as e:
                raise Exception("folderCopyTask: Copying to folder Exception: {0}".format(str(e)))
        except Exception as e:
            exception = CopyTaskException("folderCopyTask: {0}, src:{1}, dst:{2}".format(str(e), absPath(src), absPath(dst)))
            loggerInstance.logHandledException("folderCopyTask", exception)
            raise exception
    else:
        exception = CopySourceFolderNotFoundException("Source folder not found Copying Folder from {0} to {1}".format(absPath(src), absPath(dst)))
        loggerInstance.logHandledException("folderCopyTask", exception)
        raise exception

# To move models for refresh of a given product
def moveModels(productid, path=""):
    try:
        copyFolder(os.path.join(modelsPath, productid), absPath(os.path.join(path, productid)))
        loggerInstance.logInsights("copyModelToFolder: Copied Folder for product {0} to {1}".format(productid, absPath(os.path.join(path, productid))))
    except Exception as e:
        raise ModelDownloadFailed("Model can't be downloaded " + str(e))

# Get resources configuration
def downloadResourceConfig():
    try:
        copyFolder(os.path.join(modelsPath, "resourceConfig"), absPath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "resourceConfig")))
    except Exception as e:
        raise ResourceConfigDownloadFailed("Resource config can't be downloaded " + str(e))

config = json.loads(open(absPath(os.path.join("SearchModule", "resourceConfig.json")), "r").read())
resourceConfig = config["resourceConfig"]

# Get productId/pesId for a given resource
def getProductId(resourceObj):
    productids = []
    if resourceObj["ResourceType"] == "App":
        apptypes = resourceObj["AppType"].split(",")
        for app in apptypes:
            if app == "WebApp":
                platformtypes = resourceObj["PlatformType"].split(",")
                for platform in platformtypes:
                    try:
                        productids.append(resourceConfig[resourceObj["ResourceType"]][app][platform])
                    except KeyError:
                        pass
    if productids:
        return list(set(productids))
    else:
        return None

def getAllProductIds(node):
    allProductIds = []
    if type(node).__name__=='dict':
        for key in node.keys():
            allProductIds += getAllProductIds(node[key])
    else:
        allProductIds.append(node)
    return allProductIds

# Verify whether a file exists and is accessible to the app
def verifyFile(filename, absolute=False, prelogMessage=""):
    fName = absPath(filename) if not absolute else filename
    try:
        with open(fName, "rb") as fp:
            fp.close()
        loggerInstance.logInsights("{0}Verified File {1}".format(prelogMessage, fName))
        return True
    except FileNotFoundError:
        loggerInstance.logInsights("{0}Failed to Verify File {1}".format(prelogMessage, fName))
        return False