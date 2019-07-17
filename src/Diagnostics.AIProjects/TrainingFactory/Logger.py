import requests, json

class LoggingException(Exception):
    pass

class Logger:
	def __init__(self, kustoEnabled=True):
		self.isLogToKustoEnabled = kustoEnabled

	def logToFile(self, fileName, logvalue):
		print(logvalue)
		try:
			fp = open(fileName, "a")
			fp.write(logvalue + "\r\n")
			fp.close()
		except Exception as e:
			pass
		
	def logHandledException(self, requestId, exception):
		exp = { "eventType": "HandledException", "eventContent": json.dumps({ "requestId": requestId, "exceptionType": type(exception).__name__, "exceptionDetails": str(exception) }) }
		self.logEvent(exp)

	def logUnhandledException(self, requestId, exception):
		exp = { "eventType": "UnhandledException", "eventContent": json.dumps({ "requestId": requestId, "exceptionType": type(exception).__name__, "exceptionDetails": str(exception) }) }
		self.logEvent(exp)

	def logTrainingException(self, requestId, trainingId, productId, exception):
		exp = { "eventType": "TrainingException", "eventContent": json.dumps({ "requestId": requestId, "trainingId": trainingId, "productId": productId, "exceptionType": type(exception).__name__, "exceptionDetails": str(exception) }) }
		self.logEvent(exp)

	def logInsights(self, insights):
		exp = { "eventType": "Insights", "eventContent": json.dumps(insights) }
		self.logEvent(exp)

	def logApiSummary(self, requestId, operationName, statusCode, latencyInMilliseconds, startTime, endTime, content):
		exp = { "eventType": "APISummary", "eventContent": json.dumps({ "requestId": requestId, "operationName": operationName, "statusCode": statusCode, "latencyInMilliseconds": latencyInMilliseconds, "startTime": startTime, "endTime": endTime, "content": content }) }
		self.logEvent(exp)

	def logTrainingSummary(self, requestId, trainingId, productId, latencyInMilliseconds, startTime, endTime, content):
		exp = { "eventType": "TrainingSummary", "eventContent": json.dumps({ "requestId": requestId, "trainingId": trainingId, "productId": productId, "latencyInMilliseconds": latencyInMilliseconds, "startTime": startTime, "endTime": endTime, "content": content }) }
		self.logEvent(exp)

	def logEvent(self, event):
		if self.isLogToKustoEnabled:
			config = json.loads(open("resourceConfig/config.json", "r").read())
			LOGGER_URL = "http://localhost:{0}/internal/logger".format(config["internalApiPort"])
			response = requests.post(LOGGER_URL, data=json.dumps(event), headers={"Content-Type": "application/json"})

loggerInstance = Logger()