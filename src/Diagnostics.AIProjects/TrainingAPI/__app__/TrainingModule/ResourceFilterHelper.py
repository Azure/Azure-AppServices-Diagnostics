import json, os
from __app__.AppSettings.AppSettings import appSettings

resourceConfig = {}
def findProductIdRecursive(configDict, productid):
    if type(configDict).__name__=="str":
        if configDict==productid:
            return True
        return False
    try:
        return any([findProductIdRecursive(configDict[key], productid) for key in configDict.keys()])
    except:
        return False

def findProductId(productid):
    resourceConfig = json.loads(open(os.path.join(appSettings.MODEL_DATA_PATH, "resourceConfig/config.json"), "r").read())["resourceConfig"]
    return findProductIdRecursive(resourceConfig, productid)

def getProductId(resourceObj):
    resourceConfig = json.loads(open(os.path.join(appSettings.MODEL_DATA_PATH, "resourceConfig/config.json"), "r").read())["resourceConfig"]
    productids = []
    if "ResourceType" in resourceObj and (resourceObj["ResourceType"] == "App"):
        apptypes = resourceObj["AppType"].split(",")
        for app in apptypes:
            if app == "WebApp":
                platformtypes = resourceObj["PlatformType"].split(",")
                for platform in platformtypes:
                    try:
                        productids.append(resourceConfig[resourceObj["ResourceType"]][app][platform])
                    except KeyError:
                        pass
            elif app == "FunctionApp":
                try:
                    productids.append(resourceConfig[resourceObj["ResourceType"]][app])
                except KeyError:
                    pass
    else:
        try:
            productids.append(resourceConfig[resourceObj["ResourceType"]])
        except KeyError:
            pass
    if productids:
        return list(set(productids))
    else:
        return []