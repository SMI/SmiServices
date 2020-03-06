﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FAnsi.Discovery;
using Microservices.IsIdentifiable.Reporting;
using Microservices.IsIdentifiable.Rules;

namespace IsIdentifiableReviewer.Out
{
    public class RowUpdater : OutBase
    {
        public const string DefaultFileName = "RedList.yaml";

        /// <summary>
        /// Set to true to only output updates to redlist instead of trying to update the database.
        /// This is useful if you want to  run in manual mode to process everything then run unattended
        /// for the updates.
        /// </summary>
        public bool RulesOnly { get; set; }

        Dictionary<DiscoveredTable,DiscoveredColumn> _primaryKeys = new Dictionary<DiscoveredTable, DiscoveredColumn>();
        
        public RowUpdater(FileInfo rulesFile) : base(rulesFile)
        {
        }

        public RowUpdater() : this(new FileInfo(DefaultFileName))
        {
        }

        public void Update(Target target, Failure failure, bool addRule)
        {
            Update(target.Discover(),failure,addRule);
        }

        public void Update(DiscoveredServer server, Failure failure, bool addRule)
        {
            //add the update rule to the redlist
            if(addRule)
                Add(failure,RuleAction.Report);

            //if we are running in rules only mode we don't need to also update the database
            if(RulesOnly)
                return;

            var syntax = server.GetQuerySyntaxHelper();

            //the fully specified name e.g. [mydb]..[mytbl]
            string tableName = failure.Resource;

            var tokens = tableName.Split('.', StringSplitOptions.RemoveEmptyEntries);

            var db = tokens.First();
            tableName = tokens.Last();

            if(string.IsNullOrWhiteSpace(db) || string.IsNullOrWhiteSpace(tableName) || string.Equals(db , tableName))
                throw new NotSupportedException($"Could not understand table name {failure.Resource}, maybe it is not full specified with a valid database and table name?");

            db = syntax.GetRuntimeName(db);
            tableName = syntax.GetRuntimeName(tableName);

            DiscoveredTable table = server.ExpectDatabase(db).ExpectTable(tableName);

            //if we've never seen this table before
            if (!_primaryKeys.ContainsKey(table))
            {
                var pk = table.DiscoverColumns().SingleOrDefault(k => k.IsPrimaryKey);
                _primaryKeys.Add(table,pk);
            }

            using (var con = server.GetConnection())
            {
                con.Open();
                
                foreach (var part in failure.Parts)             
                {
                    string sql =
                        $@"update {table.GetFullyQualifiedName()} 
                SET {syntax.EnsureWrapped(failure.ProblemField)} = 
                REPLACE({syntax.EnsureWrapped(failure.ProblemField)},'{syntax.Escape(part.Word)}', 'SMI_REDACTED')
                WHERE {_primaryKeys[table].GetFullyQualifiedName()} = '{syntax.Escape(failure.ResourcePrimaryKey)}'";

                    var cmd = server.GetCommand(sql, con);
                    cmd.ExecuteNonQuery();
                }   
            }
        }

        /// <summary>
        /// Handler for loading <paramref name="failure"/>.  If the user previously made an update decision an
        /// update will transparently happen for this record and false is returned.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="failure"></param>
        /// <returns>True if <paramref name="failure"/> is novel and not seen before</returns>
        public bool OnLoad(DiscoveredServer server,Failure failure)
        {
            //we have bigger problems than if this is novel!
            if (server == null)
                return true;

            //if we have seen this before
            if (IsCoveredByExistingRule(failure))
            {
                //since user has issued an update for this exact problem before we can update this one too
                Update(server,failure,false);

                //and return false to indicate that it is not a novel issue
                return false;
            }

            return true;
        }

    }
}
