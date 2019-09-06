import requests, json

class LoggingException(Exception):
    pass

def logToFile(fileName, logvalue):
	try:
		fp = open(fileName, "a")
		fp.write(logvalue + "\r\n")
		fp.close()
	except Exception as e:
		pass

def logHandledException(requestId, exception):
	exp = { "eventType": "HandledException", "eventContent": json.dumps({ "requestId": requestId, "exceptionType": type(exception).__name__, "exceptionDetails": str(exception) }) }
	logEvent(exp)

def logUnhandledException(requestId, exception):
	exp = { "eventType": "UnhandledException", "eventContent": json.dumps({ "requestId": requestId, "exceptionType": type(exception).__name__, "exceptionDetails": str(exception) }) }
	logEvent(exp)

def logTrainingException(requestId, trainingId, productId, exception):
	exp = { "eventType": "TrainingException", "eventContent": json.dumps({ "requestId": requestId, "trainingId": trainingId, "productId": productId, "exceptionType": type(exception).__name__, "exceptionDetails": str(exception) }) }
	logEvent(exp)

def logInsights(insights):
	exp = { "eventType": "Insights", "eventContent": json.dumps(insights) }
	logEvent(exp)

def logApiSummary(requestId, operationName, statusCode, latencyInMilliseconds, startTime, endTime, content):
	exp = { "eventType": "APISummary", "eventContent": json.dumps({ "requestId": requestId, "operationName": operationName, "statusCode": statusCode, "latencyInMilliseconds": latencyInMilliseconds, "startTime": startTime, "endTime": endTime, "content": content }) }
	logEvent(exp)

def logTrainingSummary(requestId, trainingId, productId, latencyInMilliseconds, startTime, endTime, content):
	exp = { "eventType": "TrainingSummary", "eventContent": json.dumps({ "requestId": requestId, "trainingId": trainingId, "productId": productId, "latencyInMilliseconds": latencyInMilliseconds, "startTime": startTime, "endTime": endTime, "content": content }) }
	logEvent(exp)

def logEvent(event):
	config = json.loads(open("resourceConfig/config.json", "r").read())
	LOGGER_URL = "http://localhost:{0}/internal/logger".format(config["internalApiPort"])
	response = requests.post(LOGGER_URL, data=json.dumps(event), headers={"Content-Type": "application/json"})