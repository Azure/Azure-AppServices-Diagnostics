import nltk
from nltk import ngrams
import itertools
from nltk.corpus import stopwords
from nltk.stem import WordNetLemmatizer

try:
    nltk.data.find('tokenizers/punkt')
except LookupError:
    nltk.download('punkt')
try:
    nltk.data.find('stopwords')
except LookupError:
    nltk.download('stopwords')

lemmatizer = WordNetLemmatizer()
stop = stopwords.words('english')

def tokenize_text(txt):
    return [lemmatizer.lemmatize(word) for word in nltk.word_tokenize(txt.lower()) if word not in stop]

def getNGrams(sentence, n):
    return [' '.join(list(x)) for x in ngrams(tokenize_text(sentence), n)]

def getAllNGrams(sentence, n=1):
    return list(itertools.chain.from_iterable([getNGrams(sentence, i) for i in range(1, min([n, len(sentence.split())])+1)]))