import os, json, itertools, nltk
from nltk.corpus import stopwords
from gensim.models import TfidfModel
from nltk.stem.porter import *
from gensim import corpora, similarities
from azure.storage.blob import BlockBlobService
from DataProcessor import DataProcessor

credentials = json.loads(open("credentials.json", "r").read())
STORAGE_ACCOUNT_NAME = credentials["STORAGE_ACCOUNT_NAME"]
STORAGE_ACCOUNT_KEY = credentials["STORAGE_ACCOUNT_KEY"]
blob_service = BlockBlobService(account_name=STORAGE_ACCOUNT_NAME, account_key=STORAGE_ACCOUNT_KEY)

def downloadTrainingData():
    container_name = 'trainingdata'
    for blobname in list(map(lambda x: x.name, list(blob_service.list_blobs(container_name)))):
        blob_service.get_blob_to_path(container_name, blobname, blobname)

def uploadModels(productid, outpath):
    container_name = "modelpackages"
    for file in os.listdir(outpath):
        blob_service.create_blob_from_path(container_name, "{0}/{1}".format(productid, file), outpath + "/{0}".format(file))

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
    dictionary.save(outpath + "/dictionary.dict")
    
def trainModelM1(tests, detector_tokens, sampleUtterances_tokens, productid, outpath):
    dictionary = corpora.Dictionary.load(outpath + "/dictionary.dict")
    corpus = [dictionary.doc2bow(line) for line in detector_tokens]
    model = TfidfModel(corpus)
    index = similarities.MatrixSimilarity(model[corpus])
    for test in tests:
        if not testModelForSearch(model, dictionary, index, test):
            return
    model.save(outpath + "/m1.model")
    index.save(outpath + "/m1.index")

def trainModelM2(tests, detector_tokens, sampleUtterances_tokens, productid, outpath):
    dictionary = corpora.Dictionary.load(outpath + "/dictionary.dict")
    corpus = [dictionary.doc2bow(line) for line in sampleUtterances_tokens]
    model = TfidfModel(corpus)
    index = similarities.MatrixSimilarity(model[corpus])
    for test in tests:
        if not testModelForSearch(model, dictionary, index, test):
            return
    model.save(outpath + "/m2.model")
    index.save(outpath + "/m2.index")

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
    outpath = "modelpackages_{0}".format(productid)
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
    detectorsdata = open(datapath + "/Detectors.json", "r").read()
    detectors = json.loads(detectorsdata)
    detector_tokens = [tokenize_text(x["name"] + " " + x["description"] + " " + " ".join([y["text"] for y in x["utterances"]])) for x in detectors]

    #Stackoverflow and Case Incidents data load
    sampleUtterancesContent = json.loads(open(datapath + "/SampleUtterances.json", "r").read())
    sampleUtterances = (sampleUtterancesContent["incidenttitles"] if trainingConfig["include-casetitles"] else []) + (sampleUtterancesContent["stackoverflowtitles"] if trainingConfig["include-softitles"] else [])
    sampleUtterances_tokens = [tokenize_text(sampleUtterances[i]["text"]) for i in range(len(sampleUtterances))]

    trainDictionary(detector_tokens + sampleUtterances_tokens, productid, outpath)
    trainModelM1([], detector_tokens, sampleUtterances_tokens, productid, outpath)
    trainModelM2([], detector_tokens, sampleUtterances_tokens, productid, outpath)
    open(outpath + "/Detectors.json", "w").write(json.dumps(detectors))
    open(outpath + "/SampleUtterances.json", "w").write(json.dumps(sampleUtterances))
    #uploadModels(productid, outpath)

trainModels("14748")
import requests
print(requests.get("http://localhost:8010/refreshModel?productid=14748").content)