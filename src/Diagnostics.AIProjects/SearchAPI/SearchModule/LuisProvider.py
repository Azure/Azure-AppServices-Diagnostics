import requests, json
from SearchModule import app
luis_api_endpoints = {
		"GET_INTENTS": "https://westus.api.cognitive.microsoft.com/luis/prediction/v3.0/apps/{appId}/slots/production/predict?subscription-key={key}&verbose=true&show-all-intents=true&log=true&query={queryString}",
	}
def getLuisPredictions(queryString):
    appId = app.config["LUIS_APP_ID"]
    key = app.config["LUIS_APP_KEY"]
    url = luis_api_endpoints["GET_INTENTS"].replace("{appId}", appId).replace("{key}", key).replace("{queryString}", queryString)
    r = requests.get(url)
    res = json.loads(r.content)
    try:
        predictions = res["prediction"]["intents"]
    except Exception as e:
        raise Exception(f"Failed to fetch results from LUIS response: {str(e)}")
    preds = []
    for intent in predictions.keys():
        if predictions[intent]["score"]>0.1:
            preds.append({
                "detector": intent,
                "score": round(float(predictions[intent]["score"]), 3)
            })
    return preds[: min([3, len(preds)])]

def mergeLuisResults(result):
    luis_results = result["luis_results"]
    for res in luis_results:
        found = False
        for i in range(len(result["results"])):
            if result["results"][i]["detector"]==res["detector"]:
                found = True
                result["results"][i]["score"] = max([result["results"][i]["score"], res["score"]])
                break
        if not found:
            # To make sure LUIS results are cleared by runtime host api
            res["score"] = max([0.31, res["score"]])
            result["results"].append(res)
    return result    