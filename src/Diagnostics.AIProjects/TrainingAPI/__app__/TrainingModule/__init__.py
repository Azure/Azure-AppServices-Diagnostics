import logging
logHandler = logging.getLogger("TrainingModule")
logHandler.setLevel(logging.DEBUG)
sh = logging.StreamHandler()
sh.setLevel(logging.ERROR)
formatter = logging.Formatter('%(asctime)s - %(name)s - %(levelname)s - %(message)s')
sh.setFormatter(formatter)
logHandler.addHandler(sh)