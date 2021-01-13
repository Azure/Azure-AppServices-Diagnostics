from __app__.TrainingModule.AcquireToken import acquireAccessToken
from __app__.AppSettings.AppSettings import appSettings
import requests, json

DETECTORS_TO_IGNORE = ["test_keystone_detector"]

def getAllDetectors():
    url = appSettings.DETECTORS_URL
    request_headers = {
        "Authorization": "Bearer {0}".format(acquireAccessToken())
    }
    try:
        request = requests.get(url, headers=request_headers)
    except requests.exceptions.RequestException as e:
        raise Exception("Failed to connect to detectors api {0}".format(str(e)))
    if request.status_code == 200:
        try:
            content = json.loads(request.content)
            rescontent = []
            for detector in content:
                if detector["id"] in DETECTORS_TO_IGNORE:
                    continue
                detector["utterances"] = (json.loads(detector["metadata"] or "[]")["utterances"] if "utterances" in json.loads(detector["metadata"] or "[]") else []) if "metadata" in detector else []
                detector.pop("metadata", None)
                rescontent.append(detector)
            return rescontent
        except json.decoder.JSONDecodeError as e:
            raise Exception("Failed to parse Detectors API response as JSON {0}".format(str(e)))
    else:
        raise Exception("Detectors API response is not success")