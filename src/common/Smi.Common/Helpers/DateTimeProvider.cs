using System;


namespace Smi.Common.Helpers
{
    public class DateTimeProvider
    {
        public virtual DateTime UtcNow() => DateTime.UtcNow;
    }
}
