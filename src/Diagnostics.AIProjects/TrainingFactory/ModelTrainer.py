import os, json, itertools, nltk, requests
from nltk.corpus import stopwords
from gensim.models import TfidfModel
from nltk.stem.porter import *
from gensim import corpora, similarities
from DataProcessor import DataProcessor
config = json.loads(open("metadata/config.json", "r").read())
try:
    nltk.data.find('tokenizers/punkt')
except LookupError:
    nltk.download('punkt')
try:
    nltk.data.find('stopwords')
except LookupError:
    nltk.download('stopwords')

stemmer = PorterStemmer()
stop = stopwords.words('english')

def tokenize_text(txt):
    return [stemmer.stem(word) for word in nltk.word_tokenize(txt.lower()) if word not in stop]

def testModelForSearch(model, dictionary, index, query):
    try:
        vector = model[dictionary.doc2bow(tokenize_text(query))]
        similar_doc_indices = sorted(enumerate(index[vector]), key=lambda item: -item[1])
        if similar_doc_indices[0][1]>0:
            return True
        return False
    except Exception as e:
        print(e)
        return False

def trainDictionary(alltokens, productid, outpath):
    dictionary = corpora.Dictionary(alltokens)
    dictionary.save(os.path.join(outpath, "dictionary.dict"))
    
def trainModelM1(tests, detector_tokens, sampleUtterances_tokens, productid, outpath):
    dictionary = corpora.Dictionary.load(os.path.join(outpath, "dictionary.dict"))
    corpus = [dictionary.doc2bow(line) for line in detector_tokens]
    model = TfidfModel(corpus)
    index = similarities.MatrixSimilarity(model[corpus])
    for test in tests:
        if not testModelForSearch(model, dictionary, index, test):
            return
    model.save(os.path.join(outpath, "m1.model"))
    index.save(os.path.join(outpath, "m1.index"))

def trainModelM2(tests, detector_tokens, sampleUtterances_tokens, productid, outpath):
    dictionary = corpora.Dictionary.load(os.path.join(outpath, "dictionary.dict"))
    corpus = [dictionary.doc2bow(line) for line in sampleUtterances_tokens]
    model = TfidfModel(corpus)
    index = similarities.MatrixSimilarity(model[corpus])
    for test in tests:
        if not testModelForSearch(model, dictionary, index, test):
            return
    model.save(os.path.join(outpath, "m2.model"))
    index.save(os.path.join(outpath, "m2.index"))

def publishModels(productid, modelPath):
    publishUrl = "http://localhost:{0}/internal/publishmodel".format(config["internalApiPort"])
    requests.post(publishUrl, data=json.dumps(modelPath), headers={"Content-Type": "application/json"})

def trainModels(productid):
    try:
        trainingConfig = json.loads(open("metadata/trainingConfig.json", "r").read())[productid]
    except (FileNotFoundError, KeyError, ValueError):
        trainingConfig = {
            "include-casetitles": True,
            "include-softitles": True,
            "ndays": 7
        }
    datapath = "rawdata_{0}".format(productid)
    outpath = "{0}".format(productid)
    try:
        os.mkdir(datapath)
    except FileExistsError:
        pass
    try:
        os.mkdir(outpath)
    except FileExistsError:
        pass
    dataProcessor = DataProcessor()
    dataProcessor.prepareDataForTraining(productid)
    detectorsdata = open(os.path.join(datapath, "Detectors.json"), "r").read()
    detectors = json.loads(detectorsdata)
    detector_tokens = [tokenize_text(x["name"] + " " + x["description"] + " " + " ".join([y["text"] for y in x["utterances"]])) for x in detectors]

    #Stackoverflow and Case Incidents data load
    sampleUtterancesContent = json.loads(open(os.path.join(datapath, "SampleUtterances.json"), "r").read())
    sampleUtterances = (sampleUtterancesContent["incidenttitles"] if trainingConfig["include-casetitles"] else []) + (sampleUtterancesContent["stackoverflowtitles"] if trainingConfig["include-softitles"] else [])
    sampleUtterances_tokens = [tokenize_text(sampleUtterances[i]["text"]) for i in range(len(sampleUtterances))]

    trainDictionary(detector_tokens + sampleUtterances_tokens, productid, outpath)
    trainModelM1([], detector_tokens, sampleUtterances_tokens, productid, outpath)
    trainModelM2([], detector_tokens, sampleUtterances_tokens, productid, outpath)
    open(os.path.join(outpath, "Detectors.json"), "w").write(json.dumps(detectors))
    open(os.path.join(outpath, "SampleUtterances.json"), "w").write(json.dumps(sampleUtterances))
    modelPath = os.path.join(os.path.dirname(os.path.abspath(__file__)), outpath)
    publishModels(productid, modelPath)

trainModels("14748")
#import requests
#print(requests.get("http://localhost:8010/refreshModel?productid=14748").content)