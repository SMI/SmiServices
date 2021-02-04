using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Smi.Common.Messages.Updating
{
    /// <summary>
    /// Requests to update the values in the fields <see cref="WriteIntoFields"/> to <see cref="Values"/>  where the value in <see cref="WhereFields"/> match <see cref="HaveValues"/>
    /// </summary>
    public class UpdateValuesMessage : IMessage
    {
        /// <summary>
        /// Optional Sql operators e.g. "=", "&lt;" etc to use in WHERE Sql when looking for <see cref="HaveValues"/> in <see cref="WhereFields"/>.  If null or empty "=" is assumed for all WHERE comparisons
        /// </summary>
        public string[] Operators {get;set;} = null;

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
        /// Optional.  Where present indicates the tables which should be updated.  If empty then all tables matching the fields should be updated
        /// </summary>
        public int[] ExplicitTableInfo {get;set; }  = new int[0];

        public void Validate()
        {
            if (WhereFields.Length != HaveValues.Length)
                throw new Exception($"{nameof(WhereFields)} length must match {nameof(HaveValues)} length");
            
            if (WriteIntoFields.Length != Values.Length)
                throw new Exception($"{nameof(WriteIntoFields)} length must match {nameof(Values)} length");
                        
            // If operators are specified then the WHERE column count must match the operator count
            if(Operators != null && Operators.Length != 0)
                if (Operators.Length != WhereFields.Length)
                    throw new Exception($"{nameof(WhereFields)} length must match {nameof(Operators)} length");

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
                $"{nameof(UpdateValuesMessage)}: {nameof(WhereFields)}={string.Join(",",WhereFields)} {nameof(WriteIntoFields)}={string.Join(",",WriteIntoFields)}";
        }

        /// <summary>
        /// Checks whether two messages update the same fields using the same where logic
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return obj is UpdateValuesMessage message &&
                   Enumerable.SequenceEqual(Operators ?? new string[0], message.Operators?? new string[0]) &&
                   Enumerable.SequenceEqual(WhereFields ?? new string[0], message.WhereFields?? new string[0]) &&
                   Enumerable.SequenceEqual(HaveValues?? new string[0], message.HaveValues?? new string[0]) &&
                   Enumerable.SequenceEqual(WriteIntoFields?? new string[0], message.WriteIntoFields?? new string[0]) &&
                   Enumerable.SequenceEqual(Values?? new string[0], message.Values?? new string[0]) &&
                   Enumerable.SequenceEqual(ExplicitTableInfo?? new int[0], message.ExplicitTableInfo?? new int[0]);
        }

        /// <summary>
        /// Returns a hashcode based on the sizes of arrays (ok so most of our messages would end up in the same hash bucket but that's probably fine).
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            int hashCode = -1341392600;
            hashCode = hashCode * -1521134295 + EqualityComparer<int>.Default.GetHashCode(Operators?.Length ?? 0);
            hashCode = hashCode * -1521134295 + EqualityComparer<int>.Default.GetHashCode(WhereFields?.Length ?? 0);
            hashCode = hashCode * -1521134295 + EqualityComparer<int>.Default.GetHashCode(HaveValues?.Length ?? 0);
            hashCode = hashCode * -1521134295 + EqualityComparer<int>.Default.GetHashCode(WriteIntoFields?.Length ?? 0);
            hashCode = hashCode * -1521134295 + EqualityComparer<int>.Default.GetHashCode(Values?.Length ?? 0);
            hashCode = hashCode * -1521134295 + EqualityComparer<int>.Default.GetHashCode(ExplicitTableInfo?.Length ?? 0);
            return hashCode;
        }
    }
}
