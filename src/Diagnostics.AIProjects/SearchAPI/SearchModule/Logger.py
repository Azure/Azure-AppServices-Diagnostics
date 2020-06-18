import requests, json, os
from SearchModule.ETWProvider import log

class LoggingException(Exception):
    pass

class Logger:
	def __init__(self, kustoEnabled=False):
		self.isLogToKustoEnabled = kustoEnabled
		self.eventCategoryMapping = {
			"UnhandledException": {"eventId": 4001, "category": "Error"},
			"APISummary": {"eventId": 4002, "category": "Information"},
			"Insights": {"eventId": 4005, "category": "Information"},
			"HandledException": {"eventId": 4006, "category": "Error"}
		}
	
	def logHandledException(self, requestId, exception):
		exp = { "eventType": "HandledException", "eventContent": json.dumps({ "requestId": requestId, "exceptionType": type(exception).__name__, "exceptionDetails": str(exception) }) }
		self.logEvent(exp)

	def logUnhandledException(self, requestId, exception):
		exp = { "eventType": "UnhandledException", "eventContent": json.dumps({ "requestId": requestId, "exceptionType": type(exception).__name__, "exceptionDetails": str(exception) }) }
		self.logEvent(exp)

	def logInsights(self, insights):
		exp = { "eventType": "Insights", "eventContent": insights }
		self.logEvent(exp)

	def logApiSummary(self, requestId, operationName, statusCode, latencyInMilliseconds, startTime, endTime, content):
		exp = { "eventType": "APISummary", "eventContent": json.dumps({ "requestId": requestId, "operationName": operationName, "statusCode": statusCode, "latencyInMilliseconds": latencyInMilliseconds, "startTime": startTime, "endTime": endTime, "content": content }) }
		self.logEvent(exp)

	def logEvent(self, event):
		if self.isLogToKustoEnabled:
			log(self.eventCategoryMapping[event["eventType"]]["eventId"], self.eventCategoryMapping[event["eventType"]]["category"], [event["eventContent"]], "")
loggerInstance = Logger(kustoEnabled=True)