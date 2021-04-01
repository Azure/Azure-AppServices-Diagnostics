import os

def cleanFolder(folderPath):
    for f in os.listdir(folderPath):
        os.remove(os.path.join(folderPath, f))

def compareDetectorSets(set1, set2):
    if set1 and set2:
        set1 = sorted([{k:x[k] for k in ["id", "name", "description", "utterances"]} for x in set1], key=lambda p: p["id"])
        set2 = sorted([{k:x[k] for k in ["id", "name", "description", "utterances"]} for x in set2], key=lambda p: p["id"])
        return set1 == set2
    else:
        return False