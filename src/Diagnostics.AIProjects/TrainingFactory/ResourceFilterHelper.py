import json

resourceConfig = json.loads(open("metadata/config.json", "r").read())["resourceConfig"]
def getProductId(resourceObj, refresh=False):
    global resourceConfig
    if refresh:
        resourceConfig = json.loads(open("metadata/config.json", "r").read())
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
    else:
        try:
            productids.append(resourceConfig[resourceObj["ResourceType"]])
        except KeyError:
            pass
    if productids:
        return list(set(productids))
    else:
        return []