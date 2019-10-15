import win32api
import win32con
import win32evtlog
import win32security
import win32evtlogutil
import pythoncom
pythoncom.CoInitialize()

processHandle = win32api.GetCurrentProcess()
tokenHandle = win32security.OpenProcessToken(processHandle, win32con.TOKEN_READ)
token_sid = win32security.GetTokenInformation(tokenHandle, win32security.TokenUser)[0]

eventCategories = {
    "Error": 1,
    "Warning": 2,
    "Info": 4
}

applicationName = "diag-searchservice-prod"
def log(eventID, category, logdesc, data):
    eventType = 4
    if category in eventCategories:
        eventType = eventCategories[category]
    win32evtlogutil.ReportEvent(applicationName, eventID, eventCategory=eventType, eventType=eventType, strings=logdesc, data=data.encode('ascii'), sid=token_sid)