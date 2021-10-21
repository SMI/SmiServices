using RabbitMQ.Client;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataExport.DataExtraction.Commands;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataFlowPipeline.Requirements;
using Rdmp.Dicom.Extraction;
using ReusableLibraryCode.Progress;
using Smi.Common;
using Smi.Common.Messages.Extraction;
using Smi.Common.Messaging;
using Smi.Common.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace SmiPlugin
{
    /// <summary>
    /// Facilitates image extraction from RDMP clients (windows GUI and command line).  This component dispatches
    /// messages to RabbitMQ so that microservices can anonymise and extract images on disk.  It does not itself
    /// anonymise images during execution.
    /// </summary>
    public class SmiImageExtractor : IDataFlowComponent<DataTable>, IPipelineRequirement<IExtractCommand>
    {
        #region Public user configurable properties

        [DemandsInitialization("The name of the field in your datasets that contains dicom image paths", DefaultValue = SmiConstants.DefaultImagePathColumnName,Mandatory = true)]
        public string RelativeArchiveUriFieldName { get; set; } = SmiConstants.DefaultImagePathColumnName;

        [DemandsInitialization("Username and password to use when sending messages to RabbitMq", Mandatory = true)]
        public DataAccessCredentials RabbitMqCredentials { get; set; }

        [DemandsInitialization("RabbitMq server host name e.g. localhost", Mandatory = true, DefaultValue = "localhost")]
        public string RabbitMqHostName { get; set; } = "localhost";

        [DemandsInitialization("RabbitMq server port", Mandatory = true, DefaultValue = 5672)]
        public int RabbitMqHostPort { get; set; } = 5672;

        [DemandsInitialization("RabbitMq virtual host e.g. '/'", Mandatory = true, DefaultValue = "/")]
        public string RabbitMqVirtualHost { get; set; } = "/";

        [DemandsInitialization("The number of chances to give RabbitMQ to respond confirm message sends", Mandatory = true, DefaultValue = 1)]
        public int MaxConfirms { get; set; } = 1;

        [DemandsInitialization("The exchange to send ExtractFileMessages to", Mandatory = true, DefaultValue = "ExtractFileExchange")]
        public string ExtractFilesExchange { get; set; } = "ExtractFileExchange";

        [DemandsInitialization("The exchange to send ExtractFileMessages to", Mandatory = true, DefaultValue = "FileCollectionInfoExchange")]
        public string ExtractFilesInfoExchange { get; set; } = "FileCollectionInfoExchange";

        [DemandsInitialization("Optional UID mapping server.  If set then data in pipeline will have Study/Series/Instance UIDs replaced with anonymous mappings")]
        public ExternalDatabaseServer UIDMappingServer { get; set; }
        #endregion

        #region Private fields
        /// <summary>
        /// Datasets being extracted that pass through this component must have these
        /// UID fields in order to be extracted
        /// </summary>
        private Dictionary<string,UIDType> _expectedUidFields = new Dictionary<string,UIDType>{
            { SmiConstants.DefaultStudyIdColumnName, UIDType.StudyInstanceUID},
            {SmiConstants.DefaultSeriesIdColumnName, UIDType.SeriesInstanceUID },
            {SmiConstants.DefaultInstanceIdColumnName, UIDType.SOPInstanceUID }
        };
        private RabbitMqAdapter _adapter;
        private IProducerModel _fileMessageSender;
        private IProducerModel _infoMessageSender;
        private int _projectNumber;
        private IExtractCommand _extractCommand;

        /// <summary>
        /// Records everything we were asked to extract as list of UIDs.  Required to send messages
        /// </summary>
        private ExtractionRequestMessage _request;
        #endregion

        public DataTable ProcessPipelineData(DataTable toProcess, IDataLoadEventListener listener, GracefulCancellationToken cancellationToken)
        {
            // if the dataset being extracted is not an imaging one
            if (!toProcess.Columns.Contains(RelativeArchiveUriFieldName))
            {
                // pass it along unchanged
                return toProcess;
            }

            // it is an imaging one, make sure it has all the required UIDs
            var missingFields = _expectedUidFields.Where(c => !toProcess.Columns.Contains(c.Key)).ToArray();

            if(missingFields.Any())
            {
                throw new Exception($"Imaging table was missing required field(s) {string.Join(',', missingFields)}");
            }
            
            SetupConnection();

            var mappingServer = UIDMappingServer == null ? null : new MappingRepository(UIDMappingServer);

            foreach (DataRow dr in toProcess.Rows)
            {
                // TODO: send messages
                //new ExtractFileMessage()

                if(mappingServer != null)
                {
                    SwapUIDsInRow(dr, mappingServer);
                }
            }

            return toProcess;
        }

        private void SwapUIDsInRow(DataRow dr, MappingRepository mappingServer)
        {

            //rewrite the UIDs in the pipeline data so the output CSV/Table has anonymised UIDs
            foreach (var uidField in _expectedUidFields)
            {
                var value = dr[uidField.Key] as string;

                //if there is no value for this UID (somehow)
                if (value == null)
                {
                    // skip it
                    continue;
                }

                // get anonymous UID mapping for this UID Type
                var releaseValue = mappingServer.GetOrAllocateMapping(value, _projectNumber, uidField.Value);

                //change value in data table row
                dr[uidField.Key] = releaseValue;
            }
        }


        /// <summary>
        /// Sets up RabbitMq connection if it has not already happened
        /// </summary>
        private void SetupConnection()
        {
            if (_adapter != null)
            {
                return;
            }

            var factory = new ConnectionFactory()
            {
                HostName = RabbitMqHostName,
                VirtualHost = RabbitMqVirtualHost,
                Port = RabbitMqHostPort,
                UserName = RabbitMqCredentials.Username,
                Password = RabbitMqCredentials.GetDecryptedPassword()
            };

            _adapter = new RabbitMqAdapter(factory, nameof(SmiImageExtractor));

            _fileMessageSender = _adapter.SetupProducer(new ProducerOptions()
            {
                ExchangeName = ExtractFilesExchange,
                MaxConfirmAttempts = MaxConfirms
            });

            _infoMessageSender = _adapter.SetupProducer(new ProducerOptions()
            {
                ExchangeName = ExtractFilesInfoExchange,
                MaxConfirmAttempts = MaxConfirms
            });

            _projectNumber = _extractCommand.Configuration.Project.ProjectNumber ?? throw new Exception("Project must have a number");

            _request = new ExtractionRequestMessage()
            {
                ExtractionJobIdentifier = Guid.NewGuid(),
                ProjectNumber = _projectNumber.ToString() ,

                // TODO : populate the rest of these
            };
        }

        public void Dispose(IDataLoadEventListener listener, Exception pipelineFailureExceptionIfAny)
        {

        }

        public void Abort(IDataLoadEventListener listener)
        {

        }

        public void PreInitialize(IExtractCommand value, IDataLoadEventListener listener)
        {
            _extractCommand = value;
        }
    }
}
