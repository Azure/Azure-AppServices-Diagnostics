import nltk
from nltk import ngrams
import itertools
from nltk.corpus import stopwords
from nltk.stem import WordNetLemmatizer
import re
specialChars = r'[^0-9a-zA-Z ]+'
try:
    nltk.data.find('tokenizers/punkt')
except LookupError:
    nltk.download('punkt')
try:
    nltk.data.find('corpora/stopwords')
except LookupError:
    nltk.download('stopwords')
try:
    nltk.data.find('corpora/wordnet')
except LookupError:
    nltk.download('wordnet')

lemmatizer = WordNetLemmatizer()
stop = stopwords.words('english')

def tokenize_text(txt, lemmatize=True):
    return [lemmatizer.lemmatize(word) if lemmatize else word for word in nltk.word_tokenize(txt.lower()) if word not in stop]

def getNGrams(sentence, n, lemmatize=True):
    return [' '.join(list(x)) for x in ngrams(tokenize_text(sentence, lemmatize), n)]

def getAllNGrams(sentence, n=1, lemmatize=True):
    if not sentence:
        return []
    sentence = " ".join(re.sub(specialChars, " ", sentence).split())
    return list(itertools.chain.from_iterable([getNGrams(sentence, i, lemmatize) for i in range(1, min([n, len(sentence.split())])+1)]))