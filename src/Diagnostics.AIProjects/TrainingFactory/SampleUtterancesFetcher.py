from azure.kusto.data.request import KustoClient, KustoConnectionStringBuilder, ClientRequestProperties
from azure.kusto.data.exceptions import KustoServiceError
from azure.kusto.data.helpers import dataframe_from_result_table
from RegistryReader import githubFolderPath, kustoClientId, kustoClientSecret
import re, itertools, json, requests, os
from TextSummarizer import retrieveSentences
from Logger import *

class TrainingException(Exception):
    pass

class StackOverFlowFetcher:
    def __init__(self, key, trainingConfig, trainingId):
        self.key = key
        self.trainingConfig = trainingConfig
        self.trainingId = trainingId

    def get_Tag_Questions(self, tag, topn):
        trainingId = self.trainingId
        fetchmore = True
        pagenum = 1
        items = []
        while True:
            try:
                url = "http://api.stackexchange.com/2.2/questions?key={0}((&site=stackoverflow&page={1}&order=desc&sort=votes&tagged={2}&filter=default".format(self.key, pagenum, tag)
                content = requests.get(url=url).json()
                items += [{"text": x["title"], "links": [x["link"]], "qid": x["question_id"]} for x in content["items"] if (x["score"]>0 or x["answer_count"]>0)]
                #print(txt)
                if len(items)>topn:
                    break
                if content["has_more"] == "false" or not content["has_more"]:
                    break
                pagenum += 1
                #print("\r" + str(pagenum),end='')
            except Exception as e:
                logToFile("{0}.log".format(trainingId), "[ERROR]StackOverFlowFetcher: Tag " + str(tag) + " - " + str(e))
                raise TrainingException("StackOverFlowFetcher: " + str(e))
        logToFile("{0}.log".format(trainingId), "StackOverFlowFetcher: Fetched " + str(len(items)) + " questions for tag " + str(tag))
        return items

    def fetchStackOverflowTitles(self, productid, datapath):
        trainingId = self.trainingId
        #Get tags for product id
        try:
            tagconfig = self.trainingConfig
            print("TAG DOWNLOAD SET TO --", tagconfig["download-softitles"])
            if not tagconfig["download-softitles"]:
                return
            tags = tagconfig["sof-tags"]
            topn = tagconfig["topn-sof"]
        except (FileNotFoundError, ValueError, KeyError):
            tags = []
            topn = 200
        #Fetch questions for tags
        try:
            questions = json.loads(open(os.path.join(datapath, "SampleUtterances.json"), "r").read())["stackoverflowtitles"]
        except:
            questions = []
        for tag in tags:
            qids = [x["qid"] for x in questions]
            questions += [q for q in self.get_Tag_Questions(tag, topn) if q["qid"] not in qids]
        try:
            sampleUtterances = json.loads(open(os.path.join(datapath, "SampleUtterances.json"), "r").read())
            sampleUtterances["stackoverflowtitles"] = questions
            open(os.path.join(datapath, "SampleUtterances.json"), "w").write(json.dumps(sampleUtterances))
            logToFile("{0}.log".format(trainingId), "StackOverFlowFetcher: Successfully written stackoverflow questions to file SampleUtterances.json")
        except (FileNotFoundError):
            logToFile("{0}.log".format(trainingId), "[ERROR]StackOverFlowFetcher: File SampleUtterances.json does not exist, creating new file.")
            sampleUtterances = {"incidenttitles": [], "stackoverflowtitles": questions}
            open(os.path.join(datapath, "SampleUtterances.json"), "w").write(json.dumps(sampleUtterances))

class CaseTitlesFetcher:
    def __init__(self, trainingConfig, trainingId):
        self.trainingId = trainingId
        cluster = "https://usage360.kusto.windows.net"
        authority_id = "72f988bf-86f1-41af-91ab-2d7cd011db47"
        client_id = kustoClientId
        client_secret = kustoClientSecret
        kcsb = KustoConnectionStringBuilder.with_aad_application_key_authentication(cluster, client_id, client_secret, authority_id)
        self.kustoClient = KustoClient(kcsb)
        self.garbageList = [x.strip() for x in open("metadata/garbagePhrases.txt", "r").readlines()]
        self.striptrailers = [x.strip() for x in open("metadata/stripTrailers.txt", "r").readlines()]
        self.shortPhrases = [x.strip() for x in open("metadata/shortPhrasesList.txt", "r").readlines()]
        self.trainingConfig = trainingConfig

    def endSentence(self, sent):
        if not sent[-1]==".":
            return sent+"."
        return sent

    def squeeze(self, sent):
        while sent[-1]==".":
            sent = sent[:-1]
        return sent.replace(" ", "")

    def isEnglish(self, s):
        s = str(s)
        try:
            s.encode(encoding='utf-8').decode('ascii')
        except UnicodeDecodeError:
            return False
        else:
            return True

    def stripTrails(self, s):
        s = str(s).lower().strip()
        for tr in self.striptrailers:
            if s.endswith(tr.lower()):
                return self.stripTrails(s[:-len(tr)])
        return s.strip()

    def pipeCleansing(self, s):
        s = self.stripTrails(s)
        l = s.split("|")
        if len(l)>1:
            return l[-1].lstrip()
        else:
            return s.lstrip()

    def extractor(self, key, group):
        trainingId = self.trainingId
        category = key[0]+"--"+key[1]
        numsentences = group.shape[0]
        logToFile("{0}.log".format(trainingId), "Extractor: Running extractor on category " + category + " containing " + str(numsentences) + " case titles")
        lines = [(self.endSentence(row["CleanCaseTitles"]), row["SupportCenterCaseLink"])  for ind, row in group.iterrows()]
        doc = " ".join([x[0] for x in lines])
        keysentences = retrieveSentences(doc, max([10, int(numsentences/10)])*10)
        logToFile("{0}.log".format(trainingId), "Extractor: Extracted " + str(len(keysentences)) + " sentences.")
        combined = []
        for sent in keysentences:
            caselinks = [x[1] for x in lines if self.squeeze(x[0])==self.squeeze(sent)]
            if not caselinks:
                caselinks = [x[1] for x in lines if self.squeeze(sent) in self.squeeze(x[0])]
            if not caselinks:
                caselinks = [x[1] for x in lines if re.sub('[^0-9a-zA-Z]+', '', sent)==re.sub('[^0-9a-zA-Z]+', '', x[0])]
            if caselinks:
                combined.append({"text": sent, "links": caselinks, "category": category})
        return combined

    def runCaseTitlesExtraction(self, df, productid, datapath):
        trainingId = self.trainingId
        df["Incidents_SupportTopicL2Current"]=df["Incidents_SupportTopicL2Current"].fillna("NOSELECTION")
        df["Incidents_SupportTopicL3Current"]=df["Incidents_SupportTopicL3Current"].fillna("NOSELECTION")
        groups = df.groupby(["Incidents_SupportTopicL2Current", "Incidents_SupportTopicL3Current"])
        logToFile("{0}.log".format(trainingId), "RunCaseTitlesExtraction: Processing " + str(df.shape[0]) + " case titles across " + str(len(list(groups))) + " categories")
        results = sorted(list(itertools.chain.from_iterable([self.extractor(key, group) for key, group in groups])), key=lambda x: x["text"])
        try:
            sampleUtterances = json.loads(open(os.path.join(datapath, "SampleUtterances.json"), "r").read())
            #sampleUtterances = list(set(sampleUtterances+results))
            for x in results:
                found = False
                for y in sampleUtterances["incidenttitles"]:
                    if x["text"]<y["text"]:
                        break
                    elif x["text"]==y["text"] and x["category"]==y["category"]:
                        y["links"] += x["links"]
                        y["links"] = list(set(y["links"]))
                        found = True
                        break
                if not found:
                    sampleUtterances["incidenttitles"].append(x)
            open(os.path.join(datapath, "SampleUtterances.json"), "w").write(json.dumps(sampleUtterances, indent=4))
            logToFile("{0}.log".format(trainingId), "RunCaseTitlesExtraction: Successfully written extracted case titles to file SampleUtterances.json")
        except (FileNotFoundError) as e:
            logToFile("{0}.log".format(trainingId), "[ERROR]RunCaseTitlesExtraction: File SampleUtterances.json does not exist, creating new file.")
            open(os.path.join(datapath, "SampleUtterances.json"), "w").write(json.dumps({"incidenttitles" : results, "stackoverflowtitles": []}, indent=4))

    def fetchCaseTitles(self, productid, datapath):
        trainingId = self.trainingId
        try:
            ndays = int(self.trainingConfig["ndays"])
        except (FileNotFoundError, KeyError, ValueError):
            ndays = 7
        try:
            db = "Product360"
            query = """cluster('usage360').database('Product360').
	       AllCloudSupportIncidentDataWithP360MetadataMapping
	       | where DerivedProductIDStr in ('{0}')
	       | where Incidents_CreatedTime >= ago({1}d)
	       | summarize IncidentTime = any(Incidents_CreatedTime) by Incidents_IncidentId , Incidents_Severity , Incidents_ProductName , Incidents_SupportTopicL2Current , Incidents_SupportTopicL3Current, Incidents_Title  
	       | extend SupportCenterCaseLink = strcat('https://azuresupportcenter.msftcloudes.com/caseoverview?srId=', Incidents_IncidentId)
	       | order by Incidents_SupportTopicL3Current asc""".format(productid, ndays)
            response = self.kustoClient.execute(db, query)
        except Exception as e:
            raise TrainingException("KustoFetcher: " + str(e))
        
        try:
            df = dataframe_from_result_table(response.primary_results[0])
            logToFile("{0}.log".format(trainingId), "DataCleansing: " + str(df.shape[0]) + " incidents fetched")
        
    	    #Remove all non english cases
            df["isEnglish"] = df["Incidents_Title"].map(self.isEnglish)
            df_eng = df[df["isEnglish"]==True]
            del df_eng["isEnglish"]
            logToFile("{0}.log".format(trainingId), "DataCleansing: " + str(df.shape[0] - df_eng.shape[0]) + " non English language cases removed")
        
            #all cases with character length 3 or less
            mask = (df_eng["Incidents_Title"].str.len()>3)
            df_eng_1 = df_eng[mask]
        
            #Extract case title from piped sentences
            df_eng_1["Incidents_Title_PipeCleansed"] = df_eng_1["Incidents_Title"].map(self.pipeCleansing)
            
            #Remove any content in square brackets
            df_eng_1["Incidents_Title_PipeCleansed"] = df_eng_1["Incidents_Title_PipeCleansed"].map(lambda x: re.sub("[\\[].*?[\\]]", "", x))
        
            #Remove any remaining titles with character length 3 or less
            mask = (df_eng_1["Incidents_Title_PipeCleansed"].str.len()>3)
            df_eng_2 = df_eng_1[mask]

            #Remove any garbage phrases (defined in garbage list)
            mask = (df_eng_2["Incidents_Title_PipeCleansed"].isin(self.garbageList))
            df_eng_clean = df_eng_2[~mask]
            logToFile("{0}.log".format(trainingId), "DataCleansing: " + str(df_eng.shape[0] - df_eng_clean.shape[0]) + " garbage case title incidents removed")
            
            #Remove any cases with two or less words (Except for short phrases that make sense)
            df_eng_clean["wordcount"] = df_eng_clean["Incidents_Title_PipeCleansed"].map(lambda x: len([a for a in x.split() if len(a)>2]))
            df_eng_clean["drop"] = df_eng_clean[["Incidents_Title_PipeCleansed", "wordcount"]].apply(lambda x: (x["Incidents_Title_PipeCleansed"] not in self.shortPhrases) and (x["wordcount"]<2), axis=1)
            df_eng_clean = df_eng_clean[df_eng_clean["drop"] == False]
            del df_eng_clean["drop"]
            del df_eng_clean["wordcount"]
        
            df_eng_clean["CleanCaseTitles"] = df_eng_clean["Incidents_Title_PipeCleansed"]
            del df_eng_clean["Incidents_Title_PipeCleansed"]
            logToFile("{0}.log".format(trainingId), "DataCleansing: " + str(df_eng_clean.shape[0]) + " incidents will be processed for summarization")
        except Exception as e:
            raise TrainingException("DataCleansing: " + str(e))
        try:
            self.runCaseTitlesExtraction(df_eng_clean, productid, datapath)
        except Exception as e:
            raise TrainingException("CaseTitleExtraction: " + str(e))

class SampleUtterancesFetcher:
    def __init__(self, trainingConfig, trainingId):
        self.trainingConfig = trainingConfig
        self.trainingId = trainingId
        pass

    def run(self, productid, datapath):
        trainingId = self.trainingId
        try:
            caseTitlesFetcher = CaseTitlesFetcher(self.trainingConfig, self.trainingId)
            caseTitlesFetcher.fetchCaseTitles(productid, datapath)
            logToFile("{0}.log".format(trainingId), "CaseTitlesFetcher: Successfully fetched & extracted case titles from Kusto")
        except Exception as e:
            logToFile("{0}.log".format(trainingId), "[ERROR]CaseTitlesFetcher: " + str(e))
            raise TrainingException("CaseTitlesFetcher: " + str(e))
        try:
            if self.trainingConfig["download-softitles"] and self.trainingConfig["sof-key"]:
                sfFetcher = StackOverFlowFetcher(self.trainingConfig["sof-key"], self.trainingConfig, self.trainingId)
                sfFetcher.fetchStackOverflowTitles(productid, datapath)
                logToFile("{0}.log".format(trainingId), "StackOverFlowFetcher: Successfully fetched stack overflow question titles")
            else:
                logToFile("{0}.log".format(trainingId), "StackOverFlowFetcher: Disabled")
        except Exception as e:
            logToFile("{0}.log".format(trainingId), "[ERROR]StackOverFlowFetcher: " + str(e))
            raise TrainingException("StackOverFlowFetcher: " + str(e))