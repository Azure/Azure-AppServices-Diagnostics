class ModelDownloadFailed(Exception):
    pass
class ModelFileConfigFailed(Exception):
    pass
class ModelFileVerificationFailed(Exception):
    pass
class ModelFileLoadFailed(Exception):
    pass
class ResourceConfigDownloadFailed(Exception):
    pass
class ModelRefreshException(Exception):
    pass
class CopySourceFolderNotFoundException(Exception):
    pass
class CopyTaskException(Exception):
    pass