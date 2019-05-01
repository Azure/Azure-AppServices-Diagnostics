from azure.kusto.data.request import KustoClient, KustoConnectionStringBuilder, ClientRequestProperties
from azure.kusto.data.exceptions import KustoServiceError
from azure.kusto.data.helpers import dataframe_from_result_table
from RegistryReader import githubFolderPath, kustoClientId, kustoAuthority, kustoClientSecret
import re, itertools, json, requests
from TextSummarizer import retrieveSentences

class StackOverFlowFetcher:
    def __init__(self, key):
        self.key = key

    def get_Tag_Questions(self, tag, topn):
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
                print(e)
                break
        print("\n" + tag,":",pagenum,"pages,",len(items),"questions")
        return items

    def fetchStackOverflowTitles(self, productid, datapath):
        #Get tags for product id
        try:
            tagconfig = json.loads(open("metadata/trainingConfig.json", "r").read())[productid]
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
            questions = json.loads(open(datapath + "/SampleUtterances.json", "r").read())["stackoverflowtitles"]
        except:
            questions = []
        for tag in tags:
            qids = [x["qid"] for x in questions]
            questions += [q for q in self.get_Tag_Questions(tag, topn) if q["qid"] not in qids]
        try:
            sampleUtterances = json.loads(open(datapath + "/SampleUtterances.json", "r").read())
            sampleUtterances["stackoverflowtitles"] = questions
            open(datapath + "/SampleUtterances.json", "w").write(json.dumps(sampleUtterances))
        except (FileNotFoundError, ValueError):
            sampleUtterances = {"incidenttitles": [], "stackoverflowtitles": questions}
            open(datapath + "/SampleUtterances.json", "w").write(json.dumps(sampleUtterances))

class CaseTitlesFetcher:
    def __init__(self):
        cluster = "https://usage360.kusto.windows.net"
        authority_id = kustoAuthority
        client_id = kustoClientId
        client_secret = kustoClientSecret
        kcsb = KustoConnectionStringBuilder.with_aad_application_key_authentication(cluster, client_id, client_secret, authority_id)
        self.kustoClient = KustoClient(kcsb)
        self.garbageList = [x.strip() for x in open("metadata/garbagePhrases.txt", "r").readlines()]
        self.striptrailers = [x.strip() for x in open("metadata/stripTrailers.txt", "r").readlines()]
        self.shortPhrases = [x.strip() for x in open("metadata/shortPhrasesList.txt", "r").readlines()]
        self.trainingConfig = json.loads(open("metadata/trainingConfig.json", "r").read())

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
        print("/", end="")
        category = key[0]+"--"+key[1]
        numsentences = group.shape[0]
        lines = [(self.endSentence(row["CleanCaseTitles"]), row["SupportCenterCaseLink"])  for ind, row in group.iterrows()]
        doc = " ".join([x[0] for x in lines])
        keysentences = retrieveSentences(doc, max([10, int(numsentences/10)])*10)
        combined = []
        for sent in keysentences:
            caselinks = [x[1] for x in lines if self.squeeze(x[0])==self.squeeze(sent)]
            if not caselinks:
                caselinks = [x[1] for x in lines if self.squeeze(sent) in self.squeeze(x[0])]
            if not caselinks:
                caselinks = [x[1] for x in lines if re.sub('[^0-9a-zA-Z]+', '', sent)==re.sub('[^0-9a-zA-Z]+', '', x[0])]
            if caselinks:
                combined.append({"text": sent, "links": caselinks, "category": category})
        print("\r.", end="")
        return combined

    def runCaseTitlesExtraction(self, df, productid, datapath):
        df["Incidents_SupportTopicL2Current"]=df["Incidents_SupportTopicL2Current"].fillna("NOSELECTION")
        df["Incidents_SupportTopicL3Current"]=df["Incidents_SupportTopicL3Current"].fillna("NOSELECTION")
        groups = df.groupby(["Incidents_SupportTopicL2Current", "Incidents_SupportTopicL3Current"])
        print("Processing {0} case titles across {1} categories".format(df.shape[0], len(list(groups))))
        results = sorted(list(itertools.chain.from_iterable([self.extractor(key, group) for key, group in groups])), key=lambda x: x["text"])
        try:
            sampleUtterances = json.loads(open(datapath + "/SampleUtterances.json", "r").read())
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
            open(datapath + "/SampleUtterances.json", "w").write(json.dumps(sampleUtterances, indent=4))
        except (FileNotFoundError, ValueError) as e:
            open(datapath + "/SampleUtterances.json", "w").write(json.dumps({"incidenttitles" : results, "stackoverflowtitles": []}, indent=4))

    def fetchCaseTitles(self, productid, datapath):
        try:
            ndays = int(self.trainingConfig[productid]["ndays"])
        except (FileNotFoundError, KeyError, ValueError):
            ndays = 7
        db = "Product360"
        query = """cluster('usage360').database('Product360').
	    AllCloudSupportIncidentDataWithP360MetadataMapping
	    | where DerivedProductIDStr in ('{0}')
	    | where Incidents_CreatedTime >= ago({1}d)
	    | summarize IncidentTime = any(Incidents_CreatedTime) by Incidents_IncidentId , Incidents_Severity , Incidents_ProductName , Incidents_SupportTopicL2Current , Incidents_SupportTopicL3Current, Incidents_Title  
	    | extend SupportCenterCaseLink = strcat('https://azuresupportcenter.msftcloudes.com/caseoverview?srId=', Incidents_IncidentId)
	    | order by Incidents_SupportTopicL3Current asc""".format(productid, ndays)
        response = self.kustoClient.execute(db, query)
        df = dataframe_from_result_table(response.primary_results[0])
        print(df.shape[0], "incidents fetched")
    
	    #Remove all non english cases
        df["isEnglish"] = df["Incidents_Title"].map(self.isEnglish)
        df_eng = df[df["isEnglish"]==True]
        del df_eng["isEnglish"]
        print(df.shape[0] - df_eng.shape[0], "non English language cases removed")
    
        #all cases with character length 3 or less
        mask = (df_eng["Incidents_Title"].str.len()>3)
        df_eng_1 = df_eng[mask]
    
        #Extract case title from piped sentences
        df_eng_1["Incidents_Title_PipeCleansed"] = df_eng_1["Incidents_Title"].map(self.pipeCleansing)
        print("Pipes Cleansed")
    
        #Remove any content in square brackets
        df_eng_1["Incidents_Title_PipeCleansed"] = df_eng_1["Incidents_Title_PipeCleansed"].map(lambda x: re.sub("[\\[].*?[\\]]", "", x))
    
        #Remove any remaining titles with character length 3 or less
        mask = (df_eng_1["Incidents_Title_PipeCleansed"].str.len()>3)
        df_eng_2 = df_eng_1[mask]

        #Remove any garbage phrases (defined in garbage list)
        mask = (df_eng_2["Incidents_Title_PipeCleansed"].isin(self.garbageList))
        df_eng_clean = df_eng_2[~mask]
        print(df_eng.shape[0] - df_eng_clean.shape[0], "garbage case title incidents removed")
    
        #Remove any cases with two or less words (Except for short phrases that make sense)
        df_eng_clean["wordcount"] = df_eng_clean["Incidents_Title_PipeCleansed"].map(lambda x: len([a for a in x.split() if len(a)>2]))
        df_eng_clean["drop"] = df_eng_clean[["Incidents_Title_PipeCleansed", "wordcount"]].apply(lambda x: (x["Incidents_Title_PipeCleansed"] not in self.shortPhrases) and (x["wordcount"]<2), axis=1)
        df_eng_clean = df_eng_clean[df_eng_clean["drop"] == False]
        del df_eng_clean["drop"]
        del df_eng_clean["wordcount"]
    
        df_eng_clean["CleanCaseTitles"] = df_eng_clean["Incidents_Title_PipeCleansed"]
        del df_eng_clean["Incidents_Title_PipeCleansed"]
    
        print(df_eng_clean.shape[0], "incidents will be processed for summarization")
        self.runCaseTitlesExtraction(df_eng_clean, productid, datapath)

class SampleUtterancesFetcher:
    def __init__(self):
        pass

    def run(self, productid, datapath):
        caseTitlesFetcher = CaseTitlesFetcher()
        caseTitlesFetcher.fetchCaseTitles(productid, datapath)
        sfFetcher = StackOverFlowFetcher("U4DMV*8nvpm3EOpvf69Rxw((")
        sfFetcher.fetchStackOverflowTitles(productid, datapath)