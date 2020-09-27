import logging
class TestCase:
    def __init__(self, query, expectedResults):
        self.query = query
        self.failDetails = []
        self.isPassed = False
        self.results = None
        if expectedResults:
            self.expectedResults = [res.lower() for res in expectedResults]
        else:
            raise Exception("Please provide at least one expected result.")
    
    def run(self, model, threshold=0.5):
        results = [res["detector"].lower() for res in model.queryDetectors(self.query)["results"] if float(res["score"])>=0.3]
        self.results = results
        numpassed = 0
        for result in self.expectedResults:
            if result in results:
                numpassed += 1
            else:
                self.failDetails.append(result)
        if numpassed/len(self.expectedResults)>=threshold:
            self.isPassed = True
            logging.info(f"Test case passed!")
        return