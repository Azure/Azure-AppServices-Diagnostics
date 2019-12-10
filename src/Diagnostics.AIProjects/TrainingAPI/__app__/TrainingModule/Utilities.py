import os

def cleanFolder(folderPath):
    for f in os.listdir(folderPath):
        os.remove(os.path.join(folderPath, f))