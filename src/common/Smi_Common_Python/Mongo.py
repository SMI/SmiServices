from pymongo import MongoClient
import re

class SmiPyMongoCollection:
  """Python class to help get records from a specific collection in a MongoDB"""

  def __init__(self, hostname, username = None, password = None):
    """Initialise the class with the MongoDB hostname username and password"""

    if username != None:
      self.mongoConnection = MongoClient(host=hostname, username=username, password=password, authSource='admin')
    else:
      self.mongoConnection = MongoClient(host=hostname)


  def setImageCollection(self, modality):
    """After initialisation set the desired collection using the two-letter modality, eg. SR selects dicom.image_SR"""

    self.mongoCollection = self.mongoConnection['dicom']['image_'+modality]


  def DicomFilePathToJSON(self, DicomFilePath):
    """After setting a collection(modality) you can extract a document given a DICOM path (can be absolute, as everything upto PACS stripped off, or relative to root of collection)"""

    # strip off any full path prefix so it starts with the year
    DicomFilePath = re.sub('^.*PACS/', '', DicomFilePath)

    return self.mongoCollection.find_one( { "header.DicomFilePath": DicomFilePath } )
