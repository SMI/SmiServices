using Equ;
using System;

namespace SmiServices.Common.Messages.Updating;

/// <summary>
/// Requests to update the values in the fields <see cref="WriteIntoFields"/> to <see cref="Values"/>  where the value in <see cref="WhereFields"/> match <see cref="HaveValues"/>
/// </summary>
public class UpdateValuesMessage : MemberwiseEquatable<UpdateValuesMessage>, IMessage
{
    /// <summary>
    /// Optional Sql operators e.g. "=", "&lt;" etc to use in WHERE Sql when looking for <see cref="HaveValues"/> in <see cref="WhereFields"/>.  If null or empty "=" is assumed for all WHERE comparisons
    /// </summary>
    public string[]? Operators { get; set; } = null;

    /// <summary>
    /// The field(s) to search the database for (this should be the human readable name without qualifiers as it may match multiple tables e.g. ECHI)
    /// </summary>
    public string?[] WhereFields { get; set; } = [];

    /// <summary>
    /// The values to search for when deciding which records to update
    /// </summary>
    public string?[] HaveValues { get; set; } = [];

    /// <summary>
    /// The field(s) which should be updated, may be the same as the <see cref="WhereFields"/>
    /// </summary>
    public string[] WriteIntoFields { get; set; } = [];

    /// <summary>
    /// The values to write into matching records (see <see cref="WriteIntoFields"/>).  Null elements in this array should be treated as <see cref="DBNull.Value"/>
    /// </summary>
    public string[] Values { get; set; } = [];

    /// <summary>
    /// Optional.  Where present indicates the tables which should be updated.  If empty then all tables matching the fields should be updated
    /// </summary>
    public int[] ExplicitTableInfo { get; set; } = [];

    public void Validate()
    {
        if (WhereFields.Length != HaveValues.Length)
            throw new Exception($"{nameof(WhereFields)} length must match {nameof(HaveValues)} length");

        if (WriteIntoFields.Length != Values.Length)
            throw new Exception($"{nameof(WriteIntoFields)} length must match {nameof(Values)} length");

        // If operators are specified then the WHERE column count must match the operator count
        if (Operators != null && Operators.Length != 0)
            if (Operators.Length != WhereFields.Length)
                throw new Exception($"{nameof(WhereFields)} length must match {nameof(Operators)} length");

        if (WhereFields.Length == 0)
            throw new Exception("There must be at least one search field for WHERE section.  Otherwise this would update entire tables");

        if (WriteIntoFields.Length == 0)
            throw new Exception("There must be at least one value to write");

    }

    /// <summary>
    /// Describes the message in terms of fields that are updated and checked in WHERE logic (but not values)
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return
            $"{nameof(UpdateValuesMessage)}: {nameof(WhereFields)}={string.Join(",", WhereFields)} {nameof(WriteIntoFields)}={string.Join(",", WriteIntoFields)}";
    }
}
