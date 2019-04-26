import requests, json

LOGGER_URL = "http://localhost:2743/internal/logger"

def getException(requestId, exception):
	return { "requestId": requestId, "exceptionType": type(exception).__name__, "exceptionDetails": str(exception) }

def getTrainingException(trainingId, productId, exception):
	return { "trainingId": trainingId, "productId": productId, "exceptionType": type(exception).__name__, "exceptionDetails": str(exception) }

def getInsights(requestId, message):
	return { "requestId": requestId, "message": message }

def getApiSummary(requestId, operationName, statusCode, latencyInMilliseconds, startTime, endTime, content):
	return { "requestId": requestId, "operationName": operationName, "statusCode": statusCode, "latencyInMilliseconds": latencyInMilliseconds, "startTime": startTime, "endTime": endTime, "content": content }

def getTrainingSummary(trainingId, productId, latencyInMilliseconds, startTime, endTime, content):
	return { "trainingId": trainingId, "productId": productId, "latencyInMilliseconds": latencyInMilliseconds, "startTime": startTime, "endTime": endTime, "content": content }

def logHandledException(exception):
	exp = { "eventType": "HandledException", "eventContent": json.dumps(exception) }
	logEvent(exp)

def logUnhandledException(exception):
	exp = { "eventType": "UnhandledException", "eventContent": json.dumps(exception) }
	logEvent(exp)

def logTrainingException(exception):
	exp = { "eventType": "TrainingException", "eventContent": json.dumps(exception) }
	logEvent(exp)

def logInsights(insights):
	exp = { "eventType": "Insights", "eventContent": json.dumps(insights) }
	logEvent(exp)

def logApiSummary(apiSummary):
	exp = { "eventType": "APISummary", "eventContent": json.dumps(apiSummary) }
	logEvent(exp)

def logTrainingSummary(trainingSummary):
	exp = { "eventType": "TrainingSummary", "eventContent": json.dumps(trainingSummary) }
	logEvent(exp)

def logEvent(event):
	response = requests.post(LOGGER_URL, data=json.dumps(event), headers={"Content-Type": "application/json"})