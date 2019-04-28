import requests, json

LOGGER_URL = "http://localhost:2743/internal/logger"

def logHandledException(requestId, exception):
	exp = { "eventType": "HandledException", "eventContent": json.dumps({ "requestId": requestId, "exceptionType": type(exception).__name__, "exceptionDetails": str(exception) }) }
	logEvent(exp)

def logUnhandledException(requestId, exception):
	exp = { "eventType": "UnhandledException", "eventContent": json.dumps({ "requestId": requestId, "exceptionType": type(exception).__name__, "exceptionDetails": str(exception) }) }
	logEvent(exp)

def logTrainingException(trainingId, productId, exception):
	exp = { "eventType": "TrainingException", "eventContent": json.dumps({ "trainingId": trainingId, "productId": productId, "exceptionType": type(exception).__name__, "exceptionDetails": str(exception) }) }
	logEvent(exp)

def logInsights(insights):
	exp = { "eventType": "Insights", "eventContent": json.dumps(insights) }
	logEvent(exp)

def logApiSummary(requestId, operationName, statusCode, latencyInMilliseconds, startTime, endTime, content):
	exp = { "eventType": "APISummary", "eventContent": json.dumps({ "requestId": requestId, "operationName": operationName, "statusCode": statusCode, "latencyInMilliseconds": latencyInMilliseconds, "startTime": startTime, "endTime": endTime, "content": content }) }
	logEvent(exp)

def logTrainingSummary(trainingId, productId, latencyInMilliseconds, startTime, endTime, content):
	exp = { "eventType": "TrainingSummary", "eventContent": json.dumps({ "trainingId": trainingId, "productId": productId, "latencyInMilliseconds": latencyInMilliseconds, "startTime": startTime, "endTime": endTime, "content": content }) }
	logEvent(exp)

def logEvent(event):
	response = requests.post(LOGGER_URL, data=json.dumps(event), headers={"Content-Type": "application/json"})