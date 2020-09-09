using System;
using System.Collections.Generic;
using System.Text;

namespace Smi.Common.Options
{
    /// <summary>
    /// For classes that modify <see cref="GlobalOptions"/> e.g. populate passwords from a vault etc
    /// </summary>
    public interface IOptionsDecorator
    {
        GlobalOptions Decorate(GlobalOptions options);
    }
}
