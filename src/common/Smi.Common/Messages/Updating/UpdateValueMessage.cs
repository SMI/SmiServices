using System;
using System.Collections.Generic;
using System.Text;

namespace Smi.Common.Messages.Updating
{
    /// <summary>
    /// Requests to update the values in the fields <see cref="WriteIntoFields"/> to <see cref="Values"/>  where the value in <see cref="WhereFields"/> match <see cref="HaveValues"/>
    /// </summary>
    public class UpdateValueMessage : IMessage
    {
        /// <summary>
        /// Sql operator e.g. "=" to use in WHERE Sql when looking for <see cref="HaveValues"/> in <see cref="WhereFields"/>
        /// </summary>
        public string Operator {get;set;} = "=";

        /// <summary>
        /// The field(s) to search the database for (this should be the human readable name without qualifiers as it may match multiple tables e.g. ECHI)
        /// </summary>
        public string[] WhereFields {get;set;} = new string[0];

        /// <summary>
        /// The values to search for when deciding which records to update
        /// </summary>
        public string[] HaveValues {get;set;} = new string[0];

        /// <summary>
        /// The field(s) which should be updated, may be the same as the <see cref="WhereFields"/>
        /// </summary>
        public string[] WriteIntoFields {get;set;} = new string[0];

        /// <summary>
        /// The values to write into matching records (see <see cref="WriteIntoFields"/>).  Null elements in this array should be treated as <see cref="DBNull.Value"/>
        /// </summary>
        public string[] Values {get;set;} = new string[0];

        /// <summary>
        /// Optional.  Where present indicates the tables which should be updated.  If null/empty then all tables matching the fields should be updated
        /// </summary>
        public int[] ExplicitTableInfo;

        public void Validate()
        {
            if (WhereFields.Length != HaveValues.Length)
                throw new Exception($"{nameof(WhereFields)} length must match {nameof(HaveValues)} length");

            
            if (WriteIntoFields.Length != Values.Length)
                throw new Exception($"{nameof(WriteIntoFields)} length must match {nameof(Values)} length");

            if(WhereFields.Length == 0)
                throw new Exception("There must be at least one search field for WHERE section.  Otherwise this would update entire tables");

            if(WriteIntoFields.Length == 0)
                throw new Exception("There must be at least one value to write");
        }

        /// <summary>
        /// Describes the message in terms of fields that are updated and checked in WHERE logic (but not values)
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return 
                $"{nameof(UpdateValueMessage)}: {nameof(WhereFields)}={string.Join(",",WhereFields)} {nameof(WriteIntoFields)}={string.Join(",",WriteIntoFields)}";
        }

    }
}
