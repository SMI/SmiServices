using CommandLine;
using Smi.Common.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microservices.UpdateValues.Options
{
    public abstract class UpdateValuesCliOptions: CliOptions
    {

    }

    [Verb("rdmp",HelpText ="Run the Caching engine which fetches data by date from a remote endpoint in batches of a given size (independently from loading it to any relational databases)")]
    public class UpdateValuesUsingRdmpQueryCliOptions : UpdateValuesCliOptions
    {

        int CatalogueItem {get;set;}
        
        int Filter {get;set;}        

    }
}
