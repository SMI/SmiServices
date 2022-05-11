#!/usr/bin/env python3

# Class to map from CHI number to EUPI.
# The class keeps a connection open to MySQL for speed.

import mysql.connector
import os, re, sys, yaml

class CHItoEUPI():

    """ This class can be initialised by creating an instance
    and passing it the yaml dict for configuration and then that
    instance can be deleted. The database connection is maintained
    at the class level so future instances can reuse it.
    """

    # Class members
    db_connection = None
    db_cursor = None
    dbMappingsStr = ''
    sql_command = ''

    def __init__(self, yaml_dict = None):
        """ Requires a yaml_dict which contains:
            IdentifierMapperOptions:
                MappingConnectionString, MappingTableName, SwapColumnName, ReplacementColumnName
            where MappingConnectionString looks like this:
            Server=10.0.50.95;uid=smi;password=stuff;database=nss;
        """
        if yaml_dict:
            # Get the table and column names from yaml
            tableName = yaml_dict['IdentifierMapperOptions']['MappingTableName'] # nss.MAPPING
            chiCol =  yaml_dict['IdentifierMapperOptions']['SwapColumnName'] # CHI_NUMBER
            eupiCol = yaml_dict['IdentifierMapperOptions']['ReplacementColumnName'] # ENCRYPTED_UPI
            CHItoEUPI.dbMappingsStr = yaml_dict['IdentifierMapperOptions']['MappingConnectionString'][:-1] # trim off last ';'
            # Create SQL template: SELECT ENCRYPTED_UPI FROM MAPPING WHERE CHI_NUMBER = %s;
            CHItoEUPI.sql_command = f'SELECT {eupiCol} FROM {tableName} WHERE {chiCol} = %s;'

        if not CHItoEUPI.db_connection:
            self.openDB()

    def lookup(self, chi):
        """ Return a EUPI for a given CHI, or None """
        CHItoEUPI.db_cursor.execute(CHItoEUPI.sql_command, (chi,))
        result = CHItoEUPI.db_cursor.fetchall();
        if len(result) > 0:
            return result[0][0] # i.e. [first element of array][first element of tuple]
        return None

    def openDB(self):
        # Open a connection to the database
        dbMappings = dict(x.split('=') for x in CHItoEUPI.dbMappingsStr.split(';'))
        CHItoEUPI.db_connection = mysql.connector.connect(user=dbMappings['uid'], password=dbMappings['password'], host=dbMappings['Server'], database=dbMappings['database'])
        CHItoEUPI.db_cursor = CHItoEUPI.db_connection.cursor()

    def closeDB(self):
        CHItoEUPI.db_cursor.close()
        CHItoEUPI.db_cursor = None
        CHItoEUPI.db_connection.close()
        CHItoEUPI.db_connection = None


def main():
    #configfile='$SMI_ROOT/configs/smi_dataLoad_mysql.yaml'
    configfile='../config/sample_dataload.yaml'
    with open(os.path.expandvars(configfile)) as fd:
        yaml_dict = yaml.safe_load(fd)
    c = CHItoEUPI(yaml_dict)
    r = c.lookup('0101000014')
    print(r)
    c2 = CHItoEUPI() # reuse existing database connection
    r2 = c2.lookup('0101000014')
    print(r2)

if __name__ == "__main__":
    main()
