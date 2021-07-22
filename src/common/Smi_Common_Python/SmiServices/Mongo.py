from pymongo import MongoClient
import re

class SmiPyMongoCollection:
    """Python class to help get records from a specific collection in a MongoDB.
    Typical usage:
    mongodb = Mongo.SmiPyMongoCollection(mongo_host)
    mongodb.setImageCollection('SR')
    mongojson = mongodb.DicomFilePathToJSON('/path/file2')
    print('MONGO: %s' % mongojson)
    """

    def __init__(self, hostname, username = None, password = None):
        """ Initialise the class with the MongoDB hostname username and password """

        if username != None:
            self.mongoConnection = MongoClient(host=hostname, username=username, password=password, authSource='admin')
        else:
            self.mongoConnection = MongoClient(host=hostname)
        self.mongoCollection = None


    def setSemEHRCollection(self, collection_name):
        """ After initialisation set the desired collection using the two-letter modality, eg. SR selects dicom.image_SR """

        self.mongoCollection = self.mongoConnection['semehr'][collection_name]

    def setImageCollection(self, modality):
        """ After initialisation set the desired collection using the two-letter modality, eg. SR selects dicom.image_SR """

        self.mongoCollection = self.mongoConnection['dicom']['image_'+modality]

    def DicomFilePathToJSON(self, DicomFilePath):
        """ After setting a collection(modality) you can extract a document given a DICOM path (can be absolute, as everything up to PACS stripped off, or relative to root of collection)"""

        # strip off any full path prefix so it starts with the year
        DicomFilePath = re.sub('^.*PACS/', '', DicomFilePath)

        return self.mongoCollection.find_one( { "header.DicomFilePath": DicomFilePath } )

    def StudyDateToJSONList(self, StudyDate):
        """ After setting a collection(modality) you can extract a list of documents for a given date in the form YYYY/MM/DD.
        Actually it returns a Mongo Cursor generator. """

        # Remove all spaces and slashes becaise StudyDate is always in YYYYMMDD format
        StudyDate = re.sub('[/ ]*', '', StudyDate)
        assert(len(StudyDate) == 8)

        return self.mongoCollection.find( { "StudyDate" : StudyDate } )

    def findSOPInstanceUID(self, sopinstanceuid):
        """ This is intended to check for the existence of a document having the
        given SOPInstanceUID but since it also returns that document is can be
        used as a query """
        
        return self.mongoCollection.find_one( { 'SOPInstanceUID': sopinstanceuid } )
