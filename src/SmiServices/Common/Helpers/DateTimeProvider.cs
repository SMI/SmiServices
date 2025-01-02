using System;


namespace SmiServices.Common.Helpers;

public class DateTimeProvider
{
    public virtual DateTime UtcNow() => DateTime.UtcNow;
}
