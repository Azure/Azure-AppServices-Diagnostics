from flask import request
from flask_cors import CORS, cross_origin
from datetime import datetime, timezone
from functools import wraps
import threading, json, time
from googletrans import Translator

from AuthModule.authhandler import authProvider
from SearchModule import app
from SearchModule.TextSearchModule import loadModel, refreshModel, freeModel, loaded_models
from SearchModule.Utilities import resourceConfig, getProductId, getAllProductIds
from SearchModule.StorageAccountHelper import StorageAccountHelper
from SearchModule.Logger import loggerInstance
from SearchModule.LuisProvider import getLuisPredictions, mergeLuisResults
import urllib.parse, re

translator = Translator()
specialChars = r'[^(0-9a-zA-Z )]+'
######## RUN THE API SERVER IN FLASK  #############
def getUTCTime():
    return datetime.now(timezone.utc)

def getLatency(startTime, endTime):
    return (endTime-startTime).total_seconds()*1000

def getRequestId(req):
    if req.method == 'POST':
        data = json.loads(request.data.decode('utf-8'))
        return data['requestId'] if 'requestId' in data else None
    elif req.method == 'GET':
        return request.args.get('requestId')
    return None

def loggingProvider(requestIdRequired=True):
    def loggingOuter(f):
        @wraps(f)
        def logger(*args, **kwargs):
            startTime = getUTCTime()
            res = None
            try:
                requestId = getRequestId(request)
            except Exception as e:
                exceptionMessage = "Failed to parse request to get requestId: {0}".format(str(e))
                loggerInstance.logUnhandledException("ErrorRequestId", exceptionMessage)
                return (exceptionMessage, 500)
            requestId = requestId if requestId else (str(uuid.uuid4()) if not requestIdRequired else None)
            if not requestId:
                res = ("BadRequest: Missing parameter requestId", 400)
                endTime = getUTCTime()
                loggerInstance.logApiSummary("Null", str(request.url_rule), res[1], getLatency(startTime, endTime), startTime.strftime("%H:%M:%S.%f"), endTime.strftime("%H:%M:%S.%f"), res[0])
                return res
            else:
                try:
                    res = f(*args, **kwargs)
                except Exception as e:
                    res = (str(e), 500)
                    loggerInstance.logUnhandledException(requestId, str(e))
            endTime = getUTCTime()
            if res:
                loggerInstance.logApiSummary(requestId, str(request.url_rule), res[1], getLatency(startTime, endTime), startTime.strftime("%H:%M:%S.%f"), endTime.strftime("%H:%M:%S.%f"), res[0])
            return res
        return logger
    return loggingOuter

# App routes
cors = CORS(app)
app.config.from_object("AppConfig.ProductionConfig")
app.config['CORS_HEADERS'] = 'Content-Type'

@app.before_first_request
def activate_job():
    if app.config['MODEL_SYNC_ENABLED']:
        productIds = getAllProductIds(resourceConfig)
        sah = StorageAccountHelper(loggerInstance)
        loggerInstance.logInsights("Starting model sync for {0}".format(','.join(productIds)))
        thread = threading.Thread(target=sah.watchModels, args=(productIds,))
        thread.start()
        while True:
            modelDownloadPending = [sah.firstTime[productId] if productId in sah.firstTime else True for productId in productIds]
            if any(modelDownloadPending):
                time.sleep(2)
            else:
                break
    loggerInstance.logInsights("Search service startup succeeded")


@app.route('/healthping')
@cross_origin()
def healthPing():
    return ("I am alive!", 200)

@app.route('/queryDetectors', methods=["POST"])
@cross_origin()
@authProvider()
@loggingProvider(requestIdRequired=True)
def queryDetectorsMethod():
    data = json.loads(request.data.decode('utf-8'))
    requestId = data['requestId']

    try:
        txt_data = translator.translate(urllib.parse.unquote(data['text'])).text
    except Exception as e:
        if 'text' in data:
            loggerInstance.logHandledException(requestId, Exception(f"Failed to translate the query -> {str(e)}. Querystring: {data['text']}"))
            txt_data = urllib.parse.unquote(data['text'])
        else:
            return ("Parameter with name 'text' was not provided in the request", 400)
    original_query = txt_data
    if (len(original_query)>250):
        return ("Query length exceeded the maximum limit of 250", 400)
    txt_data = " ".join(txt_data.split()) # remove extra whitespaces
    if (not txt_data) or len(txt_data)<2:
        return ("Minimum query length is 2", 400)
    productid = getProductId(data)
    if not productid:
        return (f'Resource not supported in search. Request data: {json.dumps(data)}', 404)
    productid = productid[0]
    try:
        loadModel(productid)
    except Exception as e:
        loggerInstance.logHandledException(requestId, e)
        return (json.dumps({"query_received": original_query, "query": txt_data, "data": data, "results": [], "exception": str(e)}), 404)
    results = loaded_models[productid].queryDetectors(txt_data)
    try:
        results["luis_results"] = getLuisPredictions(txt_data, productid)
    except Exception as e:
        results["luis_results"] = []
        results["luis_exception"] = f"LUISProviderError: {str(e)}"
    results = mergeLuisResults(results)
    logObject = results
    logObject["productId"] = productid
    logObject["modelId"] = loaded_models[productid].trainingId
    res = json.dumps(logObject)
    return (res, 200)

@app.route('/queryMultiple', methods=["POST"])
def queryMultipleMethod():
    data = json.loads(request.data.decode('utf-8'))
    requestId = data['requestId']

    txts = data['texts']
    if not txts:
        return ("No texts provided for search", 400)
    productid = getProductId(data)
    if not productid:
        return ('Resource data not available', 404)
    productid = productid[0]
    try:
        loadModel(productid)
    except Exception as e:
        loggerInstance.logHandledException(requestId, e)
        loggerInstance.logToFile(requestId, e)
        return (json.dumps({"query": txts, "results": [], "exception": str(e)}), 404)
    
    res = json.dumps([loaded_models[productid].queryDetectors(txt_data) for txt_data in txts])
    return (res, 200)

@app.route('/queryUtterances', methods=["POST"])
@cross_origin()
@authProvider()
def queryUtterancesMethod():
    data = json.loads(request.data.decode('utf-8'))
    requestId = data['requestId']
    
    txt_data = data['detector_description']
    try:
        existing_utterances = [str(x).lower() for x in json.loads(data['detector_utterances'])]
    except json.decoder.JSONDecodeError:
        existing_utterances = []
    if not txt_data:
        return ("No text provided for search", 400)
    productid = getProductId(data)
    if not productid:
        return (f'Resource type product data not available. Request data: {json.dumps(data)}', 404)
    results = {"query": txt_data, "results": []}
    for product in productid:
        try:
            loadModel(product)
            res = loaded_models[product].queryUtterances(txt_data, existing_utterances)
        except Exception as e:
            loggerInstance.logHandledException(requestId, e)
            res = {"query": txt_data, "results": None, "exception": str(e)}
        if res:
            results["results"] += res["results"] if res["results"] else []
    res = json.dumps(results)

    return (res, 200)

@app.route('/freeModel')
@cross_origin()
@authProvider()
def freeModelMethod():
    productid = str(request.args.get('productId'))
    freeModel(productid)
    return ('', 204)

@app.route('/refreshModel', methods=["GET"])
@cross_origin()
@authProvider()
@loggingProvider(requestIdRequired=True)
def refreshModelMethod():
    productid = str(request.args.get('productId')).strip()
    res = "{0} - {1}".format(productid, refreshModel(productid))
    return (res, 200)
